using Microsoft.AspNetCore.Mvc;
using AccessForm.Models;
using System.Text.Json;
using AccessForm.Services;
using System.Collections.Generic;

namespace AccessForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RequestController> _logger;
    private readonly IWordDocumentService _wordDocumentService;

    public RequestController(
        ApplicationDbContext dbContext, 
        ILogger<RequestController> logger,
        IWordDocumentService wordDocumentService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _wordDocumentService = wordDocumentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest()
    {
        _logger.LogInformation("Получен запрос на создание заявки");

        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Полученные данные: {RequestBody}", requestBody);

            var formData = JsonSerializer.Deserialize<JsonElement>(requestBody);

            // Определяем тип заявки (например, по полю requestTypeId или другому признаку)
            int requestTypeId = formData.TryGetProperty("requestTypeId", out var typeProp)
                ? (typeProp.ValueKind == JsonValueKind.Number ? typeProp.GetInt32() : int.Parse(typeProp.GetString() ?? "1"))
                : 1; // По умолчанию "Заявка на предоставление доступа"

            // Выбираем шаблон по типу заявки
            string templateFile = requestTypeId switch
            {
                1 => "AccessRequestTemplate.docx",
                2 => "NoticeOfTransfer.docx",
                3 => "RequestToTerminateAccess.docx",
                _ => "AccessRequestTemplate.docx"
            };
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateFile);

            var documentPath = await _wordDocumentService.CreateDocumentCopyAsync(templatePath);

            // Подготавливаем данные для замены в документе
            var replacements = new Dictionary<string, string>();
            foreach (var property in formData.EnumerateObject())
            {
                string value = property.Value.ValueKind switch
                {
                    JsonValueKind.True => "☑",
                    JsonValueKind.False => "☐",
                    JsonValueKind.Number => property.Value.ToString(),
                    _ => property.Value.GetString() ?? string.Empty
                };
                replacements[$"{{{property.Name}}}"] = value;
            }

            documentPath = await _wordDocumentService.UpdateDocumentContentAsync(documentPath, replacements);

            var request = new Request
            {
                FullName = formData.GetProperty("name").GetString() ?? string.Empty,
                Position = formData.GetProperty("position").GetString() ?? string.Empty,
                RequestTypeId = requestTypeId,
                DocumentPath = documentPath,
                CreatedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            };

            // _dbContext.AccessRequests.Add(request);
            // await _dbContext.SaveChangesAsync();

            // Конвертируем документ в PDF
            documentPath = await _wordDocumentService.ConvertToPdfAndUpdatePathAsync(documentPath, request.Id);
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(documentPath);
            var fileName = Path.GetFileName(documentPath);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании заявки");
            return Problem($"Ошибка при создании заявки: {ex.Message}");
        }
    }

    [HttpPost("UploadWithForm")]
    public async Task<IActionResult> UploadWithForm(
        [FromForm] IFormFile file,
        [FromForm] string fullName,
        [FromForm] string position,
        [FromForm] int requestTypeId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(position))
            return BadRequest("Не все обязательные поля заполнены");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "UploadedDocuments");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var fileName = $"scanned_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var accessRequest = new Request
        {
            FullName = fullName,
            Position = position,
            DocumentPath = filePath,
            RequestTypeId = requestTypeId,
            CreatedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        };
        _dbContext.AccessRequests.Add(accessRequest);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Заявка успешно создана и файл загружен" });
    }
} 