using AngleSharp.Dom.Html;
using System;
using System.Collections.Generic;

namespace ParserContracts
{
    public interface IParser
    {
        IParserHost Host { get; set; }
        string ChannelId { get; }
        string ChannelName { get; }
        string Url { get; }
        string TimeZoneId { get; }
        List<Programme> Parse(IHtmlDocument document, DateTime date);
    }
}