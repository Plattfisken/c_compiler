namespace compiler_csharp;

public class AstNode {
    public AST_TYPE type;
    public object? value;
    public List<AstNode> children = new();

    public AstNode(AST_TYPE t, object? v = null) => (type, value) = (t, v);
}

public struct ProcedureDef {
    public string name;
    public DataType return_type;
    public List<DataType> parameters;
}

public struct ProcedureCall {
    public string name;
    public List<AstNode> args;
}

public struct VarDecl {
    public DataType type;
    public string name;
}

// // TODO: this should probably just be removed when expressions as args are just more expressions
// public struct Arg {
//     public Token token;
// }

public struct Var {
    // TODO: Do we assign type during parsing? Or check it after constructing the tree
    // public DataType type;
    public string name;
}

public struct BinaryOperator {
    public TOKEN_TYPE type;
}

public struct DataType {
    public DATA_TYPE type;
    public int indirection_count;
}

public enum AST_TYPE {
    TRANSLATION_UNIT,
    PROCEDURE_DEF,
    PROCEDURE_CALL,
    VAR_DECL,
    VAR,
    BINARY_OPERATOR,
    INT_LITERAL,
    FLOAT_LITERAL,
    CHAR_LITERAL,
    STRING_LITERAL
}

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
