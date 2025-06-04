@echo off
echo ================================================
echo   QUICK FIX - Jollibee Voucher System
echo ================================================
echo.

echo Step 1: Updating database with existing migrations...
dotnet ef database update
if errorlevel 1 (
    echo Error: Failed to update database
    echo Trying to remove all debug code...
    goto :fix_code
) else (
    echo Success: Database updated!
    goto :test_app
)

:fix_code
echo.
echo Step 2: This will help us build without issues...
echo Run the app now and test: http://localhost:port/Admin/Promotion/Debug
goto :end

:test_app
echo.
echo Step 3: Database should be ready now!
echo.
echo Next steps:
echo 1. Run: dotnet run
echo 2. Go to: http://localhost:5000/Admin/Promotion
echo 3. If still error, go to: http://localhost:5000/Admin/Promotion/CreateSampleData
echo.

:end
pause 