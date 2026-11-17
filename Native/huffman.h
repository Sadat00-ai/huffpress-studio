// huffman.h - Public API of the HuffPress native engine (plain C interface).
// Kept as simple extern "C" functions so C# can call them through P/Invoke.
#ifndef HUFFMAN_H
#define HUFFMAN_H

#ifdef _WIN32
  #define HP_API extern "C" __declspec(dllexport)
#else
  #define HP_API extern "C" __attribute__((visibility("default")))
#endif

// Error codes (returned as negative numbers).
#define HP_OK             0
#define HP_ERR_BAD_ARG   -1   // null pointer or negative length
#define HP_ERR_BUFFER    -2   // output buffer too small
#define HP_ERR_FORMAT    -3   // input is not a valid .hpz stream
#define HP_ERR_CORRUPT   -4   // stream ended early / bad data

// Size of the .hpz header: magic(4) + originalSize(4) + 256 frequencies(4 each).
#define HP_HEADER_SIZE 1032

// Counts how many times each byte value (0..255) appears. freq must hold 256 values.
HP_API int HP_CountFrequencies(const unsigned char* data, int length, unsigned int* freq);

// Shannon entropy in bits per byte (0.0 .. 8.0) for the given frequency table.
HP_API double HP_Entropy(const unsigned int* freq);

// Builds Huffman codes. codes[i] holds the bits (right-aligned), lengths[i] the bit count.
// Symbols that never occur get length 0. Returns the number of distinct symbols.
HP_API int HP_BuildCodes(const unsigned int* freq, unsigned long long* codes, unsigned char* lengths);

// Exact number of bytes HP_Compress will produce for this data (header included).
HP_API int HP_CompressedSize(const unsigned char* data, int length);

// Compresses data into out. Returns bytes written, or a negative error code.
HP_API int HP_Compress(const unsigned char* data, int length, unsigned char* out, int outCapacity);

// Reads the original size stored in a .hpz header. Returns it, or a negative error code.
HP_API int HP_OriginalSize(const unsigned char* packed, int packedLength);

// Decompresses a .hpz stream into out. Returns bytes written, or a negative error code.
HP_API int HP_Decompress(const unsigned char* packed, int packedLength, unsigned char* out, int outCapacity);

// Version string, handy for checking that the DLL loaded correctly.
HP_API const char* HP_Version();

#endif
