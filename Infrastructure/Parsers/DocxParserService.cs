using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AI_Resume_Analyzer_API.Infrastructure.Parsers
{
    public class DocxParserService : IResumeParserService
    {
        public string ExtractText(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("DOCX file not found", filePath);
            }

            var textBuilder = new StringBuilder();

            using (var doc = WordprocessingDocument.Open(filePath, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        var text = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            textBuilder.AppendLine(text);
                        }
                    }
                }
            }

            return textBuilder.ToString();
        }
    }
}
