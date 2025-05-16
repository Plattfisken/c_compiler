using System.Diagnostics;
using System.Text;
using c_compiler;

namespace Tests;

public class ParserTests
{
    [Theory]
    [InlineData("1", "(1)")] 
    [InlineData("(((0)))", "(0)")] 
    [InlineData("1 + 2", "(+ 1 2)")] 
    [InlineData("a + b * c * d + e", "(+ (+ a (* (* b c) d)) e)")] 
    [InlineData("34 - 56", "(- 34 56)")] 
    [InlineData("879 * 30", "(* 879 30)")] 
    [InlineData("5 / 2", "(/ 5 2)")] 
    [InlineData("2 * 3 + 4", "(+ (* 2 3) 4)")] 
    [InlineData("2 + 3 * 4", "(+ 2 (* 3 4))")] 
    [InlineData("2 + 3 + 4;", "(+ (+ 2 3) 4)")] 
    public void parser_should_return_correct_tree(string expr, string expected_s_expr)
    {
        var parser = new Parser(expr);
        var ast = parser.expression(0);
        var s_expr = ast_to_s_expr(ast);
        Assert.Equal(s_expr, expected_s_expr);
    }

    string ast_to_s_expr(AstNode node)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        if(node.value.GetType() == typeof(TOKEN_TYPE)) 
            sb.Append(t_type_to_str((TOKEN_TYPE)node.value));
        else if (node.value.GetType() == typeof(Var))
            sb.Append(((Var)node.value).name);
        else if (node.value.GetType() == typeof(ProcedureCall))
            sb.Append(((ProcedureCall)node.value).name);
        else
            sb.Append(node.value);
        foreach (var child in node.children)
        {
            sb.Append(' ');
            if(child.children.Count > 0) sb.Append(ast_to_s_expr(child));
            else
            {
                if(child.value.GetType() == typeof(TOKEN_TYPE)) 
                    sb.Append(t_type_to_str((TOKEN_TYPE)child.value));
                else if (child.value.GetType() == typeof(Var))
                    sb.Append(((Var)child.value).name);
                else if (child.value.GetType() == typeof(ProcedureCall))
                    sb.Append(((ProcedureCall)child.value).name);
                else
                    sb.Append(child.value);
            }
        }
        sb.Append(')');
        return sb.ToString();

        string t_type_to_str(TOKEN_TYPE type)
        {
            return type switch
            {
                TOKEN_TYPE.PLUS => "+",
                TOKEN_TYPE.MINUS => "-",
                TOKEN_TYPE.STAR => "*",
                TOKEN_TYPE.SLASH => "/",
                TOKEN_TYPE.PROCENT => "%",
                _ => throw new Exception("unexpected type")
            };
        }
    }
}