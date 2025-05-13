using System.Diagnostics;

namespace compiler_csharp;

internal static class Program
{
    static void Main(string[] args) {
        bool generate_assembly_only = false;
        bool save_temp_files = false;
        string exe_name = "a.out";
        string? source_file_name = null;
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
                else {
                    Compiler.err_and_die($"Unknown option: {args[i]}");
                }

            }
            else {
                if(source_file_name is null) {
                    source_file_name = args[i];
                }
                else {
                    Compiler.err_and_die("Only one source file supported for now...");
                }
            }
        }
        if(source_file_name is null) Compiler.err_and_die("No source file specified");

        string code = Compiler.read_entire_file_as_string(source_file_name!);
        string assembly = Compiler.compile(code);

        var file_without_ext = Path.GetFileNameWithoutExtension(source_file_name);
        var assembly_file_name = file_without_ext + ".s";
        Compiler.write_to_file(assembly, assembly_file_name);

        if(generate_assembly_only) return;

        var obj_file_name = file_without_ext + ".o";
        Process p_as = new();
        p_as.StartInfo.FileName = "as";
        p_as.StartInfo.Arguments = $"-arch arm64 -o {obj_file_name} {assembly_file_name}";
        p_as.Start();
        p_as.WaitForExit();

        Process p_ld = new();
        p_ld.StartInfo.FileName = "ld";
        p_ld.StartInfo.Arguments = $"-arch arm64 -o {exe_name} -lSystem -syslibroot /Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX15.2.sdk {obj_file_name}";
        p_ld.Start();
        p_ld.WaitForExit();

        if(!save_temp_files) {
            File.Delete(assembly_file_name);
            File.Delete(obj_file_name);
        }
    }
}
