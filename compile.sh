#!/bin/bash

dotnet run code.c
as -arch arm64 -o code.o code.s
ld -arch arm64 -o a.out -lSystem -syslibroot `xcrun -sdk macosx --show-sdk-path` code.o
rm code.s code.o
