FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY KbAgent.Api.csproj .
RUN dotnet restore KbAgent.Api.csproj

COPY . .
RUN dotnet publish KbAgent.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Tesseract OCR engine + English language data, for ImageOcrTextExtractor (shells out to the `tesseract` CLI).
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KbAgent.Api.dll"]
