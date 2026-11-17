using System;
using System.Collections.Generic;
using System.Text;

namespace HuffPressStudio
{
    // One entry of the Huffman code table.
    internal sealed class CodeInfo
    {
        public int Symbol;      // byte value 0..255
        public uint Count;      // how often it occurs
        public ulong Bits;      // the code, right aligned
        public int Length;      // number of bits in the code

        // The code as text, e.g. "1011".
        public string BitString
        {
            get
            {
                char[] chars = new char[Length];
                for (int i = 0; i < Length; i++)
                {
                    int shift = Length - 1 - i;
                    chars[i] = ((Bits >> shift) & 1UL) == 1UL ? '1' : '0';
                }
                return new string(chars);
            }
        }
    }

    // Everything the UI needs to draw the charts for one file.
    internal sealed class Analysis
    {
        public uint[] Frequencies;
        public double Entropy;              // bits per byte
        public List<CodeInfo> Codes;        // sorted by Count, most frequent first
        public long TotalBytes;
        public int DistinctSymbols;
    }

    // Result of a compression run.
    internal sealed class CompressionOutcome
    {
        public byte[] Packed;
        public bool Verified;               // true when decompress(compress(x)) == x
    }

    // Thrown when the native engine reports an error code.
    internal sealed class EngineException : Exception
    {
        public int Code { get; private set; }

        public EngineException(int code, string message) : base(message)
        {
            Code = code;
        }
    }
}
