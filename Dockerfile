FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта
COPY ["AccessForm.csproj", "./"]
RUN dotnet restore "AccessForm.csproj"

# Копируем остальные файлы
COPY . .
RUN dotnet build "AccessForm.csproj" -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
RUN dotnet publish "AccessForm.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Копируем файл шаблона
COPY doc-template.docx .
# Создаем директорию для сгенерированных документов
RUN mkdir -p /app/GeneratedDocuments && \
    chmod 777 /app/GeneratedDocuments

# Установка LibreOffice Writer и шрифта Times New Roman
RUN apt-get update && \
    apt-get install -y software-properties-common && \
    echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections && \
    apt-get install -y --no-install-recommends \
    libreoffice-writer \
    fontconfig \
    cabextract \
    wget && \
    wget https://downloads.sourceforge.net/corefonts/times32.exe && \
    mkdir -p /usr/share/fonts/truetype/msttcorefonts && \
    cabextract -d /usr/share/fonts/truetype/msttcorefonts times32.exe && \
    fc-cache -f -v && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    rm -f *.exe

# Устанавливаем правильные права на директорию с документами
RUN chown -R root:root /app/GeneratedDocuments && \
    chmod -R 777 /app/GeneratedDocuments

ENTRYPOINT ["dotnet", "AccessForm.dll"] 