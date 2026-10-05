@echo off
title Sekiro Heavy Melee - Mod Builder
cd /d "%~dp0"
python build_full_mod.py
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] No se pudo ejecutar Python. Asegurate de tener Python instalado.
    pause
) else (
    echo.
    echo ==============================================
    echo  Mod empaquetado correctamente en pak01_dir.vpk
    echo ==============================================
    pause
)
