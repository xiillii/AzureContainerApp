#!/usr/bin/env python3
import subprocess
import sys

# Try to query the database
try:
    result = subprocess.run([
        'docker', 'exec', 'poc-sqlserver',
        '/opt/mssql-tools18/bin/sqlcmd',
        '-S', 'localhost',
        '-U', 'sa',
        '-P', 'P@ssw0rd123!',
        '-C',
        '-Q', 'SELECT Id, Username, Email, Role FROM TasksDb.dbo.Users; SELECT Id, Username, Email, Role FROM FilesDb.dbo.Users;'
    ], capture_output=True, text=True)
    
    print("=== TasksDb and FilesDb Users ===")
    print(result.stdout)
    if result.stderr:
        print("STDERR:", result.stderr)
    print("\nExit code:", result.returncode)
    
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)
