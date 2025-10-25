// procedures generated from lexer.h
#include "../lexer.h"
char *TOKEN_TYPE_to_string(TOKEN_TYPE enum_member) {
    switch(enum_member) {
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
