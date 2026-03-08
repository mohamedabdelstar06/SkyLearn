using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Presentation;
using System.Text;

namespace SkyLearnApi.Services.TextPipeline
{
    public class OpenXmlTextExtractor : ITextExtractor
    {
        public bool CanHandle(string contentType)
        {
            var ct = contentType.ToLowerInvariant();
            return ct.Contains("word") || ct.Contains("document") || 
                   ct.Contains("presentation") || ct.Contains("powerpoint") ||
                   contentType.Contains("officedocument");
        }

        public async Task<string> ExtractTextAsync(string filePath, string contentType)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();
                var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".docx")
                {
                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        var body = wordDoc.MainDocumentPart?.Document.Body;
                        if (body != null)
                        {
                            foreach (var para in body.Elements<Paragraph>())
                            {
                                text.AppendLine(para.InnerText);
                            }
                        }
                    }
                }
                else if (ext == ".pptx")
                {
                    using (PresentationDocument pptDoc = PresentationDocument.Open(filePath, false))
                    {
                        var presentationPart = pptDoc.PresentationPart;
                        if (presentationPart != null && presentationPart.SlideParts != null)
                        {
                            foreach (var slidePart in presentationPart.SlideParts)
                            {
                                if (slidePart.Slide?.CommonSlideData?.ShapeTree != null)
                                {
                                    foreach (var shape in slidePart.Slide.CommonSlideData.ShapeTree.Elements<DocumentFormat.OpenXml.Presentation.Shape>())
                                    {
                                        var textBody = shape.TextBody;
                                        if (textBody != null)
                                        {
                                            foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
                                            {
                                                text.AppendLine(paragraph.InnerText);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    return "Unsupported OpenXML format. Only .docx and .pptx are supported for text extraction.";
                }

                return text.ToString();
            });
        }
    }
}
