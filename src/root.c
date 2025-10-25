#include <string.h>
#include <stdbool.h>
#include <stddef.h>
#include <assert.h>

#define USEFUL_THINGS_IMPLEMENTATION
#define USEFUL_THINGS_STRIP_PREFIX
#define USEFUL_THINGS_STDLIB
#include <useful_things.h>

#include "lexer.h"
#include "generated/generated.c"
#include "preprocessor.c"
#include "compiler.c"
#include "lexer.c"
