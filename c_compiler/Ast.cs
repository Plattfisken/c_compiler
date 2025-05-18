namespace c_compiler;

// TODO: we should find a better way to do this than this fake tagged union nonsense. If only I was using a good programming language...
// potentially records or classes all inheriting from the root AstNode and then use the runtime typechecking to find the types, as sad as that makes me
public record AstNode(List<AstNode>? children = null)
{
    public List<AstNode> children = children ?? [];
}

public record TranslationUnit(List<AstNode>? children = null) : AstNode(children);

public record ProcedureDef(string name, DataType return_type, DataType[] parameters, bool variable_length,  List<AstNode>? children = null) : AstNode(children);
public record ProcedureDecl(string name, DataType return_type, DataType[] parameters, bool variable_length, List<AstNode>? children = null) : AstNode(children);

public record ProcedureCall(string name, List<AstNode>? children = null) : AstNode(children);

public record VarDecl(string name, DataType type, List<AstNode>? children = null) : AstNode(children);

// TODO: Do we assign type during parsing? Or check it after constructing the tree
public record Var(string name, List<AstNode>? children = null) : AstNode(children);

public record EmptyStatement(List<AstNode>? children = null) : AstNode(children);
public record CompoundStatement(List<AstNode>? children = null) : AstNode(children);
public record ForStatement(AstNode before, AstNode condition, AstNode each_iter, List<AstNode>? children = null) : AstNode(children);
public record WhileStatement(AstNode condition, List<AstNode>? children = null) : AstNode(children);
public record DoStatement(AstNode condition, List<AstNode>? children = null) : AstNode(children);
public record IfStatement(AstNode condition, List<AstNode>? children = null) : AstNode(children);

public record InfixOperator(TOKEN_TYPE type, List<AstNode>? children = null) : AstNode(children);
public record PrefixOperator(TOKEN_TYPE type, List<AstNode>? children = null) : AstNode(children);
public record PostfixOperator(TOKEN_TYPE type, List<AstNode>? children = null) : AstNode(children);

public record IntLiteral(long value, List<AstNode>? children = null) : AstNode(children);

// TODO: change this to double. MAKE SURE YOU CHANGE IN THE LEXER TOO SO IT'S PARSED CORRECTLY
public record FloatLiteral(float value, List<AstNode>? children = null) : AstNode(children);
public record CharLiteral(char value, List<AstNode>? children = null) : AstNode(children);
public record StringLiteral(string value, List<AstNode>? children = null) : AstNode(children);

public record Label(string name, List<AstNode>? children = null) : AstNode(children);
public record Goto(string label_name, List<AstNode>? children = null) : AstNode(children);


//public class AstNode {
//    public AST_TYPE type;
//    public object? value;
//    public List<AstNode> children = new();
//
//    public AstNode(AST_TYPE t, object? v = null) => (type, value) = (t, v);
//}
//
//public struct ProcedureDef {
//    public string name;
//    public DataType return_type;
//    public List<DataType> parameters;
//    // public AstNode body;
//}
//
//public struct ProcedureDecl {
//    public string name;
//    public DataType return_type;
//    public List<DataType> parameters;
//}
//
//public struct ProcedureCall {
//    public string name;
//    // public List<AstNode> args;
//}
//
//public struct VarDecl {
//    public DataType type;
//    public string name;
//}
//
//public struct Var {
//    // TODO: Do we assign type during parsing? Or check it after constructing the tree
//    // public DataType type;
//    public string name;
//}
//
//
//// these could be the first three nodes of the children instead of their own struct. Idk what's better...
//public struct ForStmnt {
//    public AstNode before;
//    public AstNode condition;
//    public AstNode each_iter;
//}
//
//public struct WhileStmnt {
//    public AstNode condition;
//}
//
//public struct DoStmnt {
//    public AstNode condition;
//}
//
//public struct IfStmnt {
//    public AstNode condition;
//}
//
//public enum AST_TYPE {
//    TRANSLATION_UNIT,
//    PROCEDURE_DEF,
//    PROCEDURE_DECL,
//    COMPOUND_STATEMENT,
//    PROCEDURE_CALL,
//    VAR_DECL,
//    VAR,
//    INFIX_OPERATOR,
//    PREFIX_OPERATOR,
//    POSTFIX_OPERATOR,
//    INT_LITERAL,
//    FLOAT_LITERAL,
//    CHAR_LITERAL,
//    STRING_LITERAL,
//    LABEL,
//    GOTO,
//    FOR_STATEMENT,
//    WHILE_STATEMENT,
//    DO_STATEMENT,
//    IF_STATEMENT,
//    EMPTY_STATEMENT,
//}

public enum DATA_TYPE {
    VOID,
    CHAR,
    SHORT,
    INT,
    LONG,
    UNSIGNED_CHAR,
    UNSIGNED_SHORT,
    UNSIGNED_INT,
    UNSIGNED_LONG,
    FLOAT,
    DOUBLE
}

// Why Microsoft? Why do you support equality operators for record structs but not regular structs?
public record struct DataType(DATA_TYPE type, int indirection_count = 0);
