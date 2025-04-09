
namespace compiler_csharp;

internal static class Program
{
    static void Main(string[] args) {
        var source_file_name = args[0];
        Compiler.compile(source_file_name);
    }
}
