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

    static void type_check(AstNode root_node) {
        TypeChecker.type_check(root_node);
    }

    public static void print_ast(AstNode node) {

        Console.WriteLine(generate_tree_representation(node));
    }

    public static string generate_tree_representation(AstNode node, int indentation_count = 0) {
        var sb = new StringBuilder();
        const string indent = "    ";
        for(int i = 0; i < indentation_count; ++i) {
            sb.Append(indent);
        }
        sb.Append("\"" + Parser.node_to_str(node) + "\"");
        if(node.children.Count > 0)
            sb.Append(": [\n");
        else sb.Append("\n");
        foreach(var child in node.children) {
            sb.Append(generate_tree_representation(child, indentation_count + 1));
        }

        if(node.children.Count > 0) {
            for(int i = 0; i < indentation_count; ++i) {
                sb.Append(indent);
            }
            sb.Append("]\n");
        }
        return sb.ToString();
    }

    public static string compile(string source_code, bool print_ast_only) {
        var ast = parse(source_code);
        if(print_ast_only) {
            print_ast(ast);
            System.Environment.Exit(0);
        }
        type_check(ast);
        return code_gen(ast);
    }

}
