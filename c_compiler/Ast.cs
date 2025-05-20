namespace c_compiler;

// TODO: we should find a better way to do this than this fake tagged union nonsense. If only I was using a good programming language...
// potentially records or classes all inheriting from the root AstNode and then use the runtime typechecking to find the types, as sad as that makes me
public record AstNode(AstNode[]? children = null)
{
    public AstNode[] children = children ?? [];
}

public record TranslationUnit(AstNode[]? children = null) : AstNode(children);

public record ProcedureDef(string name, DataType return_type, DataType[] parameters, string?[] param_names, bool variable_length, AstNode[] body) : AstNode(body);
public record ProcedureDecl(string name, DataType return_type, DataType[] parameters, bool variable_length) : AstNode();

public record ProcedureCall(string name, AstNode[]? args = null) : AstNode(args);

public record VarDecl(string name, DataType type, AstNode? init_expression = null) : AstNode(init_expression is null ? [] : [init_expression]);

// TODO: Do we assign type during parsing? Or check it after constructing the tree
public record Var(string name) : AstNode();

public record EmptyStatement() : AstNode();
public record CompoundStatement(AstNode[] statements) : AstNode(statements);
public record ForStatement(AstNode before, AstNode condition, AstNode each_iter, AstNode body) : AstNode([before, condition, each_iter, body]);
public record WhileStatement(AstNode condition, AstNode body) : AstNode([condition, body]);
public record DoStatement(AstNode condition, AstNode body) : AstNode([condition, body]);

public record IfStatement(AstNode condition, AstNode if_case, AstNode? else_case = null) :
    AstNode(else_case is null ? [condition, if_case] : [condition, if_case, else_case]);

public record ReturnStatement(AstNode? return_expression = null) : AstNode(return_expression is null ? [] : [return_expression]);
public record GotoStatement(string label_name) : AstNode();

public record InfixOperator(TOKEN_TYPE type, AstNode left, AstNode right) : AstNode([left, right]);
public record PrefixOperator(TOKEN_TYPE type, AstNode operand) : AstNode([operand]);
public record PostfixOperator(TOKEN_TYPE type, AstNode operand) : AstNode([operand]);
public record TernaryOperator(TOKEN_TYPE type, AstNode left, AstNode middle, AstNode right) : AstNode([left, middle, right]);

public record IntLiteral(long value) : AstNode();

// TODO: change this to double. MAKE SURE YOU CHANGE IN THE LEXER TOO SO IT'S PARSED CORRECTLY
public record FloatLiteral(float value) : AstNode();
public record CharLiteral(char value) : AstNode();
public record StringLiteral(string value) : AstNode();

public record Label(string name) : AstNode();

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
