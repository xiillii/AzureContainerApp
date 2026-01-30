# ⚠️ Nota para Usuarios de Apple Silicon (M1/M2/M3)

Este proyecto usa **Azure SQL Edge** en lugar de SQL Server estándar porque SQL Server no tiene imagen nativa para ARM64.

## Diferencias con SQL Server

Azure SQL Edge es 100% compatible con SQL Server para desarrollo y tiene las mismas capacidades que necesitamos:
- ✅ T-SQL completo
- ✅ Entity Framework Core
- ✅ Migraciones
- ✅ Funciona idéntico en desarrollo

## Cambios Realizados

En `docker-compose.yml`:
```yaml
sqlserver:
  image: mcr.microsoft.com/azure-sql-edge:latest  # En lugar de mssql/server
  platform: linux/arm64
  environment:
    - ACCEPT_EULA=1  # En lugar de Y
```

## Para Usuarios de Intel/AMD

Si estás en un Mac Intel o Windows/Linux x64, puedes usar SQL Server estándar:

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    - ACCEPT_EULA=Y
    - SA_PASSWORD=P@ssw0rd123!
```

## Despliegue a Azure desde Apple Silicon

Azure Container Apps requiere imágenes `linux/amd64`. Al construir desde un Mac con Apple Silicon:

```bash
# Agregar --platform linux/amd64 a todos los builds
docker build --platform linux/amd64 -t myimage:latest .
```

Docker usará emulación para construir la imagen en la arquitectura correcta. El proceso será más lento pero garantiza compatibilidad con Azure.

## Azurite y Versiones de API

Si encuentras errores sobre versiones de API no soportadas:

```bash
# En docker-compose.yml
command: "azurite --blobHost 0.0.0.0 --skipApiVersionCheck -l /data"
```

Esto permite que los SDKs más recientes de Azure Storage funcionen con versiones anteriores de Azurite.

## Referencias

- [Azure SQL Edge en Docker](https://hub.docker.com/_/microsoft-azure-sql-edge)
- [SQL Server en Docker](https://hub.docker.com/_/microsoft-mssql-server)
