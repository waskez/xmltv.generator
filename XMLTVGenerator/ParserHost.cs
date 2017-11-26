using AngleSharp.Parser.Html;
using ParserContracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XMLTVGenerator
{
    class ParserHost : IParserHost
    {
        #region Fields

        private readonly ILogger logger;
        private readonly HttpClient client;

        #endregion

        #region Constructor

        public ParserHost(ILogger logger)
        {
            this.logger = logger;
            client = new HttpClient();
        }

        #endregion

        #region Parsers

        public ICollection<IParser> LoadParsers(string path)
        {
            string[] dllFileNames = null;

            if (Directory.Exists(path))
            {
                dllFileNames = Directory.GetFiles(path, "*.dll");

                ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                foreach (string dllFile in dllFileNames)
                {
                    Assembly assembly = Assembly.LoadFrom(dllFile);
                    assemblies.Add(assembly);
                }

                Type pluginType = typeof(IParser);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        Type[] types = assembly.GetTypes();

                        foreach (Type type in types)
                        {
                            if (type.IsInterface || type.IsAbstract)
                            {
                                continue;
                            }
                            else
                            {
                                if (type.GetInterface(pluginType.FullName) != null)
                                {
                                    pluginTypes.Add(type);
                                }
                            }
                        }
                    }
                }

                ICollection<IParser> plugins = new List<IParser>(pluginTypes.Count);
                foreach (Type type in pluginTypes)
                {
                    IParser plugin = (IParser)Activator.CreateInstance(type);
                    plugin.Host = this;
                    plugins.Add(plugin);
                }

                return plugins;
            }

            return null;
        }

        public void Feedback(FeedbackType type, IParser parser, string format, params object[] args)
        {
            switch (type)
            {
                case FeedbackType.Debug:
                    logger.Debug($"{parser.ChannelName}: " + format, args);
                    break;
                case FeedbackType.Info:
                    logger.Information($"{parser.ChannelName}: " + format, args);
                    break;
                case FeedbackType.Warning:
                    logger.Warning($"{parser.ChannelName}: " + format, args);
                    break;
                case FeedbackType.Error:
                    logger.Error($"{parser.ChannelName}: " + format, args);
                    break;
            }
        }

        #endregion

        #region Source parsing

        public async Task<List<Programme>> Start(IParser parser, DateTime from, DateTime to)
        {
            return await Worker(parser, from, to);
        }

        private async Task<List<Programme>> Worker(IParser parser, DateTime from, DateTime to)
        {
            var programList = new List<Programme>();

            for (var date = from; date <= to; date = date.AddDays(1.0))
            {
                var url = string.Format(parser.Url, date);

                Feedback(FeedbackType.Info, parser, "Lapas {0} ielāde ...", url);
                var source = await GetSource(url);

                Feedback(FeedbackType.Info, parser, "Lapa ielādēta. IHtmlDocument izveidošana ...");
                var domParser = new HtmlParser();
                var document = await domParser.ParseAsync(source);

                Feedback(FeedbackType.Info, parser, "IHtmlDocument izveidots. Sākas {0:dd.MM.yyyy} programmas ierakstu apstrāde ...", date);
                programList.AddRange(parser.Parse(document, date));

                Feedback(FeedbackType.Info, parser, "Apstrādāti {0} programmas ieraksti", programList.Count);
            }

            return programList;
        }

        private async Task<string> GetSource(string url)
        {
            string source = null;

            var response = await client.GetAsync(url);

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                source = await response.Content.ReadAsStringAsync();
            }

            return source;
        }

        #endregion

        #region Build XMLTV

        public void Build(List<Channel> channels)
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
            var root = new XElement("tv", new XAttribute("generator-info-name", "xmltv-generator"));

            // Kanāli
            foreach (var ch in channels)
            {
                var channel = new XElement("channel", new XAttribute("id", ch.Id), new XElement("display-name", ch.Name));
                root.Add(channel);
            }

            // Kanālu programmas
            // dienas programmas pēdējam ierakstam nav beigu laika, tāpēc ja tekošā programma nav pēdējā, 
            // nepieciešams iegūt nākošās programmas pirmā ieraksta sākuma laiku, pretējā
            // gadījumā liekam nākošās dienas datumu ar laiku 00:00:00
            foreach (var ch in channels)
            {
                // sortējam pēc datumiem - vai vajag?
                var programme = ch.ProgramListing.OrderBy(p => p.Start).ToList();

                var lastProgramme = programme.Last();
                for(var p = 0; p < programme.Count; p++)
                {
                    var startDate = programme[p].Start;
                    var endDate = programme[p].End;
                    if(!endDate.HasValue)
                    {
                        if (programme[p].Equals(lastProgramme)) // pēdējais programmas ieraksts
                        {
                            endDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0).AddDays(1); // nākošās dienas datums ar laiku 00:00:00
                        }
                        else // nākošās dienas pirmā ieraksta sākuma datums
                        {
                            endDate = programme[p + 1].Start;
                        }
                    }

                    var element = new XElement("programme",
                           new XAttribute("start", DateToISOString(startDate, ch.TimeZoneId)),
                           new XAttribute("stop", DateToISOString(endDate.Value, ch.TimeZoneId)),
                           new XAttribute("channel", ch.Id),
                           new XElement("title", programme[p].Title));
                    if(!string.IsNullOrEmpty(programme[p].Description))
                    {
                        element.Add(new XElement("desc", programme[p].Description));
                    }
                    root.Add(element);
                }
            }

            doc.Add(root);
            doc.Save("XMLTV.xml");
            //var wr = new StringWriter();
            //doc.Save(wr);
            //Console.Write(wr.ToString());            
        }

        private static string DateToISOString(DateTime date, string timeZoneId)
        {
            // Datums un laiks formātā ISO 8601 'YYYYMMDDhhmmss +0200' = laika zona
            // Ja nav norādīta laika zona, tad datums tiek uzskatīts kā UTC un KODI pats pieliek laika zonu no sistēmas
            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var newDate = TimeZoneInfo.ConvertTime(date, tst);
            var utcOffset = tst.GetUtcOffset(newDate);
            var offsetString = ((utcOffset < TimeSpan.Zero) ? " -" : " +") + utcOffset.ToString("hhmm");
            return $"{date:yyyyMMddHHmmss} {offsetString}";
        }

        #endregion
    }
}