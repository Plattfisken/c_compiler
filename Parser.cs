namespace compiler_csharp;
using System.Text;

public class Parser {
    string text;
    Lexer lexer;

    List<Token> lexed_tokens = new();
    int next_token_idx = 0;


    public Parser(string source_code) {
        text = source_code;
        lexer = new(text);
    }

    public AstNode_TranslationUnit parse() {
        // Recursive descent parser. Each method corresponds to one non-terminal of the grammar
        return translation_unit();
    }

    AstNode_TranslationUnit translation_unit() {
        var translation_unit_node = new AstNode_TranslationUnit();
        while(!accept(TOKEN_TYPE.EOF))
            function_definition(translation_unit_node);
        return translation_unit_node;
    }

    void function_definition(AstNode_TranslationUnit parent) {
        var func_def_node = new AstNode_ProcedureDef();
        parent.children.Add(func_def_node);
        type_specifier();
        declarator();
        func_def_node.name = (string)peek_token(-1).value;
        expect(TOKEN_TYPE.OPEN_PAREN);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            test_function_call(func_def_node);
        // compound_statement();
    }

    void test_function_call(AstNode_ProcedureDef parent) {
        var func_call_node = new AstNode_ProcedureCall();
        parent.children.Add(func_call_node);
        // this function should be deleted later, just for some testing
        expect(TOKEN_TYPE.IDENTIFIER);
        func_call_node.name = (string)peek_token(-1).value;
        expect(TOKEN_TYPE.OPEN_PAREN);
        while(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
            if(accept(TOKEN_TYPE.IDENTIFIER)) {;}
            else if(accept(TOKEN_TYPE.INT_LITERAL)) {
                var arg = new AstNode_Arg();
                arg.token = peek_token(-1);
                func_call_node.children.Add(arg);
            }
            else if(accept(TOKEN_TYPE.FLOAT_LITERAL)) {
                var arg = new AstNode_Arg();
                arg.token = peek_token(-1);
                func_call_node.children.Add(arg);
            }
            else if(accept(TOKEN_TYPE.CHAR_LITERAL)) {
                var arg = new AstNode_Arg();
                arg.token = peek_token(-1);
                func_call_node.children.Add(arg);
            }
            else if(accept(TOKEN_TYPE.STRING_LITERAL)) {
                var arg = new AstNode_Arg();
                arg.token = peek_token(-1);
                func_call_node.children.Add(arg);
            }
            if(accept(TOKEN_TYPE.COMMA)) continue;

            expect(TOKEN_TYPE.CLOSE_PAREN);
            break;
        }
        expect(TOKEN_TYPE.SEMICOLON);
    }

    void type_specifier() {
        if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {
            if(accept(TOKEN_TYPE.KEYWORD_CHAR)) {;}
            else if(accept(TOKEN_TYPE.KEYWORD_SHORT)) {;}
            else if(accept(TOKEN_TYPE.KEYWORD_INT)) {;}
            else if(accept(TOKEN_TYPE.KEYWORD_LONG)) {;}
            else parse_error("Expected integer type", peek_token());
            return;
        }

        if(accept(TOKEN_TYPE.KEYWORD_CHAR)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {;}
        }
        else if(accept(TOKEN_TYPE.KEYWORD_SHORT)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {;}
        }
        else if(accept(TOKEN_TYPE.KEYWORD_INT)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {;}
        }
        else if(accept(TOKEN_TYPE.KEYWORD_LONG)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {;}
        }
        else if(accept(TOKEN_TYPE.KEYWORD_FLOAT)) {;}
        else if(accept(TOKEN_TYPE.KEYWORD_DOUBLE)) {;}
        else if(accept(TOKEN_TYPE.KEYWORD_VOID)) {;}
        else parse_error("Expected type", peek_token());
    }

    void declarator() {
        while(accept(TOKEN_TYPE.STAR)) {;}
        expect(TOKEN_TYPE.IDENTIFIER);
    }

    void compound_statement() {
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            block_item();
    }

    void block_item() {
        if(is_datatype(peek_token().type)) declaration();
        else statement();
    }

    void declaration() {
        type_specifier();
        init_declarator();
        expect(TOKEN_TYPE.SEMICOLON);
    }

    void init_declarator() {
        declarator();
        if(accept(TOKEN_TYPE.EQUALS)) {
            expression();
        }
    }

    void statement() {

    }

    // TODO: operator precedence
    void expression() {
        unary_expression();
        if(accept_binary_operator()) {
            expression();
        }
    }

    void unary_expression() {
        // accept()
    }

    bool accept_binary_operator() {
        if(accept(TOKEN_TYPE.PLUS)) return true;
        else if(accept(TOKEN_TYPE.MINUS)) return true;
        else if(accept(TOKEN_TYPE.STAR)) return true;
        else if(accept(TOKEN_TYPE.SLASH)) return true;
        else if(accept(TOKEN_TYPE.PROCENT)) return true;
        else if(accept(TOKEN_TYPE.SMALLER)) return true;
        else if(accept(TOKEN_TYPE.GREATER)) return true;
        else if(accept(TOKEN_TYPE.GREATER_EQUALS)) return true;
        else if(accept(TOKEN_TYPE.SMALLER_EQUALS)) return true;
        else if(accept(TOKEN_TYPE.EQUALS_EQUALS)) return true;
        else if(accept(TOKEN_TYPE.EXCLAM_EQUALS)) return true;
        else if(accept(TOKEN_TYPE.AND_AND)) return true;
        else if(accept(TOKEN_TYPE.OR_OR)) return true;
        else if(accept(TOKEN_TYPE.SMALLER_SMALLER)) return true;
        else if(accept(TOKEN_TYPE.GREATER_GREATER)) return true;
        else if(accept(TOKEN_TYPE.AND)) return true;
        else if(accept(TOKEN_TYPE.OR)) return true;
        else if(accept(TOKEN_TYPE.XOR)) return true;
        else return false;
    }

    // static int get_operator_precedence(TOKEN_TYPE op) {
    //     switch(op) {
    //         case TOKEN_TYPE.PLUS
    //         default: Compiler.assert(false, "Not an operator type");
    //     }
    // }

    static bool is_datatype(TOKEN_TYPE t) {
        return t == TOKEN_TYPE.KEYWORD_CHAR   ||
               t == TOKEN_TYPE.KEYWORD_DOUBLE ||
               t == TOKEN_TYPE.KEYWORD_FLOAT  ||
               t == TOKEN_TYPE.KEYWORD_INT    ||
               t == TOKEN_TYPE.KEYWORD_LONG   ||
               t == TOKEN_TYPE.KEYWORD_SHORT  ||
               t == TOKEN_TYPE.KEYWORD_VOID;
    }

    Token consume_token() {
        while(next_token_idx >= lexed_tokens.Count) {
            Token t;
            do {
                t = lexer.next_token();
            } while(t.type == TOKEN_TYPE.COMMENT);
            lexed_tokens.Add(t);
        }
        return lexed_tokens[next_token_idx++];
    }

    Token peek_token(int offset = 0) {
        int idx = next_token_idx + offset;
        Compiler.assert(idx >= 0, "idx has to be greater than zero");
        while(idx >= lexed_tokens.Count){
            Token t;
            do {
                t = lexer.next_token();
            } while(t.type == TOKEN_TYPE.COMMENT);
            lexed_tokens.Add(t);
        }
        return lexed_tokens[idx];
    }

    void expect(TOKEN_TYPE type) {
        if(!accept(type)) parse_error($"Expected {type} but got {peek_token().type}", peek_token());
    }

    bool accept(TOKEN_TYPE type) {
        if(peek_token().type == type) {
            consume_token();
            return true;
        }
        else return false;
    }

    void parse_error(string message, Token wrong_token) {
        Compiler.err_and_die($"Parse error: {message}\n{get_line_num_of_idx(wrong_token.loc_in_src)}: {get_line_at_idx(wrong_token.loc_in_src)}");
    }

    int get_line_num_of_idx(int idx) {
        int line_num = 1;
        for(int i = idx; i >= 0; --i) {
            if(text[i] == '\n') ++line_num;
        }
        return line_num;
    }

    string get_line_at_idx(int idx) {
        int line_start = idx;
        while(text[line_start] != '\n') {
            --line_start;
            if(line_start < 0) break;
        }
        // don't include the newline, or in the first line, increment from -1
        ++line_start;

        int line_end = idx;
        while(text[line_end] != '\n') {
            ++line_end;
            if(line_end == text.Length) break;
        }

        int idx_in_line = idx - line_start;
        string line = text[line_start..line_end];
        StringBuilder sb = new(line);
        sb.Insert(idx_in_line, '[');
        if(idx_in_line + 1 >= line.Length) sb.Append(']');
        else sb.Insert(idx_in_line + 2, ']');
        return sb.ToString();
    }
    // void expect_one_of_these(params TOKEN_TYPE[] types) {
    //     bool found_expected = false;
    //     foreach(var type in types) {
    //         if(accept(type)) found_expected = true;
    //     }
    //     if(!found_expected) {
    //         StringBuilder sb = new();
    //         sb.AppendJoin(", ", types);
    //         parse_error($"Expected one of the following: {sb.ToString()}, but got {peek_token().type}", peek_token());
    //     }
    // }
}
