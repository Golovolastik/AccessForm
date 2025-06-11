using Microsoft.AspNetCore.Mvc;
using AccessForm.Models;
using System.Text.Json;

namespace AccessForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TerminateAccessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TerminateAccessController> _logger;

    public TerminateAccessController(ApplicationDbContext dbContext, ILogger<TerminateAccessController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTerminateAccessRequest()
    {
        _logger.LogInformation("Получен запрос на создание заявки на прекращение доступа");
        
        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Полученные данные: {RequestBody}", requestBody);

            var formData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            // TODO: Добавить сохранение в базу данных
            
            return Ok(new { message = "Заявка на прекращение доступа успешно отправлена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании заявки на прекращение доступа");
            return Problem($"Ошибка при создании заявки на прекращение доступа: {ex.Message}");
        }
    }
} 