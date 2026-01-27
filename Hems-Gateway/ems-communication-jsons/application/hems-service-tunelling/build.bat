@echo off
echo EC100 Provisioning Tool - EXE Olusturuluyor...
echo.

pyinstaller --clean build.spec

echo.
echo Build tamamlandi!
echo EXE dosyasi: dist\EC100-Provisioning-Tool.exe
pause
