# HuffPress Studio

A Huffman compression tool for Windows. The compression itself is written in C++ and built as a DLL.
The window around it is a C# WinForms app that calls the DLL. You drop a file in, and the app shows
the byte counts as a bar chart, draws the top of the Huffman tree, fills a code table and shows how
much space you saved.

I built it to practise data structures and algorithms (the Huffman tree and bit packing), calling native
code from C# (P/Invoke), and drawing my own controls. The C++ uses only basic features (structs,
arrays, loops, functions, no STL containers) and the C# uses no NuGet packages.

## What you need

Windows 10 or 11, 64-bit.

- .NET 8 SDK: https://dotnet.microsoft.com/download
- A C++ compiler. Either Visual Studio (the free Community edition, with the "Desktop development
  with C++" workload) or MinGW-w64 with 64-bit g++.

## Build and run

Double-click `build.bat`. It compiles the C++ engine to `App\huffman.dll`, builds the C# app and
starts it.

To do it by hand:

```
cd Native
cl /O2 /LD huffman.cpp /Fe:..\App\huffman.dll
cd ..
dotnet run --project App -c Release
```

With MinGW use `g++ -O2 -shared -o ..\App\huffman.dll huffman.cpp` instead of the `cl` line.

The DLL and the app must both be x64. If Windows says "wrong architecture", build the DLL again from
the x64 Native Tools Command Prompt.

To test only the C++ engine, run `Native\run_tests.bat` (Windows) or `Native/run_tests.sh`
(Linux, macOS, WSL). No window is needed for that.

## Using it

- Load a file by dragging it onto the window, clicking the drop zone or pressing Open file.
- Compress runs the C++ engine, then decompresses the result again and compares it with the
  original, so you can see that nothing was lost.
- To decompress, load a `.hpz` file and press Decompress.
- Save result writes the `.hpz` archive (or the restored file).
- Hover over a bar to see exact counts. Hover over a tree node to see its code.
- Try a sample cycles through a text file, a DNA file and a bitmap.

These are the sizes I got for the samples (archive size compared with the original):

| Sample | Original | Compressed | Size left |
|---|---|---|---|
| sample_dna.txt | 60,000 bytes | 16,032 bytes | 26.7% |
| sample_image.bmp | 192,054 bytes | 82,947 bytes | 43.2% |
| sample_text.txt | 45,138 bytes | 24,006 bytes | 53.2% |

The DNA file only uses four letters, which is why it shrinks the most. Huffman coding only helps when
some bytes are much more common than others. ZIP, JPEG and MP4 files are already compressed, so you
will usually see no gain with them, and the app says so.

## Project layout

```
build.bat                one-click build and run
Native/                  C++ engine
  huffman.h              the C interface (extern "C", so C# can call it)
  huffman.cpp            tree, code assignment, bit writer, compress, decompress
  test_huffman.cpp       console tests (empty input, one symbol, random data, damaged data)
  run_tests.bat/.sh      build and run the tests
App/                     C# .NET 8 WinForms app
  Program.cs             start-up, checks that the DLL loads
  MainForm.cs            layout and the load / analyse / compress / save workflow
  HuffmanService.cs      wrapper around the DLL: buffers and error codes
  NativeMethods.cs       the raw P/Invoke declarations
  Models.cs              CodeInfo, Analysis, CompressionOutcome, EngineException
  Theme.cs               colours, fonts, DPI scaling, drawing helpers
  ProjectAssets.cs       loads the pictures from Assets
  Controls/              custom drawn controls, all based on AnimatedControl
  Assets/                logo, banner, icons, drop zone picture, app icon
Samples/                 text, DNA and bitmap files to try
Tools/make_assets.py     optional, redraws the pictures (needs Pillow)
```

## How it works

1. Count how often each byte value (0 to 255) appears in the file.
2. Take the two rarest nodes and join them under a new parent. Repeat until one tree is left.
   Common bytes end up near the top (short codes) and rare bytes deep down (long codes).
3. A left step is a 0 and a right step is a 1. The path from the root to a leaf is the code for that byte.
4. Write every byte of the file as its code, packed bit by bit.

The `.hpz` format is: `HPZ1` (4 bytes), the original size (uint32), a table of 256 frequencies
(uint32 each), then the packed bits. The decoder rebuilds the same tree from the table and reads the
bits one at a time. Ties are broken the same way in the encoder and the decoder, so both build an
identical tree. Empty files, files with one repeated byte and damaged or too-short files are handled.

The entropy card shows the theoretical lowest average number of bits per byte. Huffman gets close to it.

## Things to be ready to explain

- Why the DLL has a plain C interface: classes cannot cross the DLL boundary safely, and plain
  functions are easy to marshal from C#.
- The two-step buffer pattern: `HP_CompressedSize` is called first, so C# can allocate a buffer of
  exactly the right size before calling `HP_Compress`.
- Errors are returned as negative codes instead of exceptions, because exceptions should not cross
  the DLL boundary. The engine also checks sizes and bounds, and every compression is verified by
  decompressing it again.
- The controls are drawn by hand. They use double buffering, a 30 fps timer and design pixels so that
  the window looks the same at any Windows DPI scale.

## Ideas for later

- Canonical Huffman codes, so the header shrinks from 1 KB to 256 bytes.
- Progress reporting through a callback from the native code.
- Archives with several files.
- LZ77 followed by Huffman, which is how DEFLATE works.
