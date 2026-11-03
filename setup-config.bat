@echo off
rem MockviewAI - cai cau hinh may dev. KHONG chua mat khau/key: ban nhap khi chay.
rem File nay chi goi setup-config.ps1 (cung thu muc) va bo qua chan script cua Windows cho rieng lan chay nay.
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-config.ps1"
echo.
pause
