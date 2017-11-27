using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace XMLTVGenerator
{
    class Program
    {
        static string BaseDirectory
        {
            get
            {
                return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            }
        }

        static void Main(string[] args)
        {
            #region Inicializācija

            var console = args.SingleOrDefault(a => a.Equals("-console")) != null;

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            ILogger logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.RollingFile(BaseDirectory + @"\Logs\{Date}.txt")
                        .CreateLogger();

            var host = new ParserHost(logger, config, BaseDirectory);

            var loadShuraTv = config["AppSettings:LoadShuraTvEpg"] == "True";

            logger.Information(new string('=', 70));

            #endregion

            try
            {
                if (loadShuraTv)
                {
                    ShuraTvEpgLoader.Run(BaseDirectory);
                }

                var parserDirectory = BaseDirectory + @"\Parsers";

                var parsers = host.LoadParsers(parserDirectory);
                if(parsers.Count > 0)
                {
                    var channels = new List<Channel>();
                    foreach(var parser in parsers)
                    {
                        logger.Information("Ielādēts kanāla {0} analizētājs", parser.ChannelName);

                        var channel = new Channel(parser.TimeZoneId)
                        {
                            Id = parser.ChannelId,
                            Name = parser.ChannelName
                        };

                        Task.Run(async () => { channel.ProgramListing = await host.Start(parser, DateTime.Today, DateTime.Today.AddDays(3.0)); }).Wait();
                        channels.Add(channel);

                        logger.Information("Kanāla {0} analizētājs pabeidza darbu", parser.ChannelName);
                    }                   

                    host.Build(channels, loadShuraTv);
                }
                else
                {
                    logger.Warning("Direktorijā {0} netika atrasts neviens analizētājs", parserDirectory);
                }
            }
            catch(Exception exc)
            {
                logger.Error(exc.Message);
                if(exc.InnerException != null)
                {
                    logger.Error(exc.InnerException.Message);
                }                
            }

            logger.Information(new string('=', 70));

            if (console)
            {
                Console.ReadKey();
            }
        }
    }
}