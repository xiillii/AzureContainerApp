using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FileProcessorJob.Data;
using Shared.Models;

// Configuración
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

// Logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("🚀 File Processor Job iniciado");


try
{
    // .NET convierte AzureStorage__ConnectionString a AzureStorage:ConnectionString
    var connectionString = configuration["AzureStorage:ConnectionString"]
        ?? throw new InvalidOperationException("AzureStorage:ConnectionString no configurado");
    
    var containerName = configuration["AzureStorage:ContainerName"] ?? "files";
    
    var dbConnectionString = configuration["ConnectionStrings:DefaultConnection"]
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado");
    
    // Configurar DbContext
    logger.LogInformation("Conectando a base de datos...");
    var optionsBuilder = new DbContextOptionsBuilder<FilesDbContext>();
    optionsBuilder.UseSqlServer(dbConnectionString);
    var dbContext = new FilesDbContext(optionsBuilder.Options);

    logger.LogInformation("Conectando a Storage Account - Container: {ContainerName}", containerName);

    var blobServiceClient = new BlobServiceClient(connectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

    // Verificar que el contenedor existe
    if (!await containerClient.ExistsAsync())
    {
        logger.LogWarning("El contenedor '{ContainerName}' no existe", containerName);
        return;
    }

    logger.LogInformation("📂 Listando archivos sin procesar...");

    var processedCount = 0;
    var skippedCount = 0;

    // Listar todos los blobs
    await foreach (var blobItem in containerClient.GetBlobsAsync())
    {
        logger.LogInformation(" - Blob: {BlobName}, Tamaño: {BlobSize} bytes", blobItem.Name, blobItem.Properties.ContentLength);
        
        // Verificar si ya está procesado (tiene el sufijo)
        if (blobItem.Name.Contains("-processed"))
        {
            logger.LogInformation("⏭️  Archivo ya procesado: {FileName}", blobItem.Name);
            skippedCount++;
            continue;
        }

        logger.LogInformation("📄 Procesando archivo: {FileName}", blobItem.Name);

        // Crear nombre del archivo procesado
        var extension = Path.GetExtension(blobItem.Name);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(blobItem.Name);
        var processedFileName = $"{nameWithoutExtension}-processed-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";

        // Copiar el blob
        var sourceBlobClient = containerClient.GetBlobClient(blobItem.Name);
        var destBlobClient = containerClient.GetBlobClient(processedFileName);

        var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        await copyOperation.WaitForCompletionAsync();

        // Eliminar el archivo original
        await sourceBlobClient.DeleteAsync();

        // Actualizar metadata en la base de datos
        var metadata = await dbContext.Files.FirstOrDefaultAsync(f => f.BlobName == blobItem.Name);
        if (metadata != null)
        {
            metadata.BlobName = processedFileName;
            metadata.FileName = processedFileName; // También actualizar el nombre visible
            await dbContext.SaveChangesAsync();
            logger.LogInformation("✅ Archivo procesado y metadata actualizada: {FileName} -> {ProcessedFileName}", 
                blobItem.Name, processedFileName);
        }
        else
        {
            logger.LogWarning("⚠️  Metadata no encontrada en DB para: {FileName}", blobItem.Name);
        }
        
        processedCount++;
    }

    logger.LogInformation(
        "✨ Procesamiento completado. Archivos procesados: {ProcessedCount}, Omitidos: {SkippedCount}",
        processedCount, skippedCount);
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Error durante el procesamiento de archivos");
    Environment.Exit(1);
}

logger.LogInformation("👋 File Processor Job finalizado");
