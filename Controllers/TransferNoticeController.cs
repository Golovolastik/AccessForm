using Microsoft.AspNetCore.Mvc;
using AccessForm.Models;
using System.Text.Json;

namespace AccessForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferNoticeController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransferNoticeController> _logger;

    public TransferNoticeController(ApplicationDbContext dbContext, ILogger<TransferNoticeController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransferNotice()
    {
        _logger.LogInformation("Получен запрос на создание уведомления о переводе");
        
        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Полученные данные: {RequestBody}", requestBody);

            var formData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            // TODO: Добавить сохранение в базу данных
            
            return Ok(new { message = "Уведомление о переводе успешно отправлено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании уведомления о переводе");
            return Problem($"Ошибка при создании уведомления о переводе: {ex.Message}");
        }
    }
} 