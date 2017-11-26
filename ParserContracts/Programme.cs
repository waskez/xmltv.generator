using System;

namespace ParserContracts
{
    public class Programme
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}