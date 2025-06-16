using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AccessForm.Services
{
    public class WordDocumentService : IWordDocumentService
    {
        private readonly IWebHostEnvironment _environment;
        private const string TemplateFileName = "doc-template.docx";

        public WordDocumentService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> CreateDocumentCopyAsync()
        {
            var templatePath = Path.Combine(_environment.ContentRootPath, TemplateFileName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file {TemplateFileName} not found");
            }

            var outputDirectory = Path.Combine(_environment.ContentRootPath, "GeneratedDocuments");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var newFileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var outputPath = Path.Combine(outputDirectory, newFileName);

            File.Copy(templatePath, outputPath);
            return outputPath;
        }

        public async Task<string> UpdateDocumentContentAsync(string documentPath, Dictionary<string, string> replacements)
        {
            using (var doc = WordprocessingDocument.Open(documentPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                // Проходим по всем таблицам в документе
                foreach (var table in body.Elements<Table>())
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            foreach (var paragraph in cell.Elements<Paragraph>())
                            {
                                foreach (var run in paragraph.Elements<Run>())
                                {
                                    foreach (var text in run.Elements<Text>())
                                    {
                                        // Проверяем, содержит ли текст один из ключей замены
                                        foreach (var replacement in replacements)
                                        {
                                            if (text.Text.Contains(replacement.Key))
                                            {
                                                text.Text = text.Text.Replace(replacement.Key, replacement.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                doc.MainDocumentPart.Document.Save();
            }

            return documentPath;
        }
    }
} 