@echo off
REM Ensamblar y enlazar con MASM32 (compatible con rutas que tienen espacios)
setlocal EnableExtensions EnableDelayedExpansion

if "%MASM32%"=="" set "MASM32=C:\masm32"

set "ASM=%MASM32%\bin\ml.exe"
set "MLINK=%MASM32%\bin\link.exe"
set "POLINK=%MASM32%\bin\PoLink.exe"
set "LIBDIR=%MASM32%\lib"

pushd "%~dp0"

if not exist "output.asm" (
    echo Primero genera output.asm:
    echo   quechuac.exe quechua.txt -o output.asm
    popd
    exit /b 1
)

if not exist "%ASM%" (
    echo No se encontro MASM32 en %MASM32%
    popd
    exit /b 1
)

if not exist "%LIBDIR%\msvcrt.lib" (
    echo No se encontro %LIBDIR%\msvcrt.lib
    popd
    exit /b 1
)

if exist output.obj del /f /q output.obj
if exist output.exe del /f /q output.exe

echo [1/2] Ensamblando output.asm ...
"%ASM%" /c /coff /nologo /Fooutput.obj output.asm
if errorlevel 1 (
    popd
    exit /b 1
)

if not exist output.obj (
    echo ERROR: no se genero output.obj
    popd
    exit /b 1
)

echo [2/2] Enlazando output.obj ...

REM No usar variable LINK: link.exe lee la env LINK como opciones extra (LNK1136).
if exist "%POLINK%" (
    "%POLINK%" /SUBSYSTEM:CONSOLE /OUT:output.exe /LIBPATH:"%LIBDIR%" output.obj msvcrt.lib kernel32.lib
) else (
    "%MLINK%" /SUBSYSTEM:CONSOLE /nologo /LIBPATH:"%LIBDIR%" /OUT:output.exe output.obj msvcrt.lib kernel32.lib
)
set "LINK_ERR=!ERRORLEVEL!"

if not "!LINK_ERR!"=="0" (
    echo Reintentando con link.exe ...
    set "LINK="
    "%MLINK%" /SUBSYSTEM:CONSOLE /nologo /LIBPATH:"%LIBDIR%" /OUT:output.exe output.obj msvcrt.lib kernel32.lib
    set "LINK_ERR=!ERRORLEVEL!"
)

if not exist output.exe (
    echo.
    echo ERROR al enlazar. Comprueba MASM32 en: %MASM32%
    echo   %LIBDIR%\msvcrt.lib
    echo   %LIBDIR%\kernel32.lib
    popd
    exit /b 1
)

echo.
echo Listo: output.exe
popd
endlocal
