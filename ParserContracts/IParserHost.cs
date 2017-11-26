namespace ParserContracts
{
    public interface IParserHost
    {
        /// <summary>
        /// Logs feedback messages
        /// </summary>
        /// <param name="type">Feedback type</param>
        /// <param name="parser">The parser that called the feedback</param>
        /// <param name="format">A composite format string</param> 
        /// <param name="args">An object to write using format</param>
        void Feedback(FeedbackType type, IParser parser, string format, params object[] args);
    }

    public enum FeedbackType
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }
}