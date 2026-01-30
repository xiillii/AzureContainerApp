FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY shared/Shared.Models/Shared.Models.csproj shared/Shared.Models/
COPY backends/FileProcessorJob/FileProcessorJob.csproj backends/FileProcessorJob/
RUN dotnet restore backends/FileProcessorJob/FileProcessorJob.csproj

# Copiar el resto del código y construir
COPY shared/Shared.Models/ shared/Shared.Models/
COPY backends/FileProcessorJob/ backends/FileProcessorJob/
WORKDIR /src/backends/FileProcessorJob
RUN dotnet publish -c Release -o /app/publish

# Imagen final
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FileProcessorJob.dll"]
