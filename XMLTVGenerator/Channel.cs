using ParserContracts;
using System.Collections.Generic;

namespace XMLTVGenerator
{
    public class Channel
    {
        string timeZone;
        public Channel(string timeZoneId)
        {
            timeZone = timeZoneId;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string TimeZoneId
        {
            get
            {
                return timeZone;
            }
            private set
            {
                timeZone = value;
            }
        }
        public List<Programme> ProgramListing { get; set; }
    }
}