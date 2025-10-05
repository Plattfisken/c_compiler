#include "lexer.h"
#include <string.h>

int is_end_of_line(char c) {
    return c == '\n' || c == '\r';
}

int is_white_space(char c) {
    return c == ' ' || c == '\t' || is_end_of_line(c);
}

int is_ascii_alphabetic(char c) {
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z' );
}

int is_numeric(char c) {
    return c >= '0' && c <= '9';
}

int is_ascii_alpha_numeric(char c) {
    return is_numeric(c) || is_ascii_alphabetic(c);
}

int is_valid_id_char(char c) {
    return is_ascii_alpha_numeric(c) || c == '_';
}

char *stringify_token_type_name(TOKEN_TYPE t) {
    switch(t) {
        case T_IDENTIFIER: return "T_IDENTIFIER";
        case T_STRING_LITERAL: return "T_STRING_LITERAL";
        case T_INT_LITERAL: return "T_INT_LITERAL";
        case T_FLOAT_LITERAL: return "T_FLOAT_LITERAL";
        case T_CHAR_LITERAL: return "T_CHAR_LITERAL";

        case T_PLUS_PLUS: return "T_PLUS_PLUS";
        case T_MINUS_MINUS: return "T_MINUS_MINUS";
        case T_LEFT_SHIFT: return "T_LEFT_SHIFT";
        case T_RIGHT_SHIFT: return "T_RIGHT_SHIFT";
        case T_PLUS_EQUALS: return "T_PLUS_EQUALS";
        case T_MINUS_EQUALS: return "T_MINUS_EQUALS";
        case T_STAR_EQUALS: return "T_STAR_EQUALS";
        case T_SLASH_EQUALS: return "T_SLASH_EQUALS";
        case T_PROCENT_EQUALS: return "T_PROCENT_EQUALS";
        case T_AND_EQUALS: return "T_AND_EQUALS";
        case T_OR_EQUALS: return "T_OR_EQUALS";
        case T_XOR_EQUALS: return "T_XOR_EQUALS";
        case T_NOT_EQUALS: return "T_NOT_EQUALS";
        case T_LEFT_SHIFT_EQUALS: return "T_LEFT_SHIFT_EQUALS";
        case T_RIGHT_SHIFT_EQUALS: return "T_RIGHT_SHIFT_EQUALS";
        case T_EQUALS_EQUALS: return "T_EQUALS_EQUALS";
        case T_EXCLAM_EQUALS: return "T_EXCLAM_EQUALS";
        case T_GREATER_EQUALS: return "T_GREATER_EQUALS";
        case T_LESSER_EQUALS: return "T_LESSER_EQUALS";
        case T_OR_OR: return "T_OR_OR";
        case T_AND_AND: return "T_AND_AND";
        case T_ARROW: return "T_ARROW";

        case T_PREPROC_LINE: return "T_PREPROC_LINE";
        case T_EOF: return "T_EOF";
        case T_ERROR: return "T_ERROR";

        default: return NULL;
    }
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

Token next_token(Lexer *lexer) {
    consume_white_space_and_comments(lexer);

    // end of file
    if(!lexer->at[0])
        return (Token){ lexer->at, 0, T_EOF };

    // preprocessor lines, if we implement a preprocessor these should all be gone at this stage, but let's handle them anyway for now.
    if(*lexer->at == '#') {
        char *token_start = lexer->at++;
        while(!is_end_of_line(lexer->at[0])) {
            if(lexer->at[0] == '\\') ++lexer->at;
            ++lexer->at;
        }
        uint32 token_len = (uint32)(lexer->at - token_start);
        return (Token){ token_start, token_len, T_PREPROC_LINE };
    }

    // string/char literals
    if(lexer->at[0] == '"' || lexer->at[0] == '\'') {
        return tokenize_quote_literal(lexer);
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
        return (Token){ token_start, (int)(lexer->at - token_start), T_IDENTIFIER };
    }

    {
        char *token_start = lexer->at;
        TOKEN_TYPE result = multi_char_token(lexer);
        if(result) {
            int token_len = (int)(lexer->at - token_start);
            return (Token){ token_start, token_len, result };
        }
    }

    Token ret = { lexer->at, 1, lexer->at[0] };
    ++lexer->at;
    return ret;
}

void print_token(Token t) {
    if(t.type < T_IDENTIFIER){
        printf("%.*s\n", (int)t.len, t.loc);
    } else {
        printf("%s: %.*s\n",stringify_token_type_name(t.type), (int)t.len, t.loc);
    }
}
