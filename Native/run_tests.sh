#!/bin/sh
# Builds and runs the console tests for the C++ engine (Linux / macOS / WSL).
cd "$(dirname "$0")" && g++ -O2 -Wall -Wextra -o test_huffman huffman.cpp test_huffman.cpp -lm && ./test_huffman
