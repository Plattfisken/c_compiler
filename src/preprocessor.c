#include "lexer.h"

// #define USEFUL_THINGS_STRIP_PREFIX
// #include <useful_things.h>

// bool UT_strings_are_equal(UT_String s1, UT_String s2) {
//     if(s1.length != s2.length) return false;
//     for(int i = 0; i < s1.length; ++i) {
//         if(s1.data[i] != s2.data[i]) return false;
//     }
//     return true;
// }
//
// bool UT_is_null_terminated(UT_String s) {
//     return s.data[s.length] == 0;
// }
//
// UT_String UT_pointer_to_string(char *p, int length) {
//     return (UT_String){ .data = p, .length = length };
// }
//
// UT_String UT_null_term_to_string(char *s) {
//     int length = 0;
//     while(s++) ++length;
//     return UT_pointer_to_string(s, length);
// }
//
// UT_String UT_copy_string(UT_String s, UT_Arena *arena) {
//     return UT_make_string(s.data, s.length, arena);
// }
//
// UT_String UT_make_null_terminated(UT_String s, UT_Arena *arena) {
//     if(UT_is_null_terminated(s)) return s;
//     return UT_copy_string(s, arena);
// }

bool accept_token(Lexer *lexer, TOKEN_TYPE expected) {
    if(peek_token(lexer).type == expected) {
        next_token(lexer);
        return true;
    }
    else return false;
}

void expect_token(Lexer *lexer, TOKEN_TYPE expected) {
    // TODO: fix this shit
    if(!accept_token(lexer, expected)) assert(0 && "Expected another thing");
}

typedef struct {
    char *data;
    size_t length;
    size_t capacity;
} StringBuilder;

void sb_append(StringBuilder *sb, String s) {
    assert(sb->capacity - sb->length >= s.length && "doesn't fit");
    memcpy(sb->data + sb->length, s.data, s.length);
    sb->length += s.length;
}

void parse_preprocessor_directive(Lexer *lexer, StringBuilder *output, Arena *arena) {
    assert(lexer->token.type == '#');
    next_token(lexer);
    assert(lexer->token.type == T_IDENTIFIER);
    if(strings_are_equal(slice_to_string(lexer->token.loc, lexer->token.len), STR("include"))) {
        next_token(lexer);
        print_token(lexer->token);
        switch(lexer->token.type) {
            case T_STRING_LITERAL: {
                String file_name = make_string(lexer->token.loc, lexer->token.len, arena);
                String file = read_entire_file_as_string(file_name, arena);
                sb_append(output, file);
            } break;
            case '<': {
                assert(0 && "Not implemented");
            } break;
            // TODO: this shouldn't be an assert but be handled in a common way for all compile errors
            default: {
                assert(0 && "Expected '\"' or '<'");
            }
        }
    }
}

String preprocess(String file, Arena *arena) {
    Lexer lexer = { .at = file.data };
    // TODO: There is no guarantee that this will fit, should make it dynamic
    size_t sb_cap = MEGABYTES(2);
    StringBuilder sb = { .data = arena_alloc(arena, sb_cap, 1), .length = 0 , .capacity = sb_cap };
    assert(sb.data && "failed to allocate lmao");
    char *last_preproc = file.data;
    size_t length_since_last_preproc = 0;
    while(next_token(&lexer)) {
        if(lexer.token.type == '#') {
            sb_append(&sb, slice_to_string(last_preproc, length_since_last_preproc));
            length_since_last_preproc = 0;
            parse_preprocessor_directive(&lexer, &sb, arena);
            last_preproc = lexer.at;
        }
        else {
            ++length_since_last_preproc;
        }
    }
    sb_append(&sb, slice_to_string(last_preproc, length_since_last_preproc));
    return (String){ .data = sb.data, .length = sb.length };
}
