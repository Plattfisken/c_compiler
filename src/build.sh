#!/bin/sh
clang -o meta_program -I../../useful_things ./meta_programs/meta_programming.c && ./meta_program && rm meta_program
mkdir -p ../build/
clang -Wall -Wextra -Wno-switch -o ../build/compiler -I../../useful_things root.c
