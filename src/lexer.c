#include "lexer.h"

#include <string.h>
#include <stdbool.h>

bool is_end_of_line(char c) {
    return c == '\n' || c == '\r';
}

bool is_white_space(char c) {
    return c == ' ' || c == '\t' || is_end_of_line(c);
}

bool is_ascii_alphabetic(char c) {
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z' );
}

bool is_numeric(char c) {
    return c >= '0' && c <= '9';
}

bool is_ascii_alpha_numeric(char c) {
    return is_numeric(c) || is_ascii_alphabetic(c);
}

bool is_valid_id_char(char c) {
    return is_ascii_alpha_numeric(c) || c == '_';
}

TOKEN_TYPE multi_char_token(Lexer *lexer) {
    TOKEN_TYPE ret = 0;
    if(!lexer->at[0] || !lexer->at[1]) return ret;
    if(lexer->at[2]) {
        if(strncmp(lexer->at, "<<=", 3) == 0) ret = T_LEFT_SHIFT_EQUALS;
        if(strncmp(lexer->at, ">>=", 3) == 0) ret = T_RIGHT_SHIFT_EQUALS;
    }

    if(strncmp(lexer->at, "++", 2) == 0) ret = T_PLUS_PLUS;
    if(strncmp(lexer->at, "--", 2) == 0) ret = T_MINUS_MINUS;
    if(strncmp(lexer->at, "<<", 2) == 0) ret = T_LEFT_SHIFT;
    if(strncmp(lexer->at, ">>", 2) == 0) ret = T_RIGHT_SHIFT;
    if(strncmp(lexer->at, "+=", 2) == 0) ret = T_PLUS_EQUALS;
    if(strncmp(lexer->at, "-=", 2) == 0) ret = T_MINUS_EQUALS;
    if(strncmp(lexer->at, "*=", 2) == 0) ret = T_STAR_EQUALS;
    if(strncmp(lexer->at, "/=", 2) == 0) ret = T_SLASH_EQUALS;
    if(strncmp(lexer->at, "%=", 2) == 0) ret = T_PROCENT_EQUALS;
    if(strncmp(lexer->at, "&=", 2) == 0) ret = T_AND_EQUALS;
    if(strncmp(lexer->at, "|=", 2) == 0) ret = T_OR_EQUALS;
    if(strncmp(lexer->at, "^=", 2) == 0) ret = T_XOR_EQUALS;
    if(strncmp(lexer->at, "~=", 2) == 0) ret = T_NOT_EQUALS;
    if(strncmp(lexer->at, "==", 2) == 0) ret = T_EQUALS_EQUALS;
    if(strncmp(lexer->at, "!=", 2) == 0) ret = T_EXCLAM_EQUALS;
    if(strncmp(lexer->at, ">=", 2) == 0) ret = T_GREATER_EQUALS;
    if(strncmp(lexer->at, "<=", 2) == 0) ret = T_LESSER_EQUALS;
    if(strncmp(lexer->at, "||", 2) == 0) ret = T_OR_OR;
    if(strncmp(lexer->at, "&&", 2) == 0) ret = T_AND_AND;
    if(strncmp(lexer->at, "->", 2) == 0) ret = T_ARROW;

    if(ret == T_LEFT_SHIFT_EQUALS || ret == T_RIGHT_SHIFT_EQUALS) lexer->at += 3;
    else if(ret) lexer->at += 2;
    return ret;
}

void consume_white_space_and_comments(Lexer *lexer) {
    for(;;) {
        if(is_white_space(lexer->at[0]))
            ++lexer->at;
        // single line comment
        else if(lexer->at[0] == '/' && lexer->at[1] == '/') {
            lexer->at += 2;
            while(lexer->at[0] && !is_end_of_line(lexer->at[0])) ++lexer->at;
        }
        // multi line comment
        else if(lexer->at[0] == '/' && lexer->at[1] == '*') {
            lexer->at += 2;
            while(lexer->at[0] && !(lexer->at[0] == '*' && lexer->at[1] == '/')) {
                ++lexer->at;
            }
            // skip comment end
            lexer->at += 2;
        }
        else break;
    }
}

Token tokenize_quote_literal(Lexer *lexer) {
    assert(lexer->at[0] == '"' || lexer->at[0] == '\'' && "Start of token is not a single or double qoutation mark");
    char qoute_type = lexer->at[0];

    char *token_start = ++lexer->at;
    while(
        lexer->at[0] &&
        lexer->at[0] != qoute_type &&
        !is_end_of_line(lexer->at[0])
    )
    {
        if(lexer->at[0] == '\\' && lexer->at[1]) ++lexer->at;
        ++lexer->at;
    }
    // does not include quotes
    int token_len = (int)(lexer->at - token_start);

    // this is either eof, new line, or closing qoute, do we validate here or just leave it and continue?
    if(lexer->at[0]) ++lexer->at;
    return (Token){ token_start, token_len, qoute_type == '"' ? T_STRING_LITERAL : T_CHAR_LITERAL };
}

Token peek_token(Lexer *lexer) {
    Token current_token = lexer->token;
    next_token(lexer);
    Token peeked_token = lexer->token;
    // revert lol
    lexer->at -= peeked_token.len;
    lexer->token = current_token;
    return peeked_token;
}

// gets the next token and stores it in lexer->token. Returns true on success and false after the last token has been retrieved
bool next_token(Lexer *lexer) {
    consume_white_space_and_comments(lexer);

    // end of file
    if(!lexer->at[0]) {
        lexer->token = (Token){ lexer->at, 0, T_EOF };
        return false;
    }

    // preprocessor lines, if we implement a preprocessor these should all be gone at this stage, but let's handle them anyway for now.
    // if(*lexer->at == '#') {
    //     char *token_start = lexer->at++;
    //     while(!is_end_of_line(lexer->at[0])) {
    //         if(lexer->at[0] == '\\') ++lexer->at;
    //         ++lexer->at;
    //     }
    //     uint32 token_len = (uint32)(lexer->at - token_start);
    //     lexer->token = (Token){ token_start, token_len, T_PREPROC_LINE };
    //     return true;
    // }

    // string/char literals
    if(lexer->at[0] == '"' || lexer->at[0] == '\'') {
        lexer->token = tokenize_quote_literal(lexer);
        return true;
    }

    // numeric literals
    if(is_numeric(lexer->at[0])) {
        // hexadecimal
        if(lexer->at[0] == '0' && (lexer->at[1] == 'x' || lexer->at[1] == 'X')) {

        }
        // octal
        if(lexer->at[0] == '0' && is_numeric(lexer->at[1])) {

        }
        // decimal or floating point
        else {

        }
    }

    if(is_ascii_alphabetic(lexer->at[0]) || lexer->at[0] == '_') {
        char *token_start = lexer->at;
        while(is_valid_id_char((++lexer->at)[0]));
        lexer->token = (Token){ token_start, (int)(lexer->at - token_start), T_IDENTIFIER };
        return true;
    }

    {
        char *token_start = lexer->at;
        TOKEN_TYPE result = multi_char_token(lexer);
        if(result) {
            int token_len = (int)(lexer->at - token_start);
            lexer->token = (Token){ token_start, token_len, result };
            return true;
        }
    }

    lexer->token = (Token){ lexer->at, 1, lexer->at[0] };
    ++lexer->at;
    return true;
}
// NOTE: meta program uses this lexer to generate the function that this function uses, reason for this conditional compilation
#ifndef META_PROGRAMMING
void print_token(Token t) {
    if(t.type < T_IDENTIFIER){
        printf("%.*s\n", (int)t.len, t.loc);
    } else {
        printf("%s: %.*s\n", TOKEN_TYPE_to_string(t.type), (int)t.len, t.loc);
    }
}
#endif
