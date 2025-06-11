using Microsoft.AspNetCore.Mvc;
using AccessForm.Models;
using System.Text.Json;

namespace AccessForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AccessRequestController> _logger;

    public AccessRequestController(ApplicationDbContext dbContext, ILogger<AccessRequestController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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

            var accessRequest = new AccessRequest
            {
                FullName = formData.GetProperty("name").GetString() ?? string.Empty,
                Position = formData.GetProperty("position").GetString() ?? string.Empty,
                EmploymentDate = DateTime.SpecifyKind(DateTime.Parse(formData.GetProperty("date-of-employment").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd")), DateTimeKind.Utc),
                RequestTypeId = 1, // ID для "Заявка на предоставление доступа"
                DocumentPath = "pending", // Временное значение, будет обновлено позже
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