using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AccessForm.Models;

namespace AccessForm.Services
{
    public class WordDocumentService : IWordDocumentService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _dbContext;
        //private const string TemplateFileName = "doc-template.docx";

        public WordDocumentService(IWebHostEnvironment environment, ApplicationDbContext dbContext)
        {
            _environment = environment;
            _dbContext = dbContext;
        }

        public async Task<string> CreateDocumentCopyAsync(string templatePath)
        {
            //var templatePath = Path.Combine(_environment.ContentRootPath, TemplateFileName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found at path: {templatePath}");
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
                var body = doc.MainDocumentPart?.Document.Body;

                if (body != null)
                {
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
            }

            return documentPath;
        }

        public async Task<string> ConvertToPdfAndUpdatePathAsync(string wordDocumentPath, int accessRequestId)
        {
            if (!File.Exists(wordDocumentPath))
            {
                throw new FileNotFoundException($"Word document not found at path: {wordDocumentPath}");
            }

            var outputDirectory = Path.Combine(_environment.ContentRootPath, "GeneratedDocuments");
            var pdfFileName = Path.GetFileNameWithoutExtension(wordDocumentPath) + ".pdf";
            var pdfPath = Path.Combine(outputDirectory, pdfFileName);

            // Конвертируем документ в PDF используя LibreOffice
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "libreoffice",
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDirectory}\" \"{wordDocumentPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start LibreOffice process");
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"LibreOffice conversion failed with exit code {process.ExitCode}. Error: {error}");
                }
            }

            // Проверяем, что PDF файл был создан
            if (!File.Exists(pdfPath))
            {
                throw new Exception("PDF file was not created after conversion");
            }

            // Обновляем путь к документу в базе данных
            var accessRequest = await _dbContext.AccessRequests.FindAsync(accessRequestId);
            if (accessRequest != null)
            {
                accessRequest.DocumentPath = pdfPath;
                await _dbContext.SaveChangesAsync();
            }

            // Удаляем исходный Word документ
            if (File.Exists(wordDocumentPath))
            {
                File.Delete(wordDocumentPath);
            }

            return pdfPath;
        }
    }
} 