using ParserContracts;
using System;
using AngleSharp.Dom.Html;
using System.Collections.Generic;
using System.Linq;

namespace LNT
{
    public class Parser : IParser
    {
        public IParserHost Host { get; set; }
        public string ChannelId => "lnt";
        public string ChannelName => "LNT";
        public string Url => "https://tv.lattelecom.lv/lv/kanali/interaktivie/lnt/{0:dd.MM.yyyy}";
        public string TimeZoneId => "FLE Standard Time";//"Russian Standard Time";//

        public List<Programme> Parse(IHtmlDocument document, DateTime date)
        {
            var programListing = new List<Programme>();

            var container = document.QuerySelectorAll("ul")
                .Where(item => item.Id == "program-list-view")
                .FirstOrDefault();

            if (container != null)
            {
                var items = container.QuerySelectorAll("li").ToList();
                foreach (var item in items)
                {
                    var time = item.QuerySelector("b").TextContent.Split(':');
                    var startTime = new TimeSpan(Convert.ToInt32(time[0]), Convert.ToInt32(time[1]), 0);

                    var programme = new Programme
                    {
                        Title = item.QuerySelector("a").TextContent,
                        Description = item.QuerySelector("p")?.TextContent,
                        Start = date.Add(startTime)
                    };

                    programListing.Add(programme);
                }

                // aizpildam End laiku
                for (var i = 0; i < programListing.Count; i++)
                {
                    if (i < programListing.Count - 1) //pēdējam laiku nezinām
                    {
                        programListing[i].End = programListing[i + 1].Start;
                    }
                }
            }

            return programListing;
        }
    }
}