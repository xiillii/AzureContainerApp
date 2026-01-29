#!/bin/bash

echo "Testing SQL Server connection..."

cd srcs/backends/TasksApi

echo "Attempting to update TasksDb..."
dotnet ef database update 2>&1

echo ""
echo "Exit code: $?"

cd ../FilesApi

echo ""
echo "Attempting to update FilesDb..."
dotnet ef database update 2>&1

echo ""
echo "Exit code: $?"
