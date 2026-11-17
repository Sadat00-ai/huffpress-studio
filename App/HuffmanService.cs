using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HuffPressStudio
{
    // Wrapper around the native engine. It allocates the
    // buffers, checks error codes and converts results into plain C# objects.
    internal static class HuffmanService
    {
        public const int HeaderSize = 1032;     // must match HP_HEADER_SIZE in huffman.h
        private const int SymbolCount = 256;

        public static string EngineVersion()
        {
            return Marshal.PtrToStringAnsi(NativeMethods.HP_Version());
        }

        public static uint[] CountFrequencies(byte[] data)
        {
            uint[] freq = new uint[SymbolCount];
            Check(NativeMethods.HP_CountFrequencies(data, data.Length, freq));
            return freq;
        }

        // Builds the code table and the entropy from a frequency table.
        public static Analysis Analyze(uint[] freq)
        {
            ulong[] codes = new ulong[SymbolCount];
            byte[] lengths = new byte[SymbolCount];
            int distinct = Check(NativeMethods.HP_BuildCodes(freq, codes, lengths));

            List<CodeInfo> list = new List<CodeInfo>();
            long total = 0;
            for (int s = 0; s < SymbolCount; s++)
            {
                total += freq[s];
                if (lengths[s] == 0) continue;
                CodeInfo info = new CodeInfo();
                info.Symbol = s;
                info.Count = freq[s];
                info.Bits = codes[s];
                info.Length = lengths[s];
                list.Add(info);
            }
            list.Sort(CompareByCountDescending);

            Analysis result = new Analysis();
            result.Frequencies = freq;
            result.Entropy = NativeMethods.HP_Entropy(freq);
            result.Codes = list;
            result.TotalBytes = total;
            result.DistinctSymbols = distinct;
            return result;
        }

        public static byte[] Compress(byte[] data)
        {
            int size = Check(NativeMethods.HP_CompressedSize(data, data.Length));
            byte[] output = new byte[size];
            int written = Check(NativeMethods.HP_Compress(data, data.Length, output, output.Length));
            if (written != size) Array.Resize(ref output, written);
            return output;
        }

        public static byte[] Decompress(byte[] packed)
        {
            int original = Check(NativeMethods.HP_OriginalSize(packed, packed.Length));
            byte[] output = new byte[original];
            Check(NativeMethods.HP_Decompress(packed, packed.Length, output, output.Length));
            return output;
        }

        // Compresses, then decompresses again to check that nothing was lost.
        public static CompressionOutcome CompressAndVerify(byte[] data)
        {
            CompressionOutcome outcome = new CompressionOutcome();
            outcome.Packed = Compress(data);
            byte[] restored = Decompress(outcome.Packed);
            outcome.Verified = BytesEqual(data, restored);
            return outcome;
        }

        // True when the bytes start with the .hpz magic and have a full header.
        public static bool LooksPacked(byte[] data)
        {
            return data.Length >= HeaderSize
                && data[0] == (byte)'H' && data[1] == (byte)'P'
                && data[2] == (byte)'Z' && data[3] == (byte)'1';
        }

        // Reads the byte-frequency table stored in a .hpz header.
        public static uint[] ReadHeaderFrequencies(byte[] packed)
        {
            uint[] freq = new uint[SymbolCount];
            for (int s = 0; s < SymbolCount; s++)
            {
                freq[s] = BitConverter.ToUInt32(packed, 8 + s * 4);   // little endian, same as the C++ side
            }
            return freq;
        }

        // helpers

        private static int CompareByCountDescending(CodeInfo a, CodeInfo b)
        {
            int c = b.Count.CompareTo(a.Count);
            return c != 0 ? c : a.Symbol.CompareTo(b.Symbol);
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static int Check(int code)
        {
            if (code < 0) throw new EngineException(code, Describe(code));
            return code;
        }

        private static string Describe(int code)
        {
            switch (code)
            {
                case -1: return "Invalid argument passed to the engine.";
                case -2: return "The output buffer was too small.";
                case -3: return "This is not a valid HuffPress (.hpz) file.";
                case -4: return "The compressed data is damaged or incomplete.";
                default: return "Unknown engine error (" + code + ").";
            }
        }
    }
}
