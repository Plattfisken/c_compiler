using System.Text;

namespace compiler_csharp;

public static class Compiler {
    static void err_and_die(string err_message) {
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
            if(tokens.Last().type == TOKEN_TYPE.PARSE_ERROR) err_and_die(tokens.Last().value);
            if(tokens.Last().type == TOKEN_TYPE.EOF) break;
        }
        return tokens.ToArray();
    }

    struct SyntaxTreeNode {
    }
    static SyntaxTreeNode parse(Token[] tokens) {
        SyntaxTreeNode s;
        return s;
    }

    static string code_gen(SyntaxTreeNode root_node) {
        return "";
    }

    public static void compile(string source_file_path) {
        var source_code = read_entire_file_as_string(source_file_path);
        var tokens = tokenize(source_code);

        int[] token_count = new int[(int)TOKEN_TYPE.PARSE_ERROR];
        foreach(var token in tokens) {
            token_count[(int)token.type]++;
        }
        for(int i = 0; i < token_count.Length; ++i) {
            Console.WriteLine($"{((TOKEN_TYPE)(i)).ToString()} : {token_count[i]}");
        }

        // Console.WriteLine(source_code);
        // foreach(var token in tokens) {
        //     Console.WriteLine($"{token.type.ToString()} : {token.value}");
        // }

        // var abstract_syntax_tree = parse(tokens);
        // var assembly             = code_gen(abstract_syntax_tree);
        // write_to_file(assembly, "output.as");
    }
}
