using Microsoft.EntityFrameworkCore;
using Npgsql;

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

var app = builder.Build();

// Включаем CORS
app.UseCors();

// Разрешаем использование статических файлов
app.UseStaticFiles();

// Тестовый эндпоинт для проверки подключения к БД
app.MapGet("/api/test-db", async () =>
{
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
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
app.MapPost("/api/access-request", async (HttpContext context) =>
{
    Console.WriteLine("Получен запрос на /api/access-request");
    var form = await context.Request.ReadFormAsync();
    Console.WriteLine("Данные формы:");
    foreach (var key in form.Keys)
    {
        Console.WriteLine($"{key}: {form[key]}");
    }
    return Results.Ok(new { message = "Форма успешно отправлена" });
});

app.MapPost("/api/transfer-notice", async (HttpContext context) =>
{
    Console.WriteLine("Получен запрос на /api/transfer-notice");
    var form = await context.Request.ReadFormAsync();
    Console.WriteLine("Данные формы:");
    foreach (var key in form.Keys)
    {
        Console.WriteLine($"{key}: {form[key]}");
    }
    return Results.Ok(new { message = "Уведомление о переводе успешно отправлено" });
});

app.MapPost("/api/terminate-access", async (HttpContext context) =>
{
    Console.WriteLine("Получен запрос на /api/terminate-access");
    var form = await context.Request.ReadFormAsync();
    Console.WriteLine("Данные формы:");
    foreach (var key in form.Keys)
    {
        Console.WriteLine($"{key}: {form[key]}");
    }
    return Results.Ok(new { message = "Заявка на прекращение доступа успешно отправлена" });
});

app.Run();
