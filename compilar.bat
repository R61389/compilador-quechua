@echo off
REM Uso: compilar.bat  (compila y ejecuta quechua.txt en esta carpeta)
setlocal EnableExtensions

cls
set "DIR=%~dp0"
pushd "%DIR%"

set "ENTRADA=quechua.txt"
set "LOG=%DIR%compilacion.log"

if not exist "%ENTRADA%" (
    echo No existe: %ENTRADA%
    popd
    exit /b 1
)

if "%MASM32%"=="" set "MASM32=C:\masm32"

echo.
echo ============================================================
echo  QUECHUAC - Compilador Quechua
echo  Archivo: %ENTRADA%
echo ============================================================
echo.

REM Una sola salida: evita tablas repetidas o mezcladas en la consola
quechuac.exe "%ENTRADA%" -o output.asm -v > "%LOG%" 2>&1
set "QCERR=%ERRORLEVEL%"
type "%LOG%"
if not "%QCERR%"=="0" (
    echo.
    echo Compilacion fallida. Revisa los errores arriba.
    popd
    exit /b 1
)

echo.
echo --- FASE 5: ENSAMBLADO Y ENLACE (MASM32) ---
call "%DIR%build_masm.bat"
if errorlevel 1 (
    popd
    exit /b 1
)

echo.
echo --- SALIDA FINAL EJECUTADA ---
"%DIR%output.exe"
echo.
echo ============================================================
popd
endlocal
