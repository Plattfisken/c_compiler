using System.Diagnostics;
using System.Text;

namespace Tests;
using compiler_csharp;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        string expr = "1 * 2 + 3 - 4 / 5 % --id2 * (7 + 8 / 4) - func(-13 * (5 + id++));";
        var parser = new Parser(expr);
        var ast = parser.expression(0, TOKEN_TYPE.SEMICOLON);
        var s_expr = ast_to_S_expr(ast);
        
    }

    string ast_to_S_expr(AstNode node)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.Append(node.value);
        foreach (var child in node.children)
        {
            if(child.children.Count > 0) sb.Append(ast_to_S_expr(child));
            else sb.Append(child.value);
        }
        sb.Append(')');
        return sb.ToString();
    }
}