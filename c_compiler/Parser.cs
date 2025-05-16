namespace c_compiler;

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
        return translation_unit();
    }

    AstNode translation_unit() {
        var translation_unit_node = new AstNode(AST_TYPE.TRANSLATION_UNIT);
        while(!accept(TOKEN_TYPE.EOF)) {
            translation_unit_node.children.Add(procedure_definition_or_declaration());
        }
        return translation_unit_node;
    }

    AstNode procedure_definition_or_declaration() {
        var type = type_specifier();
        var indirection_count = declarator();
        DataType return_type;
        return_type.type = type;
        return_type.indirection_count = indirection_count;

        var name = (string)peek_token(-1).value;
        var parameters = new List<DataType>();

        declared_procedures.Add(name);

        expect(TOKEN_TYPE.OPEN_PAREN);
        if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
            parameters = param_specifier();
            expect(TOKEN_TYPE.CLOSE_PAREN);
        }

        // declaration
        if(accept(TOKEN_TYPE.SEMICOLON)){
            ProcedureDecl proc_decl;
            proc_decl.name = name;
            proc_decl.return_type = return_type;
            proc_decl.parameters = parameters;
            return new AstNode(AST_TYPE.PROCEDURE_DECL, proc_decl);
        }
        // definition
        ProcedureDef proc_def;
        proc_def.name = name;
        proc_def.return_type = return_type;
        proc_def.parameters = parameters;
        var ret = new AstNode(AST_TYPE.PROCEDURE_DEF, proc_def);
        ret.children.Add(compound_statement());
        return ret;
    }

    List<DataType> param_specifier() {
        var parameters = new List<DataType>();
        do {
            DataType parameter;
            parameter.type = type_specifier();
            parameter.indirection_count = declarator();
            parameters.Add(parameter);
        } while(accept(TOKEN_TYPE.COMMA));
        return parameters;
        // if(accept(TOKEN_TYPE.COMMA)) param_specifier(parent);
        // Compiler.assert(parent.type == AST_TYPE.PROCEDURE_DEF, "param specifier should only be called with procedure definition");
    }

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

    AstNode compound_statement() {
        var ret = new AstNode(AST_TYPE.COMPOUND_STATEMENT);
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            ret.children.Add(statement());
        return ret;
    }

    AstNode declaration() {
        VarDecl var_decl;
        var_decl.type.type = type_specifier();
        var_decl.type.indirection_count = declarator();
        var_decl.name = (string)peek_token(-1).value;
        var ret = new AstNode(AST_TYPE.VAR_DECL, var_decl);
        if(accept(TOKEN_TYPE.EQUALS)) {
            ret.children.Add(expression(0));
        }
        return ret;
    }

    AstNode statement() {
        AstNode ret;
        var t = peek_token();
        // TODO: What do we pass as the parent? Do we still wanna do it like this? Or simply return nodes from the functions instead?
        // where do we add the expressions
        switch(t.type) {
            case TOKEN_TYPE.SEMICOLON:
                ret = new AstNode(AST_TYPE.EMPTY_STATEMENT);
                consume_token();
                break;
            case TOKEN_TYPE.KEYWORD_IF:
                ret = if_statement();
                break;
            case TOKEN_TYPE.KEYWORD_DO:
                ret = do_statement();
                break;
            case TOKEN_TYPE.KEYWORD_WHILE:
                ret = while_statement();
                break;
            case TOKEN_TYPE.KEYWORD_FOR:
                ret = for_statement();
                break;
            // TODO: switch statement... how are they done?
            // case TOKEN_TYPE.KEYWORD_SWITCH:
            //     ret = switch_statement();
            //     break;
            case TOKEN_TYPE.KEYWORD_GOTO:
                consume_token();
                ret = new AstNode(AST_TYPE.GOTO, peek_token().value);
                expect(TOKEN_TYPE.IDENTIFIER);
                expect(TOKEN_TYPE.SEMICOLON);
                break;

            case TOKEN_TYPE.OPEN_CURLY:
                ret = compound_statement();
                break;

            default:
                if(is_datatype(t.type)) {
                    ret = declaration();
                    expect(TOKEN_TYPE.SEMICOLON);
                }
                else if(t.type == TOKEN_TYPE.IDENTIFIER && peek_token(1).type == TOKEN_TYPE.COLON) {
                    ret = new AstNode(AST_TYPE.LABEL, peek_token().value);
                    // Or consume_token(); x2. But this is a little clearer. Even though it's two unnecessary checks
                    expect(TOKEN_TYPE.IDENTIFIER);
                    expect(TOKEN_TYPE.COLON);
                }
                else {
                    ret = expression(0);
                    expect(TOKEN_TYPE.SEMICOLON);
                }
                break;
        }
        return ret;
    }

    // AstNode switch_statement() {
    //     expect(TOKEN_TYPE.KEYWORD_SWITCH);
    // }

    AstNode if_statement() {
        AstNode ret = new AstNode(AST_TYPE.IF_STATEMENT);
        expect(TOKEN_TYPE.KEYWORD_IF);
        expect(TOKEN_TYPE.OPEN_PAREN);
        IfStmnt if_stmnt;
        if_stmnt.condition = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);

        ret.children.Add(statement());
        if(accept(TOKEN_TYPE.KEYWORD_ELSE)) {
            ret.children.Add(statement());
        }
        ret.value = if_stmnt;
        return ret;
    }

    AstNode do_statement() {
        AstNode ret = new AstNode(AST_TYPE.DO_STATEMENT);
        expect(TOKEN_TYPE.KEYWORD_DO);
        ret.children.Add(statement());
        expect(TOKEN_TYPE.KEYWORD_WHILE);
        expect(TOKEN_TYPE.OPEN_PAREN);
        DoStmnt do_stmnt;
        do_stmnt.condition = expression(0);
        ret.value = do_stmnt;
        expect(TOKEN_TYPE.CLOSE_PAREN);
        expect(TOKEN_TYPE.SEMICOLON);
        return ret;
    }

    AstNode while_statement() {
        AstNode ret = new AstNode(AST_TYPE.WHILE_STATEMENT);
        expect(TOKEN_TYPE.KEYWORD_WHILE);
        expect(TOKEN_TYPE.OPEN_PAREN);
        WhileStmnt while_stmnt;
        while_stmnt.condition = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        ret.value = while_stmnt;
        ret.children.Add(statement());
        return ret;
    }

    AstNode for_statement() {
        AstNode ret = new AstNode(AST_TYPE.FOR_STATEMENT);
        expect(TOKEN_TYPE.KEYWORD_FOR);
        expect(TOKEN_TYPE.OPEN_PAREN);

        ForStmnt for_stmnt;
        for_stmnt.before = expression(0);
        expect(TOKEN_TYPE.SEMICOLON);

        for_stmnt.condition = expression(0);
        expect(TOKEN_TYPE.SEMICOLON);

        for_stmnt.each_iter = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        ret.value = for_stmnt;
        ret.children.Add(statement());
        return ret;
    }

    List<AstNode> arguments() {
        var ret = new List<AstNode>();
        do {
            ret.Add(expression(0));
        } while(accept(TOKEN_TYPE.COMMA));
        return ret;
    }

    // TODO:
    // prefix operators: cast
    // infix operators: , . ->
    public AstNode expression(int min_bp) {
        var t = consume_token();
        AstNode left_hand_side;
        switch(t.type) {
            case TOKEN_TYPE.IDENTIFIER:
                var name = (string)t.value;
                if(accept(TOKEN_TYPE.OPEN_PAREN)) {
                    ProcedureCall proc_call;
                    proc_call.name = name;
                    left_hand_side = new AstNode(AST_TYPE.PROCEDURE_CALL, proc_call);
                    if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
                        left_hand_side.children = arguments();
                        expect(TOKEN_TYPE.CLOSE_PAREN);
                    }
                }
                // variable
                else {
                    Var variable;
                    variable.name = name;
                    left_hand_side = new AstNode(AST_TYPE.VAR, variable);
                }
                break;
            case TOKEN_TYPE.OPEN_PAREN:
                left_hand_side = expression(0);
                expect(TOKEN_TYPE.CLOSE_PAREN);
                break;
            case TOKEN_TYPE.INT_LITERAL:
                left_hand_side = new AstNode(AST_TYPE.INT_LITERAL, (long)t.value);
                break;
            case TOKEN_TYPE.FLOAT_LITERAL:
                left_hand_side = new AstNode(AST_TYPE.FLOAT_LITERAL, (float)t.value);
                break;
            case TOKEN_TYPE.CHAR_LITERAL:
                left_hand_side = new AstNode(AST_TYPE.CHAR_LITERAL, (long)t.value);
                break;
            case TOKEN_TYPE.STRING_LITERAL:
                left_hand_side = new AstNode(AST_TYPE.STRING_LITERAL, (string)t.value);
                break;
            default:
                var op = new AstNode(AST_TYPE.PREFIX_OPERATOR, t.type);
                var r_bp = get_prefix_binding_power(t.type);

                if(r_bp == -1)
                    parse_error("Expected identifier, literal or prefix operator", peek_token(-1));

                var right_hand_side = expression(r_bp);
                op.children.Add(right_hand_side);
                left_hand_side = op;
                break;
        }
        while(true) {
            // if(terminating_token_types.Contains(peek_token().type))
            //     break;

            var op_type = peek_token().type;
            
            var l_bp = get_postfix_binding_power(op_type);
            if(l_bp != -1) {
                if(l_bp < min_bp) break;
                consume_token();
                var postfix_op = new AstNode(AST_TYPE.POSTFIX_OPERATOR, op_type);
                postfix_op.children.Add(left_hand_side);
                left_hand_side = postfix_op;
                continue;
            }
            
            (l_bp, var r_bp) = get_infix_binding_power(op_type);
            if ((l_bp, r_bp) != (-1, -1)) {
                if(l_bp < min_bp) break;
                
                consume_token();
                var right_hand_side = expression(r_bp);
                
                var op = new AstNode(AST_TYPE.INFIX_OPERATOR, op_type);
                op.children.Add(left_hand_side);
                op.children.Add(right_hand_side);
                left_hand_side = op;
                continue; 
            }
            break;
        }
        return left_hand_side;
    }

    int get_postfix_binding_power(TOKEN_TYPE t) {
        switch(t) {
            case TOKEN_TYPE.PLUS_PLUS:
            case TOKEN_TYPE.MINUS_MINUS: return 24;
            default: return -1;
        }
    }

    // return -1 if its not a valid prefix operator
    int get_prefix_binding_power(TOKEN_TYPE t) {
        switch(t) {
            // TODO: (cast)
            case TOKEN_TYPE.PLUS:
            case TOKEN_TYPE.MINUS:
            case TOKEN_TYPE.PLUS_PLUS:
            case TOKEN_TYPE.MINUS_MINUS:
            case TOKEN_TYPE.EXCLAM:
            case TOKEN_TYPE.NOT:
            case TOKEN_TYPE.STAR:
            case TOKEN_TYPE.AND:
            case TOKEN_TYPE.KEYWORD_SIZEOF: return 23;
            default:
                return -1;
        }
    }

    // return (-1, -1) if it's not a valid infix operator
    (int, int) get_infix_binding_power(TOKEN_TYPE t) {
        switch(t) {
            // TODO: comma
            // Low binding power, associativity right to left
            case TOKEN_TYPE.EQUALS:                           // =
            case TOKEN_TYPE.PLUS_EQUALS:                      // +=
            case TOKEN_TYPE.MINUS_EQUALS:                     // -=
            case TOKEN_TYPE.STAR_EQUALS:                      // *=
            case TOKEN_TYPE.SLASH_EQUALS:                     // /=
            case TOKEN_TYPE.PROCENT_EQUALS:                   // %=
            case TOKEN_TYPE.RIGHT_SHIFT_EQUALS:               // >>=
            case TOKEN_TYPE.LEFT_SHIFT_EQUALS:                // <<=
            case TOKEN_TYPE.AND_EQUALS:                       // &=
            case TOKEN_TYPE.OR_EQUALS:                        // |=
            case TOKEN_TYPE.XOR_EQUALS: return (2, 1);        // ^=

            // left to right associativity
            case TOKEN_TYPE.OR_OR:   return (3, 4);           // ||
            case TOKEN_TYPE.AND_AND: return (5, 6);           // &&
            case TOKEN_TYPE.OR:      return (7, 8);           // |
            case TOKEN_TYPE.XOR:     return (9, 10);          // ^
            case TOKEN_TYPE.AND:     return (11, 12);         // &

            case TOKEN_TYPE.EXCLAM_EQUALS:                    // !=
            case TOKEN_TYPE.EQUALS_EQUALS: return (13, 14);   // ==

            case TOKEN_TYPE.SMALLER:                          // <
            case TOKEN_TYPE.SMALLER_EQUALS:                   // <=
            case TOKEN_TYPE.GREATER:                          // >
            case TOKEN_TYPE.GREATER_EQUALS: return (15, 16);  // >=

            case TOKEN_TYPE.GREATER_GREATER:                  // >>
            case TOKEN_TYPE.SMALLER_SMALLER: return (17, 18); // <<

            case TOKEN_TYPE.PLUS:                             // +
            case TOKEN_TYPE.MINUS: return (19, 20);           // -

            case TOKEN_TYPE.STAR:                             // *
            case TOKEN_TYPE.SLASH:                            // /
            case TOKEN_TYPE.PROCENT: return (21, 22);         // %
            default:
                return (-1,-1);
        }
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
        return line;
    }
}
