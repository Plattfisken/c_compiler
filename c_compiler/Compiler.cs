using System.Globalization;
using System.Text;

namespace c_compiler;

public static class Compiler {
    public static void assert(bool exp, string message) {
        if(!exp) {
            Console.WriteLine($"Assertion failed: {message}");
            System.Environment.Exit(1);
        }
    }
    public static void err_and_die(string err_message) {
        // TODO: Go through everywhere this is called and write good error messages, or alternatively, if it's an internal error, handle it properly
        Console.WriteLine($"Error: {err_message}");
        System.Environment.Exit(1);
    }

    static long get_file_size(FileStream file) {
        var file_size = file.Seek(0, SeekOrigin.End);
        file.Seek(0, SeekOrigin.Begin);
        return file_size;
    }

    public static string read_entire_file_as_string(string file_path) {
        try {
            var file = File.Open(file_path, FileMode.Open);
            var file_size = get_file_size(file!);
            var file_buffer = new byte[file_size];
            file!.Read(file_buffer);
            return Encoding.UTF8.GetString(file_buffer);
        }
        catch(Exception e) {
            err_and_die($"Something went wrong: {e.Message}");
            // NOTE: Unreachable
            return "";
        }
    }

    public static void write_to_file(string content, string file_name) {
        File.WriteAllText(file_name, content);
    }

    static Token[] tokenize(string code) {
        var lexer = new Lexer(code);
        var tokens = new List<Token>();
        while(true) {
            tokens.Add(lexer.next_token());
            if(tokens.Last().type == TOKEN_TYPE.PARSE_ERROR) err_and_die((string)tokens.Last().value);
            if(tokens.Last().type == TOKEN_TYPE.EOF) break;
        }
        return tokens.ToArray();
    }

    static AstNode parse(string source_code) {
        var parser = new Parser(source_code);
        return parser.parse();
    }

    static string code_gen(AstNode root_node) {
        var code_generator = new CodeGen();
        return code_generator.code_gen(root_node);
    }

    static void print_ast(AstNode root_node) {
        Queue<AstNode> q = new();
        var node = root_node;
        q.Enqueue(node);
        while(q.Count > 0) {
            var new_node = q.Dequeue();
            node = new_node;
            Console.Write(node_text(node));
            if(node.children.Count > 0) Console.Write(" --> ");
            foreach(var child in node.children) {
                q.Enqueue(child);
                Console.Write(node_text(child));
                Console.Write(", ");
            }
            Console.WriteLine();
        }
        string node_text(AstNode n) {
            switch(n.type) {
                case AST_TYPE.VAR:
                    return ((Var)n.value).name;
                case AST_TYPE.INT_LITERAL:
                    return ((long)n.value).ToString();
                case AST_TYPE.FLOAT_LITERAL:
                    return ((float)n.value).ToString();
                case AST_TYPE.CHAR_LITERAL:
                    return ((char)(long)n.value).ToString();
                case AST_TYPE.STRING_LITERAL:
                    return (string)n.value;
                case AST_TYPE.INFIX_OPERATOR:
                case AST_TYPE.PREFIX_OPERATOR:
                case AST_TYPE.POSTFIX_OPERATOR:
                    return ((TOKEN_TYPE)n.value).ToString();
                case AST_TYPE.PROCEDURE_CALL: {
                    var call = ((ProcedureCall)n.value);
                    return call.name;
                }
                default:
                    return n.type.ToString();
            }
        }
    }

    public static string compile(string source_code, bool print_ast_only) {
        var ast = parse(source_code);
        if(print_ast_only) {
            print_ast(ast);
            System.Environment.Exit(0);
        }
        return code_gen(ast);
    }

}
