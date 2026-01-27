@echo off
cd /d "%~dp0"
echo EC100 Provisioning Tool Baslatiliyor...
echo.

py --version
if errorlevel 1 (
    echo.
    echo HATA: Python bulunamadi!
    echo Python 3.10+ yukleyin.
    echo.
    pause
    exit /b 1
)

echo.
echo Uygulama baslatiliyor...
py main.py

if errorlevel 1 (
    echo.
    echo HATA: Uygulama calistirilirken bir sorun olustu.
    echo.
)
pause
