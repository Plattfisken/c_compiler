using System.Diagnostics;

namespace c_compiler;

internal static class Program
{
    static void Main(string[] args) {
        bool generate_assembly_only = false;
        bool save_temp_files = false;
        bool print_ast_only = false;
        string exe_name = "a.out";
        List<string> source_file_names = new();
        for(int i = 0; i < args.Length; ++i) {
            if(args[i][0] == '-') {
                if(args[i] == "-S") generate_assembly_only = true;
                else if(args[i] == "--save-temps") save_temp_files = true;
                else if(args[i] == "-o") {
                    if(i >= args.Length - 1) {
                        Compiler.err_and_die("No name specified after -o flag");
                    }
                    exe_name = args[++i];
                }
                else if(args[i] == "-A") print_ast_only = true;
                else {
                    Compiler.err_and_die($"Unknown option: {args[i]}");
                }

            }
            else {
                source_file_names.Add(args[i]);
            }
        }
        if(source_file_names.Count < 1) Compiler.err_and_die("No source file specified");

        var source_file_names_without_ext = source_file_names.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
        for(int i = 0; i < source_file_names.Count; ++i) {
            string code = Compiler.read_entire_file_as_string(source_file_names[i]!);
            string assembly = Compiler.compile(code, print_ast_only);
            Compiler.write_to_file(assembly, source_file_names_without_ext[i] + ".s");
        }

        if(generate_assembly_only) return;

        // Invoke assembler
        Process p_as = new();
        p_as.StartInfo.FileName = "as";
        p_as.StartInfo.ArgumentList.Add("-arch");
        p_as.StartInfo.ArgumentList.Add("arm64");
        foreach(var file_without_ext in source_file_names_without_ext)
            p_as.StartInfo.ArgumentList.Add(file_without_ext + ".s");

        p_as.Start();
        p_as.WaitForExit();

        // Invoke linker
        Process p_ld = new();
        p_ld.StartInfo.FileName = "ld";
        p_ld.StartInfo.ArgumentList.Add("-arch");
        p_ld.StartInfo.ArgumentList.Add("arm64");
        p_ld.StartInfo.ArgumentList.Add($"-o");
        p_ld.StartInfo.ArgumentList.Add(exe_name);
        p_ld.StartInfo.ArgumentList.Add("-lSystem");
        p_ld.StartInfo.ArgumentList.Add("-syslibroot");
        p_ld.StartInfo.ArgumentList.Add("/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX15.2.sdk");
        foreach(var file_without_ext in source_file_names_without_ext)
            p_ld.StartInfo.ArgumentList.Add(file_without_ext + ".o");

        p_ld.Start();
        p_ld.WaitForExit();

        if(save_temp_files) return;

        foreach(var file_without_ext in source_file_names_without_ext) {
            File.Delete(file_without_ext + ".s");
            File.Delete(file_without_ext + ".o");
        }
    }
}
