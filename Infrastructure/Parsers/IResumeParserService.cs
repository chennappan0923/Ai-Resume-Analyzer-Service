namespace AI_Resume_Analyzer_API.Infrastructure.Parsers
{
    public interface IResumeParserService
    {
        string ExtractText(string filePath);
    }
}
