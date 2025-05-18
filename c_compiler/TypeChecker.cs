namespace c_compiler;

public static class TypeChecker {
    public static void type_check(AstNode node) {
        Compiler.assert(node is TranslationUnit, "Can only type check with translation unit as root");
        // TODO: implement
        return;
        var global_vars = new Dictionary<string, DataType>();
        foreach(var child in node.children) {
            switch(child) {
                case VarDecl v: {
                    if(!global_vars.TryAdd(v.name, v.type))
                        Compiler.err_and_die($"Redefinition of symbol: {v.name}");
                    if(v.children.Any()) {
                        // TODO: for now only literals are supported. Expressions that can be evaluated at compile time should also be valid
                        var init_type = type_of_literal(v.children.First());
                        if(init_type is null)
                            Compiler.err_and_die($"Can only initialize symbol: {v.name} with a literal value");
                        else if(init_type.Value != v.type) {
                            
                        }
                    }
                } break;
            }
        }
    }

    static DataType[] implicit_casts(DataType t) {
        if(t.indirection_count > 0)
            return [];
        return t.type switch {
            DATA_TYPE.LONG => [new DataType(DATA_TYPE.UNSIGNED_LONG), new DataType(DATA_TYPE.INT), new DataType(DATA_TYPE.UNSIGNED_INT)],
        };
    }

    static DataType? type_of_literal(AstNode lit) => lit switch {
        IntLiteral => new DataType(DATA_TYPE.LONG),
        FloatLiteral => new DataType(DATA_TYPE.FLOAT),
        StringLiteral => new DataType(DATA_TYPE.CHAR, 1),
        CharLiteral => new DataType(DATA_TYPE.CHAR),
        _ => null
    };
}

