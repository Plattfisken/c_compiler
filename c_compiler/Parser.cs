using System.Globalization;
using System.Text;

namespace c_compiler;

public class Parser {
    string text;
    Lexer lexer;

    List<Token> lexed_tokens = new();
    int next_token_idx = 0;

    public Parser(string source_code) {
        text = source_code;
        lexer = new(text);
    }

    public AstNode parse() {
        return translation_unit();
    }

    AstNode translation_unit() {
        var translation_unit_node = new TranslationUnit();
        while(!accept(TOKEN_TYPE.EOF)) {
            translation_unit_node.children.Add(proc_def_or_top_level_decl());
        }
        return translation_unit_node;
    }

    AstNode proc_def_or_top_level_decl() {
        var t = type_specifier();
        var i = declarator();
        var type = new DataType(t, i);

        var name = (string)peek_token(-1).value;
        DataType[] parameters = [];
        bool variable_length = false;

        // global var
        if(accept(TOKEN_TYPE.SEMICOLON)) {
            return new VarDecl(name, type);
        }
        else if(accept(TOKEN_TYPE.EQUALS)) {
            return new VarDecl(name, type, [expression(0)]);
        }

        // procedure
        expect(TOKEN_TYPE.OPEN_PAREN);
        if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
            (parameters, variable_length) = param_specifier();
            expect(TOKEN_TYPE.CLOSE_PAREN);
        }
        // declaration
        if(accept(TOKEN_TYPE.SEMICOLON)){
            return new ProcedureDecl(name, type, parameters, variable_length);
        }
        // definition
        return new ProcedureDef(name, type, parameters, variable_length, compound_statement().children);
    }

    (DataType[], bool) param_specifier() {
        var parameters = new List<DataType>();
        var variable_length = false;
        do {
            if(accept(TOKEN_TYPE.ELLIPSIS)) {
                variable_length = true;
                break;
            }
            var t = type_specifier();
            var parameter = new DataType(t);
            while(accept(TOKEN_TYPE.STAR))
                ++parameter.indirection_count;

            accept(TOKEN_TYPE.IDENTIFIER);
            parameters.Add(parameter);
        } while(accept(TOKEN_TYPE.COMMA));
        return (parameters.ToArray(), variable_length);
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
        var ret = new CompoundStatement();
        expect(TOKEN_TYPE.OPEN_CURLY);
        while(!accept(TOKEN_TYPE.CLOSE_CURLY))
            ret.children.Add(statement());
        return ret;
    }

    AstNode declaration() {
        var type = type_specifier();
        var indirection_count = declarator();
        var name = (string)peek_token(-1).value;

        if(accept(TOKEN_TYPE.EQUALS))
            return new VarDecl(name, new DataType(type, indirection_count), [expression(0)]);

        return new VarDecl(name, new DataType(type, indirection_count));
    }

    AstNode statement() {
        var t = peek_token();
        switch(t.type) {
            case TOKEN_TYPE.SEMICOLON:
                consume_token();
                return new EmptyStatement();
            case TOKEN_TYPE.KEYWORD_IF:
                return if_statement();
            case TOKEN_TYPE.KEYWORD_DO:
                return do_statement();
            case TOKEN_TYPE.KEYWORD_WHILE:
                return while_statement();
            case TOKEN_TYPE.KEYWORD_FOR:
                return for_statement();
            // TODO: switch statement... how are they done?
            // case TOKEN_TYPE.KEYWORD_SWITCH:
            //     ret = switch_statement();
            //     break;
            case TOKEN_TYPE.KEYWORD_GOTO:
                consume_token();
                var name = (string)peek_token().value;
                expect(TOKEN_TYPE.IDENTIFIER);
                expect(TOKEN_TYPE.SEMICOLON);
                return new Goto(name);

            case TOKEN_TYPE.OPEN_CURLY:
                return compound_statement();

            default:
                if(is_datatype(t.type)) {
                    var ret = declaration();
                    expect(TOKEN_TYPE.SEMICOLON);
                    return ret;
                }
                else if(t.type == TOKEN_TYPE.IDENTIFIER && peek_token(1).type == TOKEN_TYPE.COLON) {
                    var ret = new Label((string)peek_token().value);
                    // Or consume_token(); x2. But this is a little clearer. Even though it's two unnecessary checks
                    expect(TOKEN_TYPE.IDENTIFIER);
                    expect(TOKEN_TYPE.COLON);
                    return ret;
                }
                else {
                    var ret = expression(0);
                    expect(TOKEN_TYPE.SEMICOLON);
                    return ret;
                }
        }
    }

    // AstNode switch_statement() {
    //     expect(TOKEN_TYPE.KEYWORD_SWITCH);
    // }

    AstNode if_statement() {
        expect(TOKEN_TYPE.KEYWORD_IF);
        expect(TOKEN_TYPE.OPEN_PAREN);
        var condition = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);

        var ret = new IfStatement(condition);
        var if_case = statement();
        if(accept(TOKEN_TYPE.KEYWORD_ELSE)) {
            var else_case = statement();
            return new IfStatement(condition, [if_case, else_case]);
        }
        return new IfStatement(condition, [if_case]);
    }

    AstNode do_statement() {
        expect(TOKEN_TYPE.KEYWORD_DO);
        var stmnt = statement();
        expect(TOKEN_TYPE.KEYWORD_WHILE);
        expect(TOKEN_TYPE.OPEN_PAREN);
        var condition = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        expect(TOKEN_TYPE.SEMICOLON);
        return new DoStatement(condition, [stmnt]);
    }

    AstNode while_statement() {
        expect(TOKEN_TYPE.KEYWORD_WHILE);
        expect(TOKEN_TYPE.OPEN_PAREN);
        var condition = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        var stmnt = statement();
        return new WhileStatement(condition, [stmnt]);
    }

    AstNode for_statement() {
        expect(TOKEN_TYPE.KEYWORD_FOR);
        expect(TOKEN_TYPE.OPEN_PAREN);

        var before = is_datatype(peek_token().type) ? declaration() : expression(0);
        expect(TOKEN_TYPE.SEMICOLON);

        var condition = expression(0);
        expect(TOKEN_TYPE.SEMICOLON);

        var each_iter = expression(0);
        expect(TOKEN_TYPE.CLOSE_PAREN);
        var stmnt = statement();
        return new ForStatement(before, condition, each_iter, [stmnt]);
    }

    List<AstNode> arguments() {
        var ret = new List<AstNode>();
        do {
            ret.Add(expression(0, break_at_comma: true));
        } while(accept(TOKEN_TYPE.COMMA));
        return ret;
    }

    // TODO: prefix operator (cast)
    public AstNode expression(int min_bp, bool break_at_comma = false) {
        var t = consume_token();
        AstNode left_hand_side;
        switch(t.type) {
            case TOKEN_TYPE.IDENTIFIER:
                var name = (string)t.value;
                if(accept(TOKEN_TYPE.OPEN_PAREN)) {
                    left_hand_side = new ProcedureCall(name);
                    if(!accept(TOKEN_TYPE.CLOSE_PAREN)) {
                        left_hand_side.children = arguments();
                        expect(TOKEN_TYPE.CLOSE_PAREN);
                    }
                }
                // variable
                else {
                    left_hand_side = new Var(name);
                }
                break;
            case TOKEN_TYPE.OPEN_PAREN:
                left_hand_side = expression(0);
                expect(TOKEN_TYPE.CLOSE_PAREN);
                break;
            case TOKEN_TYPE.INT_LITERAL:
                left_hand_side = new IntLiteral((long)t.value);
                break;
            case TOKEN_TYPE.FLOAT_LITERAL:
                left_hand_side = new FloatLiteral((float)t.value);
                break;
            case TOKEN_TYPE.CHAR_LITERAL:
                left_hand_side = new CharLiteral((char)(long)t.value);
                break;
            case TOKEN_TYPE.STRING_LITERAL:
                left_hand_side = new StringLiteral((string)t.value);
                break;
            default:
                var op = new PrefixOperator(t.type);
                var r_bp = get_prefix_binding_power(t.type);

                if(r_bp == -1)
                    parse_error("Expected identifier, literal or prefix operator", peek_token(-1));

                var right_hand_side = expression(r_bp);
                op.children.Add(right_hand_side);
                left_hand_side = op;
                break;
        }
        while(true) {
            var op_type = peek_token().type;

            if(break_at_comma && op_type == TOKEN_TYPE.COMMA) break;

            var l_bp = get_postfix_binding_power(op_type);
            if(l_bp != -1) {
                if(l_bp < min_bp) break;
                consume_token();

                if(op_type == TOKEN_TYPE.OPEN_BRACKET) {
                    var right_hand_side = expression(0);
                    expect(TOKEN_TYPE.CLOSE_BRACKET);
                    var postfix_op = new PostfixOperator(op_type);
                    postfix_op.children.Add(left_hand_side);
                    postfix_op.children.Add(right_hand_side);
                    left_hand_side = postfix_op;
                }
                else {
                    var postfix_op = new PostfixOperator(op_type);
                    postfix_op.children.Add(left_hand_side);
                    left_hand_side = postfix_op;
                }
                continue;
            }

            (l_bp, var r_bp) = get_infix_binding_power(op_type);
            if ((l_bp, r_bp) != (-1, -1)) {
                if(l_bp < min_bp) break;

                consume_token();

                if(op_type == TOKEN_TYPE.QUESTION_MARK) {
                    var middle_hand_side = expression(0);
                    expect(TOKEN_TYPE.COLON);
                    var right_hand_side = expression(r_bp);

                    var op = new InfixOperator(op_type);
                    op.children.Add(left_hand_side);
                    op.children.Add(middle_hand_side);
                    op.children.Add(right_hand_side);
                    left_hand_side = op;
                }
                else {
                    var right_hand_side = expression(r_bp);

                    var op = new InfixOperator(op_type);
                    op.children.Add(left_hand_side);
                    op.children.Add(right_hand_side);
                    left_hand_side = op;
                }
                continue;
            }
            break;
        }
        return left_hand_side;
    }

    static int get_postfix_binding_power(TOKEN_TYPE t) {
        switch(t) {
            case TOKEN_TYPE.PLUS_PLUS:
            case TOKEN_TYPE.MINUS_MINUS:
            case TOKEN_TYPE.OPEN_BRACKET: return 28;
            default: return -1;
        }
    }

    // return -1 if its not a valid prefix operator
    static int get_prefix_binding_power(TOKEN_TYPE t) {
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
            case TOKEN_TYPE.KEYWORD_SIZEOF: return 27;
            default:
                return -1;
        }
    }

    // return (-1, -1) if it's not a valid infix operator
    static (int, int) get_infix_binding_power(TOKEN_TYPE t) {
        switch(t) {
            case TOKEN_TYPE.COMMA: return (1, 2);

            // associativity right to left
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
            case TOKEN_TYPE.XOR_EQUALS: return (4, 3);        // ^=

            case TOKEN_TYPE.QUESTION_MARK: return (6, 5);

            // associativity left to right
            case TOKEN_TYPE.OR_OR:   return (7, 8);           // ||
            case TOKEN_TYPE.AND_AND: return (9, 10);          // &&
            case TOKEN_TYPE.OR:      return (11, 12);         // |
            case TOKEN_TYPE.XOR:     return (13, 14);         // ^
            case TOKEN_TYPE.AND:     return (15, 16);         // &

            case TOKEN_TYPE.EXCLAM_EQUALS:                    // !=
            case TOKEN_TYPE.EQUALS_EQUALS: return (17, 18);   // ==

            case TOKEN_TYPE.SMALLER:                          // <
            case TOKEN_TYPE.SMALLER_EQUALS:                   // <=
            case TOKEN_TYPE.GREATER:                          // >
            case TOKEN_TYPE.GREATER_EQUALS: return (19, 20);  // >=

            case TOKEN_TYPE.GREATER_GREATER:                  // >>
            case TOKEN_TYPE.SMALLER_SMALLER: return (21, 22); // <<

            case TOKEN_TYPE.PLUS:                             // +
            case TOKEN_TYPE.MINUS: return (23, 24);           // -

            case TOKEN_TYPE.STAR:                             // *
            case TOKEN_TYPE.SLASH:                            // /
            case TOKEN_TYPE.PROCENT: return (25, 26);         // %

            case TOKEN_TYPE.DOT: return (28, 29);             // .
            case TOKEN_TYPE.ARROW: return (28, 29);           // ->
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

    public static string ast_to_s_expr(AstNode node)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.Append(node_to_str(node));

        foreach (var child in node.children)
        {
            sb.Append(' ');
            if(child.children.Count > 0) sb.Append(ast_to_s_expr(child));
            else
            {
                sb.Append(node_to_str(child));
            }
        }
        sb.Append(')');
        return sb.ToString();

    }

    public static string node_to_str(AstNode n) => n switch {
        PrefixOperator op => Lexer.token_type_to_lexeme(op.type),
        PostfixOperator op => Lexer.token_type_to_lexeme(op.type),
        InfixOperator op => Lexer.token_type_to_lexeme(op.type),
        Var v => v.name,
        ProcedureCall p => p.name,
        ProcedureDecl p => p.name,
        ProcedureDef p => p.name,
        StringLiteral s => s.value,
        IntLiteral i => i.value.ToString(),
        FloatLiteral f => f.value.ToString(CultureInfo.InvariantCulture),
        CharLiteral c => c.value.ToString(),
        _ => n.GetType().ToString()
    };
}
