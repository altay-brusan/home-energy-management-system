@echo off
echo ==========================================
echo EC100 Provisioning Tool - Kurulum
echo ==========================================
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
echo Bagimliliklar yukleniyor...
echo.

py -m pip install PyQt6 paramiko cryptography pyinstaller

if errorlevel 1 (
    echo.
    echo HATA: Bagimliliklar yuklenirken sorun olustu.
    echo.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo Kurulum basariyla tamamlandi!
echo ==========================================
echo.
echo Uygulamayi calistirmak icin: run.bat
echo.
pause
