using System.Text;
using UglyToad.PdfPig;

namespace AI_Resume_Analyzer_API.Infrastructure.Parsers
{
    public class PdfParserService : IResumeParserService
    {
        public string ExtractText(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("PDF file not found", filePath);
            }

            var textBuilder = new StringBuilder();

            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }
            }

            return textBuilder.ToString();
        }
    }
}
