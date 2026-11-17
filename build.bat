@echo off
setlocal
title HuffPress Studio - build and run
cd /d "%~dp0"

echo.
echo ===== Step 1 of 3: building the C++ engine (huffman.dll) =====
set "OUT=%~dp0App\huffman.dll"

where cl >nul 2>nul
if errorlevel 1 call :find_visual_studio

where cl >nul 2>nul
if not errorlevel 1 goto build_with_msvc

where g++ >nul 2>nul
if not errorlevel 1 goto build_with_gpp

echo.
echo ERROR: no C++ compiler found.
echo   Install "Desktop development with C++" from the Visual Studio Installer
echo   (the free Community edition is fine), or install MinGW-w64 (64-bit g++).
goto failed

:build_with_msvc
echo Using the Microsoft C++ compiler.
pushd Native
cl /nologo /O2 /LD /W3 huffman.cpp /Fe:"%OUT%"
set "RC=%errorlevel%"
del /q huffman.obj huffman.lib huffman.exp 2>nul
popd
if not "%RC%"=="0" goto failed
goto build_app

:build_with_gpp
echo Using g++ (MinGW-w64).
g++ -O2 -Wall -shared -static-libgcc -static-libstdc++ -o "%OUT%" Native\huffman.cpp
if errorlevel 1 goto failed
goto build_app

:build_app
echo.
echo ===== Step 2 of 3: building the C# app =====
where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: the .NET 8 SDK was not found. Get it from https://dotnet.microsoft.com/download
    goto failed
)
dotnet build App\HuffPressStudio.csproj -c Release -nologo
if errorlevel 1 goto failed

echo.
echo ===== Step 3 of 3: starting HuffPress Studio =====
start "" "App\bin\Release\net8.0-windows\HuffPressStudio.exe"
goto end

:find_visual_studio
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" exit /b 1
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSPATH=%%i"
if not defined VSPATH exit /b 1
call "%VSPATH%\VC\Auxiliary\Build\vcvars64.bat" >nul
exit /b 0

:failed
echo.
echo Build failed - see the messages above.
pause
exit /b 1

:end
endlocal
