
using System.Text;

namespace compiler_csharp;

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

    static string read_entire_file_as_string(string file_path) {
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

    static void write_to_file(string content, string file_name) {
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
        return CodeGen.code_gen(root_node);
    }

    public static void compile(string source_file_path) {
        var source_code = read_entire_file_as_string(source_file_path);
        var ast = parse(source_code);
        var assembly = code_gen(ast);

        var assembly_file_name = Path.GetFileNameWithoutExtension(source_file_path) + ".s";
        write_to_file(assembly, assembly_file_name);
    }
}
