namespace compiler_csharp;

public class Parser {
    string text;
    Lexer lexer;

    List<Token> lexed_tokens = new();
    int next_token_idx = 0;

    List<string> declared_procedures = new();


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
        proc_def.return_type.type = type_specifier();
        proc_def.return_type.indirection_count = declarator();
        proc_def.name = (string)peek_token(-1).value;
        proc_def.parameters = new();

        declared_procedures.Add(proc_def.name);
        var node = new AstNode(AST_TYPE.PROCEDURE_DEF, proc_def);

        expect(TOKEN_TYPE.OPEN_PAREN);
        if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
            param_specifier(node);
            expect(TOKEN_TYPE.CLOSE_PAREN);
        }

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

    void param_specifier(AstNode parent) {
        DataType param_type;
        param_type.type = type_specifier();
        param_type.indirection_count = declarator();
        Compiler.assert(parent.type == AST_TYPE.PROCEDURE_DEF, "param specifier should only be called with procedure definition");
        ((ProcedureDef)parent.value).parameters.Add(param_type);
        if(accept(TOKEN_TYPE.COMMA)) param_specifier(parent);
    }

    // void test_procedure_call(AstNode parent) {
    //     ProcedureCall proc_call;
    //     // this function should be deleted later, just for some testing
    //     expect(TOKEN_TYPE.IDENTIFIER);
    //     proc_call.name = (string)peek_token(-1).value;
    //     if(!declared_procedures.Contains(proc_call.name)) {
    //         parse_error($"Use of undeclared identifier \"{proc_call.name}\"", peek_token(-1));
    //     }
    //     var node = new AstNode(AST_TYPE.PROCEDURE_CALL, proc_call);
    //     parent.children.Add(node);
    //
    //     expect(TOKEN_TYPE.OPEN_PAREN);
    //     while(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
    //         if(accept(TOKEN_TYPE.IDENTIFIER)    ||
    //            accept(TOKEN_TYPE.INT_LITERAL)   ||
    //            accept(TOKEN_TYPE.FLOAT_LITERAL) ||
    //            accept(TOKEN_TYPE.CHAR_LITERAL)  ||
    //            accept(TOKEN_TYPE.STRING_LITERAL)) {
    //             Arg arg;
    //             arg.token = peek_token(-1);
    //             node.children.Add(new AstNode(AST_TYPE.ARG, arg));
    //         }
    //         if(accept(TOKEN_TYPE.COMMA)) continue;
    //
    //         expect(TOKEN_TYPE.CLOSE_PAREN);
    //         break;
    //     }
    //     expect(TOKEN_TYPE.SEMICOLON);
    // }

    DATA_TYPE type_specifier() {
        if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED)) {
            if(accept(TOKEN_TYPE.KEYWORD_CHAR)) return DATA_TYPE.UNSIGNED_CHAR;
            else if(accept(TOKEN_TYPE.KEYWORD_SHORT)) return DATA_TYPE.UNSIGNED_SHORT;
            else if(accept(TOKEN_TYPE.KEYWORD_INT)) return DATA_TYPE.UNSIGNED_INT;
            else if(accept(TOKEN_TYPE.KEYWORD_LONG)) return DATA_TYPE.UNSIGNED_LONG;
            else parse_error("Expected integer type", peek_token());
            // NOTE: unreachable
            return DATA_TYPE.VOID;
        }

        if(accept(TOKEN_TYPE.KEYWORD_CHAR)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED))
                return DATA_TYPE.UNSIGNED_CHAR;
            return DATA_TYPE.CHAR;
        }
        else if(accept(TOKEN_TYPE.KEYWORD_SHORT)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED))
                return DATA_TYPE.UNSIGNED_SHORT;
            return DATA_TYPE.SHORT;
        }
        else if(accept(TOKEN_TYPE.KEYWORD_INT)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED))
                return DATA_TYPE.UNSIGNED_INT;
            return DATA_TYPE.INT;
        }
        else if(accept(TOKEN_TYPE.KEYWORD_LONG)) {
            if(accept(TOKEN_TYPE.KEYWORD_UNSIGNED))
                return DATA_TYPE.UNSIGNED_LONG;
            return DATA_TYPE.LONG;
        }
        else if(accept(TOKEN_TYPE.KEYWORD_FLOAT)) return DATA_TYPE.FLOAT;
        else if(accept(TOKEN_TYPE.KEYWORD_DOUBLE)) return DATA_TYPE.DOUBLE;
        else if(accept(TOKEN_TYPE.KEYWORD_VOID)) return DATA_TYPE.VOID;
        else parse_error("Expected type", peek_token());
        // NOTE: unreachable
        return DATA_TYPE.VOID;
    }

    int declarator() {
        int indirection_count = 0;
        while(accept(TOKEN_TYPE.STAR)) ++indirection_count;
        expect(TOKEN_TYPE.IDENTIFIER);
        return indirection_count;
    }

    void compound_statement(AstNode parent) {
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            statement(parent);
    }

    void declaration(AstNode parent) {
        VarDecl var_decl;
        var_decl.type.type = type_specifier();
        var_decl.type.indirection_count = declarator();
        var_decl.name = (string)peek_token(-1).value;
        var node = new AstNode(AST_TYPE.VAR_DECL, var_decl);
        parent.children.Add(node);
        if(accept(TOKEN_TYPE.EQUALS)) {
            // TODO: this should be an expression
            expression(node);
            // if(accept(TOKEN_TYPE.INT_LITERAL)) {
            //     DATA_TYPE[] int_types = {
            //         DATA_TYPE.CHAR,
            //         DATA_TYPE.SHORT,
            //         DATA_TYPE.INT,
            //         DATA_TYPE.LONG,
            //         DATA_TYPE.UNSIGNED_CHAR,
            //         DATA_TYPE.UNSIGNED_SHORT,
            //         DATA_TYPE.UNSIGNED_INT,
            //         DATA_TYPE.UNSIGNED_LONG
            //     };
            //     if(!int_types.Contains(var_decl.type.type))
            //         parse_error($"Cannot assign integer literal to variable of type: {var_decl.type.type}", peek_token(-1));
            // }
            // else if(accept(TOKEN_TYPE.FLOAT_LITERAL)) {
            //     DATA_TYPE[] floating_point_lit = {
            //         DATA_TYPE.FLOAT,
            //         DATA_TYPE.DOUBLE
            //     };
            //     if(!floating_point_lit.Contains(var_decl.type.type))
            //         parse_error($"Cannot assign floating point literal to variable of type: {var_decl.type.type}", peek_token(-1));
            // }
            // else {
            //     parse_error($"Cannot assign token of type {peek_token(-1).type} to variable of type {var_decl.type.type}", peek_token(-1));
            // }
            // var_decl.init_value = peek_token(-1).value;
        }
        expect(TOKEN_TYPE.SEMICOLON);
    }

    void statement(AstNode parent) {
        if(is_datatype(peek_token().type)) declaration(parent);
        else expression(parent);//test_procedure_call(parent);
    }

    // TODO: operator precedence
    void expression(AstNode parent) {
        AstNode left_hand_side;
        if(accept(TOKEN_TYPE.IDENTIFIER)) {
            string name = (string)peek_token(-1).value;
            // procedure call
            if(accept(TOKEN_TYPE.OPEN_PAREN)) {
                ProcedureCall proc_call;
                proc_call.name = name;
                proc_call.args = new();
                left_hand_side = new AstNode(AST_TYPE.PROCEDURE_CALL, proc_call);
                if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
                    arguments(left_hand_side);
                    expect(TOKEN_TYPE.CLOSE_PAREN);
                }
            }
            // variable
            else {
                Var variable;
                variable.name = name;
                left_hand_side = new AstNode(AST_TYPE.VAR, variable);
            }
        }
        else if(accept(TOKEN_TYPE.INT_LITERAL)) {
            left_hand_side = new AstNode(AST_TYPE.INT_LITERAL, (long)peek_token(-1).value);
        }
        else if(accept(TOKEN_TYPE.FLOAT_LITERAL)) {
            left_hand_side = new AstNode(AST_TYPE.FLOAT_LITERAL, (float)peek_token(-1).value);
        }
        else if(accept(TOKEN_TYPE.CHAR_LITERAL)) {
            left_hand_side = new AstNode(AST_TYPE.CHAR_LITERAL, (long)peek_token(-1).value);
        }
        else if(accept(TOKEN_TYPE.STRING_LITERAL)) {
            left_hand_side = new AstNode(AST_TYPE.STRING_LITERAL, (string)peek_token(-1).value);
        }
        else {
            parse_error("Exprected identifier or literal", peek_token(-1));
            // unreachable
            left_hand_side = null!;
        }
        if(is_binary_operator(peek_token().type)) {
            BinaryOperator bin_op;
            bin_op.type = consume_token().type;
            AstNode op = new(AST_TYPE.BINARY_OPERATOR, bin_op);
            parent.children.Add(op);
            op.children.Add(left_hand_side);
            expression(op);
        }
        else {
            parent.children.Add(left_hand_side);
            expect(TOKEN_TYPE.SEMICOLON);
        }
    }

    void arguments(AstNode parent) {
        Compiler.assert(parent.type == AST_TYPE.PROCEDURE_CALL, "Should only be called with procedure call");
        // We don't know the type yet
        AstNode arg = new((AST_TYPE)(-1));
        ((ProcedureCall)parent.value).args.Add(arg);
        expression(arg);
        if(accept(TOKEN_TYPE.COMMA))
            arguments(parent);
    }

    // Gets the data type from the most recent collected token, if the most recent token is not a type then return -1
    // DATA_TYPE get_data_type_from_prev_tokens() {
    //     switch(peek_token(-1).type) {
    //         case TOKEN_TYPE.KEYWORD_UNSIGNED: {
    //             switch(peek_token(-2).type) {
    //                 case TOKEN_TYPE.KEYWORD_CHAR:
    //                     return DATA_TYPE.UNSIGNED_CHAR;
    //                 case TOKEN_TYPE.KEYWORD_SHORT:
    //                     return DATA_TYPE.UNSIGNED_SHORT;
    //                 case TOKEN_TYPE.KEYWORD_INT:
    //                     return DATA_TYPE.UNSIGNED_INT;
    //                 case TOKEN_TYPE.KEYWORD_LONG:
    //                     return DATA_TYPE.UNSIGNED_LONG;
    //                 default:
    //                     return (DATA_TYPE)(-1);
    //             }
    //         }
    //         case TOKEN_TYPE.KEYWORD_CHAR: {
    //             if(next_token_idx - 2 >= 0) {
    //                 if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
    //                     return DATA_TYPE.UNSIGNED_CHAR;
    //             }
    //             return DATA_TYPE.CHAR;
    //         }
    //         case TOKEN_TYPE.KEYWORD_SHORT: {
    //             if(next_token_idx - 2 >= 0) {
    //                 if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
    //                     return DATA_TYPE.UNSIGNED_SHORT;
    //             }
    //             return DATA_TYPE.SHORT;
    //         }
    //         case TOKEN_TYPE.KEYWORD_INT: {
    //             if(next_token_idx - 2 >= 0) {
    //                 if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
    //                     return DATA_TYPE.UNSIGNED_INT;
    //             }
    //             return DATA_TYPE.INT;
    //         }
    //         case TOKEN_TYPE.KEYWORD_LONG: {
    //             if(next_token_idx - 2 >= 0) {
    //                 if(peek_token(-2).type == TOKEN_TYPE.KEYWORD_UNSIGNED)
    //                     return DATA_TYPE.UNSIGNED_LONG;
    //             }
    //             return DATA_TYPE.LONG;
    //         }
    //         case TOKEN_TYPE.KEYWORD_FLOAT:
    //             return DATA_TYPE.FLOAT;
    //         case TOKEN_TYPE.KEYWORD_DOUBLE:
    //             return DATA_TYPE.DOUBLE;
    //         case TOKEN_TYPE.KEYWORD_VOID:
    //             return DATA_TYPE.VOID;
    //         default:
    //             return (DATA_TYPE)(-1);
    //     }
    // }


    // TOKEN_TYPE accept_binary_operator(AstNode parent) {
    //     if(accept(TOKEN_TYPE.PLUS)) return true;
    //     else if(accept(TOKEN_TYPE.MINUS)) return true;
    //     else if(accept(TOKEN_TYPE.STAR)) return true;
    //     else if(accept(TOKEN_TYPE.SLASH)) return true;
    //     else if(accept(TOKEN_TYPE.PROCENT)) return true;
    //     else if(accept(TOKEN_TYPE.SMALLER)) return true;
    //     else if(accept(TOKEN_TYPE.GREATER)) return true;
    //     else if(accept(TOKEN_TYPE.GREATER_EQUALS)) return true;
    //     else if(accept(TOKEN_TYPE.SMALLER_EQUALS)) return true;
    //     else if(accept(TOKEN_TYPE.EQUALS_EQUALS)) return true;
    //     else if(accept(TOKEN_TYPE.EXCLAM_EQUALS)) return true;
    //     else if(accept(TOKEN_TYPE.AND_AND)) return true;
    //     else if(accept(TOKEN_TYPE.OR_OR)) return true;
    //     else if(accept(TOKEN_TYPE.SMALLER_SMALLER)) return true;
    //     else if(accept(TOKEN_TYPE.GREATER_GREATER)) return true;
    //     else if(accept(TOKEN_TYPE.AND)) return true;
    //     else if(accept(TOKEN_TYPE.OR)) return true;
    //     else if(accept(TOKEN_TYPE.XOR)) return true;
    //     else return (TOKEN_TYPE)(-1);
    // }

    // static int get_operator_precedence(TOKEN_TYPE op) {
    //     switch(op) {
    //         case TOKEN_TYPE.PLUS
    //         default: Compiler.assert(false, "Not an operator type");
    //     }
    // }
    static bool is_binary_operator(TOKEN_TYPE t) {
        return t == TOKEN_TYPE.PLUS            ||
               t == TOKEN_TYPE.MINUS           ||
               t == TOKEN_TYPE.STAR            ||
               t == TOKEN_TYPE.SLASH           ||
               t == TOKEN_TYPE.PROCENT         ||
               t == TOKEN_TYPE.SMALLER         ||
               t == TOKEN_TYPE.GREATER         ||
               t == TOKEN_TYPE.EQUALS          ||
               t == TOKEN_TYPE.EQUALS_EQUALS   ||
               t == TOKEN_TYPE.GREATER_EQUALS  ||
               t == TOKEN_TYPE.SMALLER_EQUALS  ||
               t == TOKEN_TYPE.EQUALS_EQUALS   ||
               t == TOKEN_TYPE.EXCLAM_EQUALS   ||
               t == TOKEN_TYPE.AND_AND         ||
               t == TOKEN_TYPE.OR_OR           ||
               t == TOKEN_TYPE.SMALLER_SMALLER ||
               t == TOKEN_TYPE.GREATER_GREATER ||
               t == TOKEN_TYPE.AND             ||
               t == TOKEN_TYPE.OR              ||
               t == TOKEN_TYPE.XOR;
    }

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
