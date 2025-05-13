namespace compiler_csharp;

public class Parser {
    string text;
    Lexer lexer;

    List<Token> lexed_tokens = new();
    int next_token_idx = 0;

    List<string> declared_procedures = new();
    List<string> declared_global_variables = new();

    public Parser(string source_code) {
        text = source_code;
        lexer = new(text);
    }

    public AstNode parse() {
        // Recursive descent parser. Each method corresponds to one non-terminal of the grammar
        return translation_unit();
    }

    AstNode translation_unit() {
        var translation_unit_node = new AstNode(AST_TYPE.TRANSLATION_UNIT);
        while(!accept(TOKEN_TYPE.EOF)) {
            procedure_definition_or_declaration(translation_unit_node);
        }
        return translation_unit_node;
    }

    void procedure_definition_or_declaration(AstNode parent) {
        ProcedureDef proc_def;
        type_specifier();
        proc_def.return_type = get_data_type_from_prev_tokens();
        declarator();
        proc_def.name = (string)peek_token(-1).value;

        declared_procedures.Add(proc_def.name);
        var node = new AstNode(AST_TYPE.PROCEDURE_DEF, proc_def);

        expect(TOKEN_TYPE.OPEN_PAREN);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        // declaration
        // TODO: add node in ast?
        if(accept(TOKEN_TYPE.SEMICOLON)){
            return;
        }
        // definition
        else {
            parent.children.Add(node);
        }
        compound_statement(node);
        // expect(TOKEN_TYPE.OPEN_CURLY);
        // while(!accept(TOKEN_TYPE.CLOSE_CURLY))
        //     test_procedure_call(proc_def_node);
    }

    void test_procedure_call(AstNode parent) {
        ProcedureCall proc_call;
        // this function should be deleted later, just for some testing
        expect(TOKEN_TYPE.IDENTIFIER);
        proc_call.name = (string)peek_token(-1).value;
        if(!declared_procedures.Contains(proc_call.name)) {
            parse_error($"Use of undeclared identifier \"{proc_call.name}\"", peek_token(-1));
        }
        var node = new AstNode(AST_TYPE.PROCEDURE_CALL, proc_call);
        parent.children.Add(node);

        expect(TOKEN_TYPE.OPEN_PAREN);
        while(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
            if(accept(TOKEN_TYPE.IDENTIFIER)    ||
               accept(TOKEN_TYPE.INT_LITERAL)   ||
               accept(TOKEN_TYPE.FLOAT_LITERAL) ||
               accept(TOKEN_TYPE.CHAR_LITERAL)  ||
               accept(TOKEN_TYPE.STRING_LITERAL)) {
                Arg arg;
                arg.token = peek_token(-1);
                node.children.Add(new AstNode(AST_TYPE.ARG, arg));
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

    void compound_statement(AstNode parent) {
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            statement(parent);
    }

    void declaration(AstNode parent) {
        VarDecl var_decl;
        type_specifier();
        var_decl.type = get_data_type_from_prev_tokens();
        declarator();
        var_decl.name = (string)peek_token(-1).value;
        if(accept(TOKEN_TYPE.EQUALS)) {
            // TODO: this should be an expression
            // expression(node);
            if(accept(TOKEN_TYPE.INT_LITERAL)) {
                DATA_TYPE[] int_types = {
                    DATA_TYPE.CHAR,
                    DATA_TYPE.SHORT,
                    DATA_TYPE.INT,
                    DATA_TYPE.LONG,
                    DATA_TYPE.UNSIGNED_CHAR,
                    DATA_TYPE.UNSIGNED_SHORT,
                    DATA_TYPE.UNSIGNED_INT,
                    DATA_TYPE.UNSIGNED_LONG
                };
                if(!int_types.Contains(var_decl.type))
                    parse_error($"Cannot assign integer literal to variable of type: {var_decl.type}", peek_token(-1));
            }
            else if(accept(TOKEN_TYPE.FLOAT_LITERAL)) {
                DATA_TYPE[] floating_point_lit = {
                    DATA_TYPE.FLOAT,
                    DATA_TYPE.DOUBLE
                };
                if(!floating_point_lit.Contains(var_decl.type))
                    parse_error($"Cannot assign floating point literal to variable of type: {var_decl.type}", peek_token(-1));
            }
            else {
                parse_error($"Cannot assign token of type {peek_token(-1).type} to variable of type {var_decl.type}", peek_token(-1));
            }
            var_decl.init_value = peek_token(-1).value;
        }
        else {
            var_decl.init_value = null;
        }
        expect(TOKEN_TYPE.SEMICOLON);
        var node = new AstNode(AST_TYPE.VAR_DECL, var_decl);
        parent.children.Add(node);
    }

    void statement(AstNode parent) {
        if(is_datatype(peek_token().type)) declaration(parent);
        else test_procedure_call(parent);
    }

    // TODO: operator precedence
    void expression(AstNode parent) {
        if(accept(TOKEN_TYPE.IDENTIFIER)    ||
           accept(TOKEN_TYPE.INT_LITERAL)   ||
           accept(TOKEN_TYPE.FLOAT_LITERAL) ||
           accept(TOKEN_TYPE.CHAR_LITERAL)  ||
           accept(TOKEN_TYPE.STRING_LITERAL))
        {
            if(accept_binary_operator()) {
                // expression();
            }
        }
        expect(TOKEN_TYPE.SEMICOLON);
    }

    // Gets the data type from the most recent collected token, if the most recent token is not a type then return -1
    DATA_TYPE get_data_type_from_prev_tokens() {
        switch(peek_token(-1).type) {
            case TOKEN_TYPE.KEYWORD_UNSIGNED: {
                switch(peek_token(-2).type) {
                    case TOKEN_TYPE.KEYWORD_CHAR:
                        return DATA_TYPE.UNSIGNED_CHAR;
                    case TOKEN_TYPE.KEYWORD_SHORT:
                        return DATA_TYPE.UNSIGNED_SHORT;
                    case TOKEN_TYPE.KEYWORD_INT:
                        return DATA_TYPE.UNSIGNED_INT;
                    case TOKEN_TYPE.KEYWORD_LONG:
                        return DATA_TYPE.UNSIGNED_LONG;
                    default:
                        return (DATA_TYPE)(-1);
                }
            }
            case TOKEN_TYPE.KEYWORD_CHAR: {
                if(next_token_idx - 2 >= 0) {
                    if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
                        return DATA_TYPE.UNSIGNED_CHAR;
                }
                return DATA_TYPE.CHAR;
            }
            case TOKEN_TYPE.KEYWORD_SHORT: {
                if(next_token_idx - 2 >= 0) {
                    if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
                        return DATA_TYPE.UNSIGNED_SHORT;
                }
                return DATA_TYPE.SHORT;
            }
            case TOKEN_TYPE.KEYWORD_INT: {
                if(next_token_idx - 2 >= 0) {
                    if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
                        return DATA_TYPE.UNSIGNED_INT;
                }
                return DATA_TYPE.INT;
            }
            case TOKEN_TYPE.KEYWORD_LONG: {
                if(next_token_idx - 2 >= 0) {
                    if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
                        return DATA_TYPE.UNSIGNED_LONG;
                }
                return DATA_TYPE.LONG;
            }
            case TOKEN_TYPE.KEYWORD_FLOAT:
                return DATA_TYPE.FLOAT;
            case TOKEN_TYPE.KEYWORD_DOUBLE:
                return DATA_TYPE.DOUBLE;
            case TOKEN_TYPE.KEYWORD_VOID:
                return DATA_TYPE.VOID;
            default:
                return (DATA_TYPE)(-1);
        }
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
        return t == TOKEN_TYPE.KEYWORD_CHAR     ||
               t == TOKEN_TYPE.KEYWORD_DOUBLE   ||
               t == TOKEN_TYPE.KEYWORD_FLOAT    ||
               t == TOKEN_TYPE.KEYWORD_INT      ||
               t == TOKEN_TYPE.KEYWORD_LONG     ||
               t == TOKEN_TYPE.KEYWORD_SHORT    ||
               t == TOKEN_TYPE.KEYWORD_UNSIGNED ||
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
        var err_message = $"{message}\n{get_line_num_of_idx(wrong_token.loc_in_src)}: {get_line_at_idx(wrong_token.loc_in_src)}";
        Compiler.err_and_die(err_message);
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
        // StringBuilder sb = new(line);
        // sb.Insert(idx_in_line, '[');
        // if(idx_in_line + 1 >= line.Length) sb.Append(']');
        // else sb.Insert(idx_in_line + 2, ']');
        return line;
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
