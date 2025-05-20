using c_compiler;

namespace c_compiler_tests;

public class CodeGenTests {
    [Theory]
    [InlineData("1 + 2 + 3", "\tmov\tw8, #1\n\tmov\tw9, #2\n\tmov\tw10, #3\n\tadd\tw8, w8, w9\n\tadd\tw8, w8, w10\n")]
    [InlineData("x + 2 * 3", "\tldr\tw8, [sp, #8]\n\tmov\tw9, #2\n\tmov\tw10, #3\n\tmul\tw9, w9, w10\n\tadd\tw8, w8, w9\n")]
    public void expression_tree_should_return_correct_assembly(string expression, string expected_assembly) {
        var parser = new Parser(expression);
        var ast = parser.expression(0);
        var code_generator = new CodeGen();
        var assembly = code_generator.gen_expression_instructions(ast, "w8", new Dictionary<string, int>{ {"x", 8} });
        Assert.Equal(expected_assembly, assembly);
    }

    //[Theory]
    //[InlineData("int main(void) { int a = 5 + 4; }", 1)]
    //[InlineData("int main(void) { int a = 5 + 4; a = func(); int b; }", 2)]
    //[InlineData("int main(void) { int a = 5 + 4; a = func(); int b; if(1) { int c; char a; float c; } else do for(int i = 0; i < 10; ++i); while(a-- > 0); int v; }", 7)]
    //public void count_var_decl_in_tree_should_work(string code, int expected_count) {
    //    var parser = new Parser(code);
    //    var ast = parser.parse();
    //    var code_generator = new CodeGen();
    //    int var_decl_count = code_generator.count_var_decl_in_tree((TranslationUnit)ast);
    //    Assert.Equal(expected_count, var_decl_count);
    //}
}
