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
    [InlineData("1 + 2 * 3", "(+ 1 (* 2 3))")]
    [InlineData("f . g . h", "(. (. f g) h)")]
    [InlineData(" 1 + 2 + f . g . h * 3 * 4", "(+ (+ 1 2) (* (* (. (. f g) h) 3) 4))")]
    [InlineData("--1 * 2", "(* (-- 1) 2)")]
    [InlineData("--f . g", "(-- (. f g))")]
    [InlineData("x[0][1]", "([ ([ x 0) 1)")]
    [InlineData("x[a + 2 % 12]", "([ x (+ a (% 2 12)))")]
    [InlineData( "a ? b : c ? d : e", "(? a b (? c d e))")]
    [InlineData( "a = b >= 10 ? a : c ? d : e", "(= a (? (>= b 10) a (? c d e)))")]
    [InlineData( "a = (d + 5, a <= d) ? func(a & b++, *c), 10 : a | b", "(= a (? (, (+ d 5) (<= a d)) (, (func (& a (++ b)) (* c)) 10) (| a b)))")]
    [InlineData("a = 0 ? b : c = d", "(= a (= (? 0 b c) d))")]
    public void parser_should_return_correct_tree(string expr, string expected_s_expr)
    {
        var parser = new Parser(expr);
        var ast = parser.expression(0);
        var s_expr = ast_to_s_expr(ast);
        Assert.Equal(expected_s_expr, s_expr);
    }

    string ast_to_s_expr(AstNode node)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        if(node.value.GetType() == typeof(TOKEN_TYPE)) 
            sb.Append(Lexer.token_type_to_lexeme((TOKEN_TYPE)node.value));
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
                    sb.Append(Lexer.token_type_to_lexeme((TOKEN_TYPE)child.value));
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
    }
}
