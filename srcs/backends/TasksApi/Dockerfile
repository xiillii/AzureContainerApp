FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared models project
COPY ["shared/Shared.Models/Shared.Models.csproj", "shared/Shared.Models/"]

# Copy TasksApi project
COPY ["backends/TasksApi/TasksApi.csproj", "backends/TasksApi/"]

# Restore dependencies
RUN dotnet restore "backends/TasksApi/TasksApi.csproj"

# Copy all source files
COPY ["shared/Shared.Models/", "shared/Shared.Models/"]
COPY ["backends/TasksApi/", "backends/TasksApi/"]

# Build
WORKDIR "/src/backends/TasksApi"
RUN dotnet build "TasksApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TasksApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "TasksApi.dll"]
