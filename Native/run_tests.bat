@echo off
rem Builds and runs the console tests for the C++ engine (no GUI needed).
cd /d "%~dp0"
where cl >nul 2>nul
if not errorlevel 1 (
    cl /nologo /O2 /EHsc huffman.cpp test_huffman.cpp /Fe:test_huffman.exe && test_huffman.exe
) else (
    g++ -O2 -Wall -o test_huffman.exe huffman.cpp test_huffman.cpp && test_huffman.exe
)
del /q *.obj 2>nul
