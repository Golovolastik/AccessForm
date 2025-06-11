using Microsoft.EntityFrameworkCore;
using Npgsql;
using AccessForm.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Явно указываем порт
builder.WebHost.UseUrls("http://+:5000");

// Добавляем поддержку JSON
builder.Services.AddControllers();

// Добавляем поддержку CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Добавляем контекст базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        
        // Проверяем, есть ли типы запросов
        if (!dbContext.RequestTypes.Any())
        {
            // Добавляем типы запросов
            dbContext.RequestTypes.AddRange(
                new RequestType { Name = "Заявка на предоставление доступа" },
                new RequestType { Name = "Уведомление о переводе" },
                new RequestType { Name = "Заявка на прекращение доступа" }
            );
            dbContext.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при инициализации базы данных");
    }
}

// Включаем CORS
app.UseCors();

// Разрешаем использование статических файлов
app.UseStaticFiles();

// Тестовый эндпоинт для проверки подключения к БД
app.MapGet("/api/test-db", async (ApplicationDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { message = "Подключение к базе данных успешно установлено" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка подключения к базе данных: {ex.Message}");
    }
});

// Перенаправляем корневой URL на нашу страницу
app.MapGet("/", () => Results.Redirect("/index.html"));

// Добавляем обработку POST запросов для форм
app.MapPost("/api/access-request", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    Console.WriteLine("Получен запрос на /api/access-request");
    
    try
    {
        // Читаем JSON из тела запроса
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine("Полученные данные:");
        Console.WriteLine(requestBody);

        // Десериализуем JSON в динамический объект
        var formData = JsonSerializer.Deserialize<JsonElement>(requestBody);
        
        // Логируем все поля формы
        Console.WriteLine("Поля формы:");
        foreach (var property in formData.EnumerateObject())
        {
            Console.WriteLine($"{property.Name}: {property.Value}");
        }

        try
        {
            // Создаем новую заявку
            var accessRequest = new AccessRequest
            {
                FullName = formData.GetProperty("name").GetString() ?? string.Empty,
                Position = formData.GetProperty("position").GetString() ?? string.Empty,
                EmploymentDate = DateTime.SpecifyKind(DateTime.Parse(formData.GetProperty("date-of-employment").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd")), DateTimeKind.Utc),
                RequestTypeId = 1, // ID для "Заявка на предоставление доступа"
                DocumentPath = "pending", // Временное значение, будет обновлено позже
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Создана заявка: {JsonSerializer.Serialize(accessRequest)}");

            // Проверяем существование типа запроса
            var requestType = await dbContext.RequestTypes.FindAsync(1);
            if (requestType == null)
            {
                Console.WriteLine("Ошибка: Тип запроса с ID=1 не найден в базе данных");
                return Results.Problem("Тип запроса не найден в базе данных");
            }

            // Сохраняем в базу данных
            dbContext.AccessRequests.Add(accessRequest);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Ошибка сохранения в базу данных: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                throw;
            }

            return Results.Ok(new { 
                message = "Форма успешно отправлена",
                requestId = accessRequest.Id 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании заявки: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.Problem($"Ошибка при создании заявки: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при обработке формы: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Ошибка при обработке формы: {ex.Message}");
    }
});

app.MapPost("/api/transfer-notice", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    Console.WriteLine("Получен запрос на /api/transfer-notice");
    
    // Читаем JSON из тела запроса
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    Console.WriteLine("Полученные данные:");
    Console.WriteLine(requestBody);
    
    // TODO: Добавить сохранение в базу данных
    
    return Results.Ok(new { message = "Уведомление о переводе успешно отправлено" });
});

app.MapPost("/api/terminate-access", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    Console.WriteLine("Получен запрос на /api/terminate-access");
    
    // Читаем JSON из тела запроса
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    Console.WriteLine("Полученные данные:");
    Console.WriteLine(requestBody);
    
    // TODO: Добавить сохранение в базу данных
    
    return Results.Ok(new { message = "Заявка на прекращение доступа успешно отправлена" });
});

app.Run();
