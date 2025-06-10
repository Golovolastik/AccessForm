var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Разрешаем использование статических файлов
app.UseStaticFiles();

// Перенаправляем корневой URL на нашу страницу
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
