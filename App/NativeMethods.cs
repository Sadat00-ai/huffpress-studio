using System;
using System.Runtime.InteropServices;

namespace HuffPressStudio
{
    // Raw P/Invoke declarations. Every function here maps 1:1 to a function
    // exported by the C++ engine (Native/huffman.h). Nothing else in the app
    // talks to the DLL directly - it goes through HuffmanService.
    internal static class NativeMethods
    {
        private const string EngineDll = "huffman";   // resolves to huffman.dll next to the .exe

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_CountFrequencies(byte[] data, int length, uint[] freq);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double HP_Entropy(uint[] freq);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_BuildCodes(uint[] freq, ulong[] codes, byte[] lengths);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_CompressedSize(byte[] data, int length);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_Compress(byte[] data, int length, byte[] output, int outputCapacity);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_OriginalSize(byte[] packed, int packedLength);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HP_Decompress(byte[] packed, int packedLength, byte[] output, int outputCapacity);

        [DllImport(EngineDll, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr HP_Version();

        // Windows look-and-feel helpers (dark scrollbars, dark title bar)

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
