using Microsoft.EntityFrameworkCore;
using Npgsql;
using AccessForm.Models;
using System.Text.Json;
using AccessForm.Services;

var builder = WebApplication.CreateBuilder(args);

// Явно указываем порт
//builder.WebHost.UseUrls("http://+:5000");

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

// Добавляем сервис для работы с Word документами
builder.Services.AddScoped<IWordDocumentService, WordDocumentService>();

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

// Перенаправляем корневой URL на нашу страницу
app.MapGet("/", () => Results.Redirect("/index.html"));

// Настраиваем маршрутизацию контроллеров
app.MapControllers();

app.Urls.Add("http://0.0.0.0:5000");
app.Urls.Add("https://0.0.0.0:5001");


app.Run();
