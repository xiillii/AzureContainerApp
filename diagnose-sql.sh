#!/bin/bash

echo "========================================="
echo "Diagnóstico de SQL Server"
echo "========================================="

# Limpiar contenedores previos
echo ""
echo "1. Limpiando contenedores previos..."
docker-compose down -v

# Levantar SQL Server
echo ""
echo "2. Levantando SQL Server (Azure SQL Edge para ARM64)..."
docker-compose up -d sqlserver

# Esperar
echo ""
echo "3. Esperando 30 segundos para que SQL Server inicie..."
for i in {30..1}; do
    echo -ne "   Esperando $i segundos...\r"
    sleep 1
done
echo ""

# Verificar contenedor
echo ""
echo "4. Estado del contenedor:"
docker ps --filter "name=poc-sqlserver"

# Ver logs
echo ""
echo "5. Últimos 30 líneas de logs:"
docker logs poc-sqlserver 2>&1 | tail -30

# Intentar conectar
echo ""
echo "6. Intentando conectar a SQL Server..."
docker exec poc-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "P@ssw0rd123!" -Q "SELECT @@VERSION" 2>&1 || echo "   ❌ No se pudo conectar"

echo ""
echo "========================================="
echo "Diagnóstico completado"
echo "========================================="
