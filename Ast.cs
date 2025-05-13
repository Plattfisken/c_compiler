namespace compiler_csharp;

public class AstNode {
    public AST_TYPE type;
    public object? value;
    public List<AstNode> children = new();

    public AstNode(AST_TYPE t, object? v = null) => (type, value) = (t, v);
}

public struct ProcedureDef {
    public string name;
    public DATA_TYPE return_type;
}

public struct ProcedureCall {
    public string name;
}

public struct VarDecl {
    public DATA_TYPE type;
    public string name;
    public object? init_value;
}

// TODO: this should probably just be removed when expressions as args are just more expressions
public struct Arg {
    public Token token;
}

public struct binary_operator {
    public TOKEN_TYPE type;
}

public enum AST_TYPE {
    TRANSLATION_UNIT,
    PROCEDURE_DEF,
    PROCEDURE_CALL,
    ARG,
    VAR_DECL,
    BINARY_OPERATOR
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
