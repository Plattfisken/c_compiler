using System.Globalization;

namespace c_compiler;

public class Lexer {
    string text;
    int cur = 0;

    public Lexer(string t) => text = t;

    public Token next_token() {
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\0') return new Token(TOKEN_TYPE.EOF, cur);
        } while(is_white_space(c));

        switch(c) {
            // string literals
            case '"':
                return get_string_literal();
            // char literals
            case '\'':
                return get_char_literal();
            // numeric literals
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return get_numeric_literal();
            // three, two or one length single value tokens
            case '<':
            case '>':
            {
                if(cur + 1 < text.Length) {
                    string three_char_lexem = text[(cur - 1) .. (cur + 2)];
                    if(is_lexeme_keyword_or_reserved_symbol(three_char_lexem, out var t)) {
                        // consume two extra chars
                        consume_char();
                        consume_char();
                        return new Token(t, cur - 3);
                    }
                }
                if(cur < text.Length) {
                    string two_char_lexeme = text[(cur - 1) .. (cur + 1)];
                    if(is_lexeme_keyword_or_reserved_symbol(two_char_lexeme, out var t)) {
                        // consume extra char
                        consume_char();
                        return new Token(t, cur - 2);
                    }
                }
                {
                    if(is_lexeme_keyword_or_reserved_symbol(c.ToString(), out var t))
                        return new Token(t, cur - 1);
                    // NOTE: should be unreachable
                    else return new Token(TOKEN_TYPE.PARSE_ERROR, cur, "How did we get here?");
                }
            }
            // two or one length single value tokens
            case '+':
            case '-':
            case '*':
            case '/':
            case '=':
            case '%':
            case '!':
            case '&':
            case '|':
            case '^':
            {
                if(c == '/') {
                    if(peek_char() == '/') {
                        return get_single_line_comment(); // Maybe just skip comments without tokenizing?
                    }
                    else if(peek_char() == '*') {
                        return get_block_comment();
                    }
                }
                if(cur < text.Length) {
                    string two_char_lexeme = text[(cur - 1) .. (cur + 1)];
                    if(is_lexeme_keyword_or_reserved_symbol(two_char_lexeme, out var t)) {
                        // have to consume an extra character since we added a two length token
                        consume_char();
                        return new Token(t, cur - 2);
                    }
                }
                {
                    if(is_lexeme_keyword_or_reserved_symbol(c.ToString(), out var t))
                        return new Token(t, cur - 1);
                    // NOTE: Should be unreachable
                    else return new Token(TOKEN_TYPE.PARSE_ERROR, cur, "How did we get here?");
                }
            }
            case '.':
                if(is_number(peek_char()))
                    return get_numeric_literal();
                else
                    return new Token(TOKEN_TYPE.DOT, cur - 1);
            default: {
                // one length single value token
                if(is_lexeme_keyword_or_reserved_symbol(c.ToString(), out var t)) {
                    return new Token(t, cur - 1);
                }
                // id's and keywords
                else {
                    return get_identifier_or_keyword();
                }
            }
        }
    }

    Token get_identifier_or_keyword() {
        int token_start = cur - 1;
        while(peek_char() != 0 && !is_white_space(peek_char()) && !is_lexeme_keyword_or_reserved_symbol(peek_char().ToString(), out var _)) {
            consume_char();
        }
        string value = text[token_start .. cur];
        TOKEN_TYPE t;
        is_lexeme_keyword_or_reserved_symbol(value, out t);
        // only care about the value if it's an identifier
        return t == TOKEN_TYPE.IDENTIFIER ? new Token(t, token_start, value) : new Token(t, token_start);
    }

    Token get_string_literal() {
        int token_start = cur - 1;
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\\') consume_char();
            if(c == '\n') return new Token(TOKEN_TYPE.PARSE_ERROR, cur, "No closing quotation mark before newline.");
            if(c == '\0') return new Token(TOKEN_TYPE.PARSE_ERROR, cur, "No closing quotation mark before eof.");
        } while(c != '"');
        return new Token(TOKEN_TYPE.STRING_LITERAL, token_start, text[token_start .. cur]);
    }

    Token get_char_literal() {
        int token_start = cur - 1;
        if(consume_char() == '\\') consume_char();
        if(consume_char() == '\'') {
            return new Token(TOKEN_TYPE.CHAR_LITERAL, token_start, i_val: (Int64)peek_char(-2));
        }
        return new Token(TOKEN_TYPE.PARSE_ERROR, token_start);
    }

    Token get_numeric_literal() {
        // TODO: we have to handle parsing signed vs unsigned, int vs long, float vs double or we will likely have a hilariously unexpected behaviour
        // TODO: support scientific notation?
        // NOTE: No need to support 0 prefix octal, right? Who even uses octal? 001 is a decimal literal
        // hexadecimal
        int token_start = cur - 1;
        char c = peek_char(-1);
        if(peek_char(-1) == '0' && (peek_char() == 'x' || peek_char() == 'X')) {
            consume_char();
            while(is_hex_number(peek_char()))
                consume_char();
            return new Token(TOKEN_TYPE.INT_LITERAL, token_start, i_val: parse_int_literal_hex(text[token_start .. cur]));
        }
        // decimal
        bool is_floating_point_number = false;
        // ugly fix if the number starts with .
        if(peek_char(-1) == '.') is_floating_point_number = true;
        while(is_number(peek_char()) || (peek_char() == '.' && !is_floating_point_number)) {
            if(peek_char() == '.') {
                if(is_number(peek_char(1))) {
                    consume_char();
                    is_floating_point_number = true;
                }
                else
                    break;
            }
            consume_char();
        }
        if(peek_char() == 'f' || peek_char() == 'F') {
            is_floating_point_number = true;
            consume_char();
        }
        return is_floating_point_number ?
            new Token(TOKEN_TYPE.FLOAT_LITERAL, token_start, f_val: parse_float_literal(text[token_start .. cur])) :
            new Token(TOKEN_TYPE.INT_LITERAL, token_start, i_val: parse_int_literal_decimal(text[token_start .. cur]));
    }

    Token get_single_line_comment() {
        int token_start = cur - 1;
        while(peek_char() != '\n' && peek_char() != '\0') {
            consume_char();
        }
        string value = text[token_start .. cur];
        return new Token(TOKEN_TYPE.COMMENT, token_start, value);
    }

    Token get_block_comment() {
        int token_start = cur - 1;
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\0') return new Token(TOKEN_TYPE.PARSE_ERROR, loc:cur);
        } while(!(c == '*' && consume_char() == '/'));
        string value = text[token_start .. cur];
        return new Token(TOKEN_TYPE.COMMENT, cur, value);
    }

    char peek_char(int offset = 0) {
        // peek at a character without updating the cursor, offset lets you specify how far ahead you wanna look, default is 0 which is the next
        // character that will be returned by consume_char()
        if(cur >= text.Length) return '\0';
        return text[cur + offset];
    }

    char consume_char() {
        if(cur >= text.Length) return '\0';
        return text[cur++];
    }

    static long parse_int_literal_hex(string i_lit) {
        return Convert.ToInt64(i_lit, 16);
    }

    static long parse_int_literal_decimal(string i_lit) {
        if(Int64.TryParse(i_lit, out long ret))
            return ret;
        else {
            // TODO: we should handle internal errors differently and reserve this function for user facing errors
            Compiler.err_and_die("Internal error: Failed to parse int");
            return 0;
        }
    }

    static float parse_float_literal(string f_lit) {
        // remove f/F
        if(f_lit.Last() == 'f' || f_lit.Last() == 'F') f_lit = f_lit[0 .. (f_lit.Length-1)];
        if(float.TryParse(f_lit, NumberStyles.Any, CultureInfo.InvariantCulture, out float ret))
            return ret;
        else {
            // TODO: we should handle internal errors differently and reserve this message for user facing errors
            Compiler.err_and_die("Internal error: Failed to parse float");
            return 0;
        }
    }

    static bool is_hex_number(char c) {
        if(is_number(c)) return true;
        switch(c) {
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
                return true;
            default:
                return false;
        }
    }

    static bool is_number(char c) {
        return char.IsNumber(c);
    }

    static bool is_white_space(char c) {
        return char.IsWhiteSpace(c);
    }

    static bool is_lexeme_keyword_or_reserved_symbol(string str, out TOKEN_TYPE t) {
        switch(str) {
            case "("       : t = TOKEN_TYPE.OPEN_PAREN;         break;
            case ")"       : t = TOKEN_TYPE.CLOSE_PAREN;        break;
            case "{"       : t = TOKEN_TYPE.OPEN_CURLY;         break;
            case "}"       : t = TOKEN_TYPE.CLOSE_CURLY;        break;
            case "["       : t = TOKEN_TYPE.OPEN_BRACKET;       break;
            case "]"       : t = TOKEN_TYPE.CLOSE_BRACKET;      break;
            case ";"       : t = TOKEN_TYPE.SEMICOLON;          break;
            case ":"       : t = TOKEN_TYPE.COLON;              break;
            case "."       : t = TOKEN_TYPE.DOT;                break;
            case ","       : t = TOKEN_TYPE.COMMA;              break;
            case "?"       : t = TOKEN_TYPE.QUESTION_MARK;      break;
            case "!"       : t = TOKEN_TYPE.EXCLAM;             break;
            case "="       : t = TOKEN_TYPE.EQUALS;             break;
            case "+"       : t = TOKEN_TYPE.PLUS;               break;
            case "-"       : t = TOKEN_TYPE.MINUS;              break;
            case "*"       : t = TOKEN_TYPE.STAR;               break;
            case "/"       : t = TOKEN_TYPE.SLASH;              break;
            case "%"       : t = TOKEN_TYPE.PROCENT;            break;
            case "|"       : t = TOKEN_TYPE.OR;                 break;
            case "&"       : t = TOKEN_TYPE.AND;                break;
            case "~"       : t = TOKEN_TYPE.NOT;                break;
            case "^"       : t = TOKEN_TYPE.XOR;                break;
            case ">"       : t = TOKEN_TYPE.GREATER;            break;
            case "<"       : t = TOKEN_TYPE.SMALLER;            break;
            case "++"      : t = TOKEN_TYPE.PLUS_PLUS;          break;
            case "+="      : t = TOKEN_TYPE.PLUS_EQUALS;        break;
            case "--"      : t = TOKEN_TYPE.MINUS_MINUS;        break;
            case "-="      : t = TOKEN_TYPE.MINUS_EQUALS;       break;
            case "->"      : t = TOKEN_TYPE.ARROW;              break;
            case "<<"      : t = TOKEN_TYPE.SMALLER_SMALLER;    break;
            case "<="      : t = TOKEN_TYPE.SMALLER_EQUALS;     break;
            case ">>"      : t = TOKEN_TYPE.GREATER_GREATER;    break;
            case ">="      : t = TOKEN_TYPE.GREATER_EQUALS;     break;
            case "||"      : t = TOKEN_TYPE.OR_OR;              break;
            case "|="      : t = TOKEN_TYPE.OR_EQUALS;          break;
            case "&&"      : t = TOKEN_TYPE.AND_AND;            break;
            case "&="      : t = TOKEN_TYPE.AND_EQUALS;         break;
            case "=="      : t = TOKEN_TYPE.EQUALS_EQUALS;      break;
            case "*="      : t = TOKEN_TYPE.STAR_EQUALS;        break;
            case "/="      : t = TOKEN_TYPE.SLASH_EQUALS;       break;
            case "!="      : t = TOKEN_TYPE.EXCLAM_EQUALS;      break;
            case "%="      : t = TOKEN_TYPE.PROCENT_EQUALS;     break;
            case "^="      : t = TOKEN_TYPE.XOR_EQUALS;         break;
            case "<<="     : t = TOKEN_TYPE.LEFT_SHIFT_EQUALS;  break;
            case ">>="     : t = TOKEN_TYPE.RIGHT_SHIFT_EQUALS; break;
            // Parse error as these symbols are not tokens by themselves, but it's useful to look them up
            case "\""      : t = TOKEN_TYPE.PARSE_ERROR;        break;
            case "'"       : t = TOKEN_TYPE.PARSE_ERROR;        break;
            case "\\"      : t = TOKEN_TYPE.PARSE_ERROR;        break;
            // Keywords
            case "break"   : t = TOKEN_TYPE.KEYWORD_BREAK;      break;
            case "case"    : t = TOKEN_TYPE.KEYWORD_CASE;       break;
            case "char"    : t = TOKEN_TYPE.KEYWORD_CHAR;       break;
            case "const"   : t = TOKEN_TYPE.KEYWORD_CONST;      break;
            case "continue": t = TOKEN_TYPE.KEYWORD_CONTINUE;   break;
            case "default" : t = TOKEN_TYPE.KEYWORD_DEFAULT;    break;
            case "do"      : t = TOKEN_TYPE.KEYWORD_DO;         break;
            case "double"  : t = TOKEN_TYPE.KEYWORD_DOUBLE;     break;
            case "else"    : t = TOKEN_TYPE.KEYWORD_ELSE;       break;
            case "enum"    : t = TOKEN_TYPE.KEYWORD_ENUM;       break;
            case "float"   : t = TOKEN_TYPE.KEYWORD_FLOAT;      break;
            case "for"     : t = TOKEN_TYPE.KEYWORD_FOR;        break;
            case "goto"    : t = TOKEN_TYPE.KEYWORD_GOTO;       break;
            case "if"      : t = TOKEN_TYPE.KEYWORD_IF;         break;
            case "int"     : t = TOKEN_TYPE.KEYWORD_INT;        break;
            case "long"    : t = TOKEN_TYPE.KEYWORD_LONG;       break;
            case "return"  : t = TOKEN_TYPE.KEYWORD_RETURN;     break;
            case "short"   : t = TOKEN_TYPE.KEYWORD_SHORT;      break;
            case "sizeof"  : t = TOKEN_TYPE.KEYWORD_SIZEOF;     break;
            case "static"  : t = TOKEN_TYPE.KEYWORD_STATIC;     break;
            case "struct"  : t = TOKEN_TYPE.KEYWORD_STRUCT;     break;
            case "switch"  : t = TOKEN_TYPE.KEYWORD_SWITCH;     break;
            case "typedef" : t = TOKEN_TYPE.KEYWORD_TYPEDEF;    break;
            case "union"   : t = TOKEN_TYPE.KEYWORD_UNION;      break;
            case "unsigned": t = TOKEN_TYPE.KEYWORD_UNSIGNED;   break;
            case "void"    : t = TOKEN_TYPE.KEYWORD_VOID;       break;
            case "while"   : t = TOKEN_TYPE.KEYWORD_WHILE;      break;
            default:
                t = TOKEN_TYPE.IDENTIFIER;
                return false;
        }
        return true;
    }
}

public enum TOKEN_TYPE {
    IDENTIFIER,
    STRING_LITERAL,
    INT_LITERAL,
    FLOAT_LITERAL,
    CHAR_LITERAL,

    OPEN_PAREN,          // (
    CLOSE_PAREN,         // )
    OPEN_CURLY,          // {
    CLOSE_CURLY,         // }
    OPEN_BRACKET,        // [
    CLOSE_BRACKET,       // ]

    SEMICOLON,           // ;
    COLON,               // :
    DOT,                 // .
    COMMA,               // ,
    QUESTION_MARK,       // ?

    EXCLAM,              // !
    EXCLAM_EQUALS,       // !=

    PLUS,                // +
    PLUS_PLUS,           // ++
    PLUS_EQUALS,         // +=

    MINUS,               // -
    MINUS_MINUS,         // --
    MINUS_EQUALS,        // -=
    ARROW,               // ->

    STAR,                // *
    STAR_EQUALS,         // *=

    SLASH,               // /
    SLASH_EQUALS,        // /=

    PROCENT,             // %
    PROCENT_EQUALS,      // %=

    EQUALS,              // =
    EQUALS_EQUALS,       // ==
                         //
    SMALLER,             // <
    SMALLER_SMALLER,     // <<
    SMALLER_EQUALS,      // <=
    LEFT_SHIFT_EQUALS,   // <<=

    GREATER,             // >
    GREATER_GREATER,     // >>
    GREATER_EQUALS,      // >=
    RIGHT_SHIFT_EQUALS,  // >>=

    AND,                 // &
    AND_AND,             // &&
    AND_EQUALS,          // &=
                         //
    OR,                  // |
    OR_OR,               // ||
    OR_EQUALS,           // |=

    XOR,                 // ^
    XOR_EQUALS,          // ^=

    NOT,                 // ~

    KEYWORD_BREAK,
    KEYWORD_CASE,
    KEYWORD_CHAR,
    KEYWORD_CONST,
    KEYWORD_CONTINUE,
    KEYWORD_DEFAULT,
    KEYWORD_DO,
    KEYWORD_DOUBLE,
    KEYWORD_ELSE,
    KEYWORD_ENUM,
    KEYWORD_FLOAT,
    KEYWORD_FOR,
    KEYWORD_GOTO,
    KEYWORD_IF,
    KEYWORD_INT,
    KEYWORD_LONG,
    KEYWORD_RETURN,
    KEYWORD_SHORT,
    KEYWORD_SIZEOF,
    KEYWORD_STATIC,
    KEYWORD_STRUCT,
    KEYWORD_SWITCH,
    KEYWORD_TYPEDEF,
    KEYWORD_UNION,
    KEYWORD_UNSIGNED,
    KEYWORD_VOID,
    KEYWORD_WHILE,

    COMMENT,
    EOF,
    PARSE_ERROR
}

public struct Token {
    public TOKEN_TYPE type;
    public object value;
    public int loc_in_src;
    // TODO: token location should be stored here probably
    public Token(TOKEN_TYPE t, int loc, string s_val = "", long i_val = 0, float f_val = 0) {
        loc_in_src = loc;
        type = t;
        value = t switch {
            TOKEN_TYPE.INT_LITERAL => i_val,
            TOKEN_TYPE.CHAR_LITERAL => i_val,
            TOKEN_TYPE.FLOAT_LITERAL => f_val,
            _ => s_val
        };
    // public string value;
    // public Int64 integer_value;
    // public float floating_point_value;
    // do we need?
    // public double double_value;
        // value = v;
        // integer_value = i_val;
        // floating_point_value = f_val;
    }
}
