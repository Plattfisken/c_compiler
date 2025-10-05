#!/bin/sh
mkdir -p ../build/
clang -Wall -Wextra -o ../build/compiler -I../../useful_things root.c
