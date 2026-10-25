@echo off
cd /d "%~dp0"
echo Bagimliliklar kuruluyor...
py -m pip install -r requirements.txt
echo.
echo Kurulum tamamlandi!
pause
