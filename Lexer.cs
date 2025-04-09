namespace compiler_csharp;

public class Lexer {
    string text;
    int cur = 0;

    public Lexer(string t) => text = t;

    public Token next_token() {
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\0') return new Token(TOKEN_TYPE.EOF);
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
                    if(is_lexeme_or_reserved_symbol(three_char_lexem, out var t)) {
                        // consume two extra chars
                        consume_char();
                        consume_char();
                        return new Token(t);
                    }
                }
                if(cur < text.Length) {
                    string two_char_lexeme = text[(cur - 1) .. (cur + 1)];
                    if(is_lexeme_or_reserved_symbol(two_char_lexeme, out var t)) {
                        // consume extra char
                        consume_char();
                        return new Token(t);
                    }
                }
                {
                    if(is_lexeme_or_reserved_symbol(c.ToString(), out var t))
                        return new Token(t);
                    // NOTE: should be unreachable
                    else return new Token(TOKEN_TYPE.PARSE_ERROR, "How did we get here?");
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
                    if(is_lexeme_or_reserved_symbol(two_char_lexeme, out var t)) {
                        // have to consume an extra character since we added a two length token
                        consume_char();
                        return new Token(t);
                    }
                }
                {
                    if(is_lexeme_or_reserved_symbol(c.ToString(), out var t))
                        return new Token(t);
                    // NOTE: Should be unreachable
                    else return new Token(TOKEN_TYPE.PARSE_ERROR, "How did we get here?");
                }
            }
            case '.':
                if(is_number(peek_char()))
                    return get_numeric_literal();
                else
                    return new Token(TOKEN_TYPE.DOT);
            default: {
                // one length single value token
                if(is_lexeme_or_reserved_symbol(c.ToString(), out var t)) {
                    return new Token(t);
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
        while(!is_white_space(peek_char()) && !is_lexeme_or_reserved_symbol(peek_char().ToString(), out var _)) {
            consume_char();
        }
        string value = text[token_start .. cur];
        TOKEN_TYPE t;
        if(is_keyword(value)) t = TOKEN_TYPE.KEYWORD;
        else t = TOKEN_TYPE.IDENTIFIER;
        return new Token(t, value);
    }

    Token get_string_literal() {
        // TODO: Handle escaping characters, handle don't allow newline.
        // the starting quote has already been consumed. As we could not have known it was a '"' until consuming
        // since we wish to include it in the token we simply subtract 1 from cur to find its index again.
        int token_start = cur - 1;
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\\') consume_char();
            if(c == '\n') return new Token(TOKEN_TYPE.PARSE_ERROR, "No closing quotation mark before newline.");
            if(c == '\0') return new Token(TOKEN_TYPE.PARSE_ERROR, "No closing quotation mark before eof.");
        } while(c != '"');
        return new Token(TOKEN_TYPE.STRING_LITERAL, text[token_start .. cur]);
    }

    Token get_char_literal() {
        int token_start = cur - 1;
        if(consume_char() == '\\') consume_char();
        if(consume_char() == '\'') {
            return new Token(TOKEN_TYPE.CHAR_LITERAL, text[token_start .. cur]);
        }
        return new Token(TOKEN_TYPE.PARSE_ERROR);
    }

    Token get_numeric_literal() {
        // TODO: support scientific notation?
        // NOTE: No need to support 0 prefix octal, right? Who even uses octal? 001 is a decimal literal
        // hexadecimal
        int token_start = cur - 1;
        if(peek_char(-1) == '0' && (peek_char() == 'x' || peek_char() == 'X')) {
            consume_char();
            while(is_hex_number(peek_char()))
                consume_char();
            return new Token(TOKEN_TYPE.INT_LITERAL, text[token_start .. cur]);
        }
        // decimal
        bool is_floating_point_number = false;
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
        TOKEN_TYPE t = is_floating_point_number ? TOKEN_TYPE.FLOAT_LITERAL : TOKEN_TYPE.INT_LITERAL;
        return new Token(t, text[token_start .. cur]);
    }

    Token get_single_line_comment() {
        int token_start = cur - 1;
        while(peek_char() != '\n' && peek_char() != '\0') {
            consume_char();
        }
        string value = text[token_start .. cur];
        return new Token(TOKEN_TYPE.COMMENT, value);
    }

    Token get_block_comment() {
        int token_start = cur - 1;
        char c = '\0';
        do {
            c = consume_char();
            if(c == '\0') return new Token(TOKEN_TYPE.PARSE_ERROR);
        } while(!(c == '*' && consume_char() == '/'));
        string value = text[token_start .. cur];
        return new Token(TOKEN_TYPE.COMMENT, value);
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

    static bool is_keyword(string str) {
        switch(str) {
            case "break"   :
            case "case"    :
            case "char"    :
            case "const"   :
            case "continue":
            case "default" :
            case "do"      :
            case "double"  :
            case "else"    :
            case "enum"    :
            case "float"   :
            case "for"     :
            case "goto"    :
            case "if"      :
            case "int"     :
            case "long"    :
            case "return"  :
            case "short"   :
            case "sizeof"  :
            case "static"  :
            case "struct"  :
            case "switch"  :
            case "typedef" :
            case "union"   :
            case "unsigned":
            case "void"    :
            case "while"   :
                return true;
            default:
                return false;
        }
    }

    static bool is_lexeme_or_reserved_symbol(string str, out TOKEN_TYPE t) {
        switch(str) {
            case "(":   t = TOKEN_TYPE.OPEN_PAREN;         break;
            case ")":   t = TOKEN_TYPE.CLOSE_PAREN;        break;
            case "{":   t = TOKEN_TYPE.OPEN_CURLY;         break;
            case "}":   t = TOKEN_TYPE.CLOSE_CURLY;        break;
            case "[":   t = TOKEN_TYPE.OPEN_BRACKET;       break;
            case "]":   t = TOKEN_TYPE.CLOSE_BRACKET;      break;
            case ";":   t = TOKEN_TYPE.SEMICOLON;          break;
            case ":":   t = TOKEN_TYPE.COLON;              break;
            case ".":   t = TOKEN_TYPE.DOT;                break;
            case ",":   t = TOKEN_TYPE.COMMA;              break;
            case "?":   t = TOKEN_TYPE.QUESTION_MARK;      break;
            case "!":   t = TOKEN_TYPE.EXCLAM;             break;
            case "=":   t = TOKEN_TYPE.EQUALS;             break;
            case "+":   t = TOKEN_TYPE.PLUS;               break;
            case "-":   t = TOKEN_TYPE.MINUS;              break;
            case "*":   t = TOKEN_TYPE.STAR;               break;
            case "/":   t = TOKEN_TYPE.SLASH;              break;
            case "%":   t = TOKEN_TYPE.PROCENT;            break;
            case "|":   t = TOKEN_TYPE.OR;                 break;
            case "&":   t = TOKEN_TYPE.AND;                break;
            case "~":   t = TOKEN_TYPE.NOT;                break;
            case "^":   t = TOKEN_TYPE.XOR;                break;
            case ">":   t = TOKEN_TYPE.GREATER;            break;
            case "<":   t = TOKEN_TYPE.SMALLER;            break;
            case "++":  t = TOKEN_TYPE.PLUS_PLUS;          break;
            case "+=":  t = TOKEN_TYPE.PLUS_EQUALS;        break;
            case "--":  t = TOKEN_TYPE.MINUS_MINUS;        break;
            case "-=":  t = TOKEN_TYPE.MINUS_EQUALS;       break;
            case "->":  t = TOKEN_TYPE.ARROW;              break;
            case "<<":  t = TOKEN_TYPE.SMALLER_SMALLER;    break;
            case "<=":  t = TOKEN_TYPE.SMALLER_EQUALS;     break;
            case ">>":  t = TOKEN_TYPE.GREATER_GREATER;    break;
            case ">=":  t = TOKEN_TYPE.GREATER_EQUALS;     break;
            case "||":  t = TOKEN_TYPE.OR_OR;              break;
            case "|=":  t = TOKEN_TYPE.OR_EQUALS;          break;
            case "&&":  t = TOKEN_TYPE.AND_AND;            break;
            case "&=":  t = TOKEN_TYPE.AND_EQUALS;         break;
            case "==":  t = TOKEN_TYPE.EQUALS_EQUALS;      break;
            case "*=":  t = TOKEN_TYPE.STAR_EQUALS;        break;
            case "/=":  t = TOKEN_TYPE.SLASH_EQUALS;       break;
            case "!=":  t = TOKEN_TYPE.EXCLAM_EQUALS;      break;
            case "%=":  t = TOKEN_TYPE.PROCENT_EQUALS;     break;
            case "^=":  t = TOKEN_TYPE.XOR_EQUALS;         break;
            case "<<=": t = TOKEN_TYPE.LEFT_SHIFT_EQUALS;  break;
            case ">>=": t = TOKEN_TYPE.RIGHT_SHIFT_EQUALS; break;
            // Parse error as these symbols are not tokens by themselves, but it's useful to look them up
            case "\"":  t = TOKEN_TYPE.PARSE_ERROR;        break;
            case "'":   t = TOKEN_TYPE.PARSE_ERROR;        break;
            case "\\":  t = TOKEN_TYPE.PARSE_ERROR;        break;
            default:
                // Arbitrary value for t
                t = TOKEN_TYPE.IDENTIFIER;
                return false;
        }
        return true;
    }
}

public enum TOKEN_TYPE {
    IDENTIFIER,
    KEYWORD,
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
    ARROW,               //

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

    COMMENT,             // Do we really care?
    EOF,
    PARSE_ERROR
}

public struct Token {
    public TOKEN_TYPE type;
    public string value;
    public Token(TOKEN_TYPE t, string v = "") => (type, value) = (t, v);
}
