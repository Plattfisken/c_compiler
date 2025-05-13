using System.Diagnostics;

namespace compiler_csharp;

internal static class Program
{
    static void Main(string[] args) {
        bool generate_assembly_only = false;
        bool save_temp_files = false;
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
                else {
                    Compiler.err_and_die($"Unknown option: {args[i]}");
                }

            }
            else {
                source_file_names.Add(args[i]);
            }
        }
        if(source_file_names.Count < 1) Compiler.err_and_die("No source file specified");

        List<string> assembly_file_names = new();
        foreach(var source_file_name in source_file_names) {
            string code = Compiler.read_entire_file_as_string(source_file_name!);
            string assembly = Compiler.compile(code);

            var file_without_ext = Path.GetFileNameWithoutExtension(source_file_name);
            var assembly_file_name = file_without_ext + ".s";
            assembly_file_names.Add(assembly_file_name);
            Compiler.write_to_file(assembly, assembly_file_name);

        }

        if(generate_assembly_only) return;

        List<string> obj_file_names = new();
        foreach(var assembly_file_name in assembly_file_names) {
            var file_without_ext = Path.GetFileNameWithoutExtension(assembly_file_name);
            var obj_file_name = file_without_ext + ".o";
            obj_file_names.Add(obj_file_name);
            Process p_as = new();
            p_as.StartInfo.FileName = "as";
            p_as.StartInfo.Arguments = $"-arch arm64 -o {obj_file_name} {assembly_file_name}";
            p_as.Start();
            p_as.WaitForExit();
        }
        if(generate_assembly_only) return;

        Process p_ld = new();
        p_ld.StartInfo.FileName = "ld";
        p_ld.StartInfo.ArgumentList.Add("-arch");
        p_ld.StartInfo.ArgumentList.Add("arm64");
        p_ld.StartInfo.ArgumentList.Add($"-o");
        p_ld.StartInfo.ArgumentList.Add(exe_name);
        p_ld.StartInfo.ArgumentList.Add("-lSystem");
        p_ld.StartInfo.ArgumentList.Add("-syslibroot");
        p_ld.StartInfo.ArgumentList.Add("/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX15.2.sdk");

        foreach(var obj_file_name in obj_file_names) {
            p_ld.StartInfo.ArgumentList.Add(obj_file_name);
        }

        p_ld.Start();
        p_ld.WaitForExit();

        if(!save_temp_files) {
            foreach(var assembly_file_name in assembly_file_names)
                File.Delete(assembly_file_name);
            foreach(var obj_file_name in obj_file_names)
                File.Delete(obj_file_name);
        }
    }
}
