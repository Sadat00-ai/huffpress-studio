// test_huffman.cpp - small console test so the engine can be verified without the GUI.
#include "huffman.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int failures = 0;
static void check(bool ok, const char* name) {
    printf("  [%s] %s\n", ok ? "PASS" : "FAIL", name);
    if (!ok) failures++;
}

static bool roundTrip(const unsigned char* data, int len, int* packedOut) {
    int size = HP_CompressedSize(data, len);
    if (size < 0) return false;
    unsigned char* packed = (unsigned char*)malloc(size > 0 ? size : 1);
    int written = HP_Compress(data, len, packed, size);
    if (written != size) { free(packed); return false; }
    if (packedOut) *packedOut = written;

    int original = HP_OriginalSize(packed, written);
    unsigned char* back = (unsigned char*)malloc(original > 0 ? original : 1);
    int got = HP_Decompress(packed, written, back, original);
    bool ok = (got == len) && (len == 0 || memcmp(back, data, len) == 0);
    free(packed); free(back);
    return ok;
}

int main() {
    printf("%s\n", HP_Version());

    const char* text = "this is an example of a huffman tree, and it should shrink nicely";
    int packed = 0;
    check(roundTrip((const unsigned char*)text, (int)strlen(text), &packed), "text round trip");

    check(roundTrip((const unsigned char*)"", 0, 0), "empty input");
    check(roundTrip((const unsigned char*)"a", 1, 0), "single byte");

    unsigned char same[5000]; memset(same, 'x', sizeof(same));
    check(roundTrip(same, sizeof(same), &packed), "one repeated symbol");
    printf("     5000 bytes of 'x' -> %d bytes\n", packed);

    unsigned char all[256 * 20];
    for (int i = 0; i < (int)sizeof(all); i++) all[i] = (unsigned char)(i % 256);
    check(roundTrip(all, sizeof(all), 0), "all 256 symbols");

    unsigned char rnd[100000];
    srand(7);
    for (int i = 0; i < (int)sizeof(rnd); i++) rnd[i] = (unsigned char)(rand() & 0xFF);
    check(roundTrip(rnd, sizeof(rnd), &packed), "random data");

    unsigned char skew[200000];
    for (int i = 0; i < (int)sizeof(skew); i++) {
        int r = rand() % 100;
        skew[i] = (unsigned char)(r < 70 ? 'e' : r < 90 ? 't' : r < 97 ? 'a' : rand() % 256);
    }
    check(roundTrip(skew, sizeof(skew), &packed), "skewed data");
    printf("     200000 bytes skewed -> %d bytes\n", packed);

    // Corruption handling.
    unsigned char junk[2000]; memset(junk, 7, sizeof(junk));
    unsigned char out[10];
    check(HP_Decompress(junk, sizeof(junk), out, sizeof(out)) == HP_ERR_FORMAT, "rejects bad magic");
    check(HP_Decompress(junk, 10, out, sizeof(out)) == HP_ERR_FORMAT, "rejects short input");

    unsigned int freq[256]; HP_CountFrequencies((const unsigned char*)text, (int)strlen(text), freq);
    printf("     entropy of text = %.3f bits/byte\n", HP_Entropy(freq));

    printf(failures == 0 ? "ALL TESTS PASSED\n" : "SOME TESTS FAILED\n");
    return failures;
}
