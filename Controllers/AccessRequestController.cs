using Microsoft.AspNetCore.Mvc;
using AccessForm.Models;
using System.Text.Json;
using AccessForm.Services;
using System.Collections.Generic;

namespace AccessForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AccessRequestController> _logger;
    private readonly IWordDocumentService _wordDocumentService;

    public AccessRequestController(
        ApplicationDbContext dbContext, 
        ILogger<AccessRequestController> logger,
        IWordDocumentService wordDocumentService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _wordDocumentService = wordDocumentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccessRequest()
    {
        _logger.LogInformation("Получен запрос на создание заявки на доступ");
        
        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Полученные данные: {RequestBody}", requestBody);

            var formData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            _logger.LogInformation("Поля формы:");
            foreach (var property in formData.EnumerateObject())
            {
                _logger.LogInformation("{PropertyName}: {PropertyValue}", property.Name, property.Value);
            }

            // Создаем копию документа
            var documentPath = await _wordDocumentService.CreateDocumentCopyAsync();
            _logger.LogInformation("Создана копия документа: {DocumentPath}", documentPath);

            // Подготавливаем данные для замены в документе
            var replacements = new Dictionary<string, string>();

            // Список полей, которые являются чекбоксами
            var checkboxFields = new HashSet<string>
            {
                "removable-media",
                "sed-access",
                "arm-mis-access",
                "arm-bookkeeper-access",
                "arm-mis-pn-access",
                "email-access",
                "ria-access",
                "rtc-access",
                "hiv-access",
                "eis-access",
                "access-control-system-access"
            };

            // Добавляем все поля из формы в словарь замен
            foreach (var property in formData.EnumerateObject())
            {
                string value;
                if (checkboxFields.Contains(property.Name))
                {
                    // Для чекбоксов используем специальные символы
                    value = property.Value.ValueKind == JsonValueKind.True ? "☑" : "☐";
                }
                else
                {
                    // Для остальных полей используем текстовое значение
                    value = property.Value.GetString() ?? string.Empty;
                }
                
                replacements[$"{{{property.Name}}}"] = value;
            }

            // Обновляем содержимое документа
            documentPath = await _wordDocumentService.UpdateDocumentContentAsync(documentPath, replacements);
            _logger.LogInformation("Документ обновлен: {DocumentPath}", documentPath);

            var accessRequest = new AccessRequest
            {
                FullName = formData.GetProperty("name").GetString() ?? string.Empty,
                Position = formData.GetProperty("position").GetString() ?? string.Empty,
                EmploymentDate = DateTime.SpecifyKind(DateTime.Parse(formData.GetProperty("date-of-employment").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd")), DateTimeKind.Utc),
                RequestTypeId = 1, // ID для "Заявка на предоставление доступа"
                DocumentPath = documentPath,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Создана заявка: {AccessRequest}", JsonSerializer.Serialize(accessRequest));

            var requestType = await _dbContext.RequestTypes.FindAsync(1);
            if (requestType == null)
            {
                _logger.LogError("Тип запроса с ID=1 не найден в базе данных");
                return Problem("Тип запроса не найден в базе данных");
            }

            _dbContext.AccessRequests.Add(accessRequest);
            await _dbContext.SaveChangesAsync();

            // Конвертируем документ в PDF
            try
            {
                documentPath = await _wordDocumentService.ConvertToPdfAndUpdatePathAsync(documentPath, accessRequest.Id);
                _logger.LogInformation("Документ сконвертирован в PDF: {DocumentPath}", documentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при конвертации документа в PDF");
                // Не прерываем выполнение, так как документ уже создан
            }

            return Ok(new { 
                message = "Форма успешно отправлена",
                requestId = accessRequest.Id 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании заявки");
            return Problem($"Ошибка при создании заявки: {ex.Message}");
        }
    }
} 