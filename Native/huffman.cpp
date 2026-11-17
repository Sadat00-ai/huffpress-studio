// huffman.cpp - Huffman coding engine written with basic C++ only
// (arrays, structs, loops, functions - no STL containers, no classes).
#include "huffman.h"
#include <math.h>

#define SYMBOLS      256
#define MAX_NODES    (SYMBOLS * 2)     // a full binary tree with 256 leaves has 511 nodes
#define MAX_CODE_LEN 64                // codes are stored in 64-bit integers

// ---------------------------------------------------------------- tree

struct Node {
    unsigned long long weight;   // sum of frequencies below this node
    int left;                    // index of left child, -1 for a leaf
    int right;                   // index of right child, -1 for a leaf
    int symbol;                  // byte value for leaves, -1 for inner nodes
};

struct Tree {
    Node nodes[MAX_NODES];
    int  count;                  // nodes used so far
    int  root;                   // index of the root node
};

static int addNode(Tree* t, unsigned long long weight, int left, int right, int symbol) {
    int i = t->count++;
    t->nodes[i].weight = weight;
    t->nodes[i].left   = left;
    t->nodes[i].right  = right;
    t->nodes[i].symbol = symbol;
    return i;
}

// Finds the active node with the smallest weight (ties: lowest index, so the
// result is the same every time and encoder/decoder always agree).
static int findSmallest(const Tree* t, const bool* active) {
    int best = -1;
    for (int i = 0; i < t->count; i++) {
        if (!active[i]) continue;
        if (best == -1 || t->nodes[i].weight < t->nodes[best].weight) best = i;
    }
    return best;
}

// Builds the Huffman tree from a frequency table. Returns the number of distinct symbols.
static int buildTree(const unsigned int* freq, Tree* t) {
    bool active[MAX_NODES];
    int distinct = 0;

    t->count = 0;
    t->root  = -1;
    for (int i = 0; i < MAX_NODES; i++) active[i] = false;

    for (int s = 0; s < SYMBOLS; s++) {
        if (freq[s] > 0) {
            int n = addNode(t, freq[s], -1, -1, s);
            active[n] = true;
            distinct++;
        }
    }
    if (distinct == 0) return 0;

    int remaining = distinct;
    while (remaining > 1) {
        int a = findSmallest(t, active);  active[a] = false;
        int b = findSmallest(t, active);  active[b] = false;
        int p = addNode(t, t->nodes[a].weight + t->nodes[b].weight, a, b, -1);
        active[p] = true;
        remaining--;
    }
    t->root = findSmallest(t, active);
    return distinct;
}

// Walks the tree and records the path to every leaf (left = 0, right = 1).
static void assignCodes(const Tree* t, int node, unsigned long long code, int depth,
                        unsigned long long* codes, unsigned char* lengths) {
    const Node* n = &t->nodes[node];
    if (n->left == -1) {                              // leaf
        codes[n->symbol]   = code;
        lengths[n->symbol] = (unsigned char)(depth == 0 ? 1 : depth);  // lone symbol still needs 1 bit
        return;
    }
    if (depth >= MAX_CODE_LEN) return;                // cannot happen for 32-bit counts
    assignCodes(t, n->left,  code << 1,       depth + 1, codes, lengths);
    assignCodes(t, n->right, (code << 1) | 1, depth + 1, codes, lengths);
}

// ---------------------------------------------------------------- bit I/O

struct BitWriter {
    unsigned char* buffer;
    int capacity;
    int bytePos;
    int bitCount;       // bits already used in the current byte (0..7)
};

static void writerInit(BitWriter* w, unsigned char* buffer, int capacity) {
    w->buffer = buffer; w->capacity = capacity; w->bytePos = 0; w->bitCount = 0;
}

static void writeByte(BitWriter* w, unsigned char value) {
    w->buffer[w->bytePos++] = value;
}

static void writeUInt32(BitWriter* w, unsigned int v) {   // little endian
    for (int i = 0; i < 4; i++) writeByte(w, (unsigned char)((v >> (8 * i)) & 0xFF));
}

static void writeBits(BitWriter* w, unsigned long long code, int length) {
    for (int i = length - 1; i >= 0; i--) {               // most significant bit first
        if (w->bitCount == 0) w->buffer[w->bytePos] = 0;
        if ((code >> i) & 1ULL) w->buffer[w->bytePos] |= (unsigned char)(0x80 >> w->bitCount);
        w->bitCount++;
        if (w->bitCount == 8) { w->bitCount = 0; w->bytePos++; }
    }
}

static void writerFlush(BitWriter* w) {
    if (w->bitCount > 0) { w->bitCount = 0; w->bytePos++; }
}

static unsigned int readUInt32(const unsigned char* p) {
    return (unsigned int)p[0] | ((unsigned int)p[1] << 8) |
           ((unsigned int)p[2] << 16) | ((unsigned int)p[3] << 24);
}

// ---------------------------------------------------------------- public API

HP_API const char* HP_Version() { return "HuffPress engine 1.0"; }

HP_API int HP_CountFrequencies(const unsigned char* data, int length, unsigned int* freq) {
    if (freq == 0 || length < 0 || (data == 0 && length > 0)) return HP_ERR_BAD_ARG;
    for (int i = 0; i < SYMBOLS; i++) freq[i] = 0;
    for (int i = 0; i < length; i++) freq[data[i]]++;
    return HP_OK;
}

HP_API double HP_Entropy(const unsigned int* freq) {
    if (freq == 0) return 0.0;
    double total = 0.0;
    for (int i = 0; i < SYMBOLS; i++) total += freq[i];
    if (total == 0.0) return 0.0;
    double entropy = 0.0;
    for (int i = 0; i < SYMBOLS; i++) {
        if (freq[i] == 0) continue;
        double p = freq[i] / total;
        entropy -= p * (log(p) / log(2.0));
    }
    return entropy;
}

HP_API int HP_BuildCodes(const unsigned int* freq, unsigned long long* codes, unsigned char* lengths) {
    if (freq == 0 || codes == 0 || lengths == 0) return HP_ERR_BAD_ARG;
    for (int i = 0; i < SYMBOLS; i++) { codes[i] = 0; lengths[i] = 0; }

    Tree tree;
    int distinct = buildTree(freq, &tree);
    if (distinct > 0) assignCodes(&tree, tree.root, 0ULL, 0, codes, lengths);
    return distinct;
}

HP_API int HP_CompressedSize(const unsigned char* data, int length) {
    unsigned int freq[SYMBOLS];
    unsigned long long codes[SYMBOLS];
    unsigned char lengths[SYMBOLS];

    int rc = HP_CountFrequencies(data, length, freq);
    if (rc != HP_OK) return rc;
    HP_BuildCodes(freq, codes, lengths);

    unsigned long long bits = 0;
    for (int s = 0; s < SYMBOLS; s++) bits += (unsigned long long)freq[s] * lengths[s];
    unsigned long long total = HP_HEADER_SIZE + (bits + 7) / 8;
    if (total > 2147483647ULL) return HP_ERR_BUFFER;
    return (int)total;
}

HP_API int HP_Compress(const unsigned char* data, int length, unsigned char* out, int outCapacity) {
    if (out == 0 || length < 0 || (data == 0 && length > 0)) return HP_ERR_BAD_ARG;

    int needed = HP_CompressedSize(data, length);
    if (needed < 0) return needed;
    if (outCapacity < needed) return HP_ERR_BUFFER;

    unsigned int freq[SYMBOLS];
    unsigned long long codes[SYMBOLS];
    unsigned char lengths[SYMBOLS];
    HP_CountFrequencies(data, length, freq);
    HP_BuildCodes(freq, codes, lengths);

    BitWriter w;
    writerInit(&w, out, outCapacity);

    // Header: magic "HPZ1", original size, then the frequency table.
    writeByte(&w, 'H'); writeByte(&w, 'P'); writeByte(&w, 'Z'); writeByte(&w, '1');
    writeUInt32(&w, (unsigned int)length);
    for (int s = 0; s < SYMBOLS; s++) writeUInt32(&w, freq[s]);

    // Body: the code of every input byte, packed bit by bit.
    for (int i = 0; i < length; i++) writeBits(&w, codes[data[i]], lengths[data[i]]);
    writerFlush(&w);
    return w.bytePos;
}

// Reads and validates the header; fills freq and returns the original size.
static int readHeader(const unsigned char* packed, int packedLength, unsigned int* freq) {
    if (packed == 0 || packedLength < 0) return HP_ERR_BAD_ARG;
    if (packedLength < HP_HEADER_SIZE)   return HP_ERR_FORMAT;
    if (packed[0] != 'H' || packed[1] != 'P' || packed[2] != 'Z' || packed[3] != '1')
        return HP_ERR_FORMAT;

    unsigned int original = readUInt32(packed + 4);
    if (original > 2147483647U) return HP_ERR_FORMAT;

    unsigned long long sum = 0;
    for (int s = 0; s < SYMBOLS; s++) {
        freq[s] = readUInt32(packed + 8 + s * 4);
        sum += freq[s];
    }
    if (sum != original) return HP_ERR_CORRUPT;       // table must match the stored size
    return (int)original;
}

HP_API int HP_OriginalSize(const unsigned char* packed, int packedLength) {
    unsigned int freq[SYMBOLS];
    return readHeader(packed, packedLength, freq);
}

HP_API int HP_Decompress(const unsigned char* packed, int packedLength, unsigned char* out, int outCapacity) {
    unsigned int freq[SYMBOLS];
    int original = readHeader(packed, packedLength, freq);
    if (original < 0) return original;
    if (out == 0 && original > 0) return HP_ERR_BAD_ARG;
    if (outCapacity < original) return HP_ERR_BUFFER;
    if (original == 0) return 0;

    Tree tree;
    int distinct = buildTree(freq, &tree);

    // Only one distinct byte value: the whole file is that byte repeated.
    if (distinct == 1) {
        unsigned char value = (unsigned char)tree.nodes[tree.root].symbol;
        for (int i = 0; i < original; i++) out[i] = value;
        return original;
    }

    int bytePos = HP_HEADER_SIZE;
    int bitPos  = 0;
    for (int i = 0; i < original; i++) {
        int node = tree.root;
        while (tree.nodes[node].left != -1) {         // walk down until a leaf is reached
            if (bytePos >= packedLength) return HP_ERR_CORRUPT;
            int bit = (packed[bytePos] >> (7 - bitPos)) & 1;
            node = bit ? tree.nodes[node].right : tree.nodes[node].left;
            if (++bitPos == 8) { bitPos = 0; bytePos++; }
        }
        out[i] = (unsigned char)tree.nodes[node].symbol;
    }
    return original;
}
