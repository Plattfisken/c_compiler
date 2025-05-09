
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

    static AstNode_TranslationUnit parse(string source_code) {
        var parser = new Parser(source_code);
        return parser.parse();
    }

    static string code_gen(AstNode root_node) {
        var sb = new StringBuilder();
        sb.AppendLine(".global _main");
        sb.AppendLine(".align 4");

        List<string> str_lits_to_add = new();
        foreach(AstNode_ProcedureDef proc_def in root_node.children) {
            sb.AppendLine($"_{proc_def.name}:");
            sb.AppendLine("\tstp\tx29, x30, [sp, #-16]!");
            sb.AppendLine("\tmov\tx29, sp");
            foreach(AstNode_ProcedureCall proc_call in proc_def.children) {
                int next_arg_register = 0;
                foreach(AstNode_Arg arg in proc_call.children) {
                    switch(arg.token.type) {
                        case TOKEN_TYPE.STRING_LITERAL:
                            str_lits_to_add.Add((string)arg.token.value);
                            sb.AppendLine($"\tadr\tx{next_arg_register++}, L.str{str_lits_to_add.Count-1}");
                            break;
                        case TOKEN_TYPE.INT_LITERAL:
                            sb.AppendLine($"\tmov\tx{next_arg_register++}, #{(long)arg.token.value}");
                            break;
                        default:
                            break;
                    }
                }
                sb.AppendLine($"\tbl\t_{proc_call.name}");
            }
            sb.AppendLine("\tldp\tx29, x30, [sp], #16");
            sb.AppendLine("\tret");
            // if(proc_def.name == "main") {
            //     sb.AppendLine("mov      x0, #0");
            //     sb.AppendLine("mov      x16, #1");
            //     sb.AppendLine("svc      #0x80");
            // }
            // else {
            //     sb.AppendLine("ret");
            //     sb.AppendLine(".cfi_endproc")
            // }
        }
        for(int i = 0; i < str_lits_to_add.Count; ++i) {
            var str_lit = str_lits_to_add[i];
            sb.AppendLine($"L.str{i}:\t.asciz {str_lit}");
        }

        return sb.ToString();
    }

    public static void compile(string source_file_path) {
        var source_code = read_entire_file_as_string(source_file_path);
        var ast = parse(source_code);
        var assembly = code_gen(ast);

        var assembly_file_name = Path.GetFileNameWithoutExtension(source_file_path) + ".s";
        write_to_file(assembly, assembly_file_name);
        // Console.WriteLine(assembly);
        // TODO: We don't need this step anymore, the parser uses the lexer internally to get the tokens as they're being parsed.
        // Keeping it only for debugging purposes during dev
        // var tokens = tokenize(source_code);
        //
        // Console.WriteLine(source_code);
        // foreach(var token in tokens) {
        //     switch(token.type) {
        //         case TOKEN_TYPE.IDENTIFIER:
        //             Console.WriteLine($"{token.type} : {(string)token.value}");
        //             break;
        //         case TOKEN_TYPE.STRING_LITERAL:
        //             Console.WriteLine($"{token.type} : {(string)token.value}");
        //             break;
        //         case TOKEN_TYPE.CHAR_LITERAL:
        //             Console.WriteLine($"{token.type} : {(char)(Int64)token.value}");
        //             break;
        //         case TOKEN_TYPE.INT_LITERAL:
        //             Console.WriteLine($"{token.type} : {(long)token.value}");
        //             break;
        //         case TOKEN_TYPE.FLOAT_LITERAL:
        //             Console.WriteLine($"{token.type} : {(float)token.value}");
        //             break;
        //         default:
        //             Console.WriteLine(token.type);
        //             break;
        //     }
        // }
        // var abstract_syntax_tree = parse(tokens);
        // var assembly             = code_gen(abstract_syntax_tree);
    }
}
