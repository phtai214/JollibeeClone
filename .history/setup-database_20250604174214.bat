@echo off
echo ========================================
echo    JOLLIBEE CLONE - DATABASE SETUP
echo ========================================
echo.

echo [1/4] Checking if migrations exist...
if exist "Migrations" (
    echo ✓ Migrations folder found
) else (
    echo ! No migrations found, creating initial migration...
    dotnet ef migrations add InitialCreate
    if errorlevel 1 (
        echo ✗ Failed to create migration
        pause
        exit /b 1
    )
    echo ✓ Initial migration created
)

echo.
echo [2/4] Updating database...
dotnet ef database update
if errorlevel 1 (
    echo ✗ Failed to update database
    echo.
    echo Possible solutions:
    echo - Check your connection string in appsettings.json
    echo - Ensure SQL Server is running
    echo - Check if you have permissions to create database
    echo.
    pause
    exit /b 1
)

echo ✓ Database updated successfully
echo.

echo [3/4] Building the project...
dotnet build
if errorlevel 1 (
    echo ✗ Build failed
    pause
    exit /b 1
)

echo ✓ Project built successfully
echo.

echo [4/4] Setup complete!
echo.
echo Next steps:
echo 1. Run: dotnet run
echo 2. Navigate to: http://localhost:5000/Admin/Promotion/Debug
echo 3. Check the debug information
echo 4. If all green, go to: http://localhost:5000/Admin/Promotion
echo.
echo ========================================
echo          SETUP COMPLETED!
echo ========================================
pause 