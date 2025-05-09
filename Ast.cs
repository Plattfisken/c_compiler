namespace compiler_csharp;

public class AstNode {
    public List<AstNode> children= new();
}

public class AstNode_TranslationUnit : AstNode {
}

public class AstNode_ProcedureDef : AstNode {
    public string name = "";
    public RETURN_TYPE ret_type;
}

public class AstNode_ProcedureCall : AstNode {
    public string name = "";
}

public class AstNode_Arg : AstNode {
    public Token token;
}

public class AstNode_Block : AstNode {
}

public class AstNode_Assgn : AstNode {
    public AstNode? left;
    public AstNode? right;
}

public class AstNode_Operator_Add : AstNode {
    public AstNode? left;
    public AstNode? right;
}

public class AstNode_Operator_Sub : AstNode {
    public AstNode? left;
    public AstNode? right;
}

public class AstNode_Operator_Mul : AstNode {
    public AstNode? left;
    public AstNode? right;
}

public class AstNode_Operator_Div : AstNode {
    public AstNode? left;
    public AstNode? right;
}

public enum RETURN_TYPE {
    VOID,
    INT,
}
