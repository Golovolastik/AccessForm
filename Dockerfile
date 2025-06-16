FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта
COPY ["AccessForm.csproj", "./"]
RUN dotnet restore

# Копируем остальные файлы
COPY . .
RUN dotnet build -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Копируем файл шаблона
COPY doc-template.docx .
# Создаем директорию для сгенерированных документов
RUN mkdir -p GeneratedDocuments
ENTRYPOINT ["dotnet", "AccessForm.dll"] 