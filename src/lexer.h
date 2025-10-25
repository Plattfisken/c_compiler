#ifndef LEXER_H
#define LEXER_H

#include <stdbool.h>

typedef enum {
    T_IDENTIFIER = 256,
    T_STRING_LITERAL,
    T_INT_LITERAL,
    T_FLOAT_LITERAL,
    T_CHAR_LITERAL,
    T_PLUS_PLUS,
    T_MINUS_MINUS,
    T_LEFT_SHIFT,
    T_RIGHT_SHIFT,
    T_PLUS_EQUALS,
    T_MINUS_EQUALS,
    T_STAR_EQUALS,
    T_SLASH_EQUALS,
    T_PROCENT_EQUALS,
    T_AND_EQUALS,
    T_OR_EQUALS,
    T_XOR_EQUALS,
    T_NOT_EQUALS,
    T_LEFT_SHIFT_EQUALS,
    T_RIGHT_SHIFT_EQUALS,
    T_EQUALS_EQUALS,
    T_EXCLAM_EQUALS,
    T_GREATER_EQUALS,
    T_LESSER_EQUALS,
    T_OR_OR,
    T_AND_AND,
    T_ARROW,

    T_PREPROC_LINE,
    T_EOF,
    T_ERROR
} TOKEN_TYPE;

typedef struct {
    char *loc;
    size_t len;
    TOKEN_TYPE type;
} Token;

typedef struct {
    char *at;
    Token token;
} Lexer;

// stores next token in lexer->token and advances the lexer
bool next_token(Lexer *lexer);
// returns the next token without advancing the lexer
Token peek_token(Lexer *lexer);
void print_token(Token t);

#endif
