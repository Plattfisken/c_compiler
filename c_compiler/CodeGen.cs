using System.Text;

namespace compiler_csharp;
public class CodeGen {
    List<string> str_lits_to_add = new();
    StringBuilder sb = new();

    public string code_gen(AstNode root_node) {
        top_level_statements(root_node);
        for(int i = 0; i < str_lits_to_add.Count; ++i) {
            var str_lit = str_lits_to_add[i];
            sb.AppendLine($"L.str{i}:\t.asciz {str_lit}");
        }

        return sb.ToString();
    }

    void top_level_statements(AstNode node) {
        Compiler.assert(node.type == AST_TYPE.TRANSLATION_UNIT, "node was not of type TRANSLATION_UNIT");
        foreach(var child in node.children) {
            if(child.type == AST_TYPE.PROCEDURE_DEF) procedure_def(child);
        }
    }

    void procedure_def(AstNode proc_def_node) {
        ProcedureDef proc_def = (ProcedureDef)proc_def_node.value;

        sb.AppendLine($".global _{proc_def.name}");
        sb.AppendLine(".align 4");

        List<VarDecl> declared_variables = new();
        foreach(AstNode child in proc_def_node.children) {
            if(child.type == AST_TYPE.VAR_DECL) {
                declared_variables.Add((VarDecl)child.value);
            }
        }
        int required_stack_space = 0;
        foreach(var v in declared_variables) {
            required_stack_space += get_size_of_type(v.type);
        }
        int stack_space_to_allocate = 32;
        while(stack_space_to_allocate < required_stack_space)
            stack_space_to_allocate += 16;

        sb.AppendLine($"_{proc_def.name}:");
        // function prologue
        sb.AppendLine($"\tsub\tsp, sp, #{stack_space_to_allocate}");
        int stack_offset = stack_space_to_allocate - 16;
        sb.AppendLine($"\tstp\tx29, x30, [sp, #{stack_offset}]");
        sb.AppendLine($"\tadd\tx29, sp, #{stack_offset}");

        Dictionary<string, int> var_offsets = new();
        foreach(AstNode child in proc_def_node.children) {
            if(child.type == AST_TYPE.VAR_DECL) {
                var var_decl = (VarDecl)child.value;
                stack_offset -= get_size_of_type(var_decl.type);
                var_offsets[var_decl.name] = stack_offset;
                // if(var_decl.init_value != null) {
                //     switch(var_decl.type.type) {
                //         case DATA_TYPE.INT:
                //             int val = (int)(long)var_decl.init_value;
                //             sb.AppendLine($"\tmov\tw8, #{val}");
                //             break;
                //     }
                //     sb.AppendLine($"\tstr\tw8, [sp, #{stack_offset}]");
                // }
            }
            // if(child.type == AST_TYPE.PROCEDURE_CALL) {
            //     var proc_call = (ProcedureCall)child.value;
            //     int next_arg_register = 0;
            //     foreach(AstNode expr in proc_call.args) {
            //         switch(expr.type) {
            //             case AST_TYPE.STRING_LITERAL:
            //                 str_lits_to_add.Add((string)expr.value);
            //                 sb.AppendLine($"\tadr\tx{next_arg_register++}, L.str{str_lits_to_add.Count-1}");
            //                 break;
            //             case AST_TYPE.INT_LITERAL:
            //                 sb.AppendLine($"\tmov\tx{next_arg_register++}, #{(long)expr.value}");
            //                 break;
            //             case AST_TYPE.VAR:
            //                 // TODO: check the type to know which register type to use. Or how much memory to load
            //                 sb.AppendLine($"\tldr\tw{next_arg_register++}, [sp, #{var_offsets[((Var)expr.value).name]}]");
            //                 break;
            //             default:
            //                 break;
            //         }
            //     }
            //     sb.AppendLine($"\tbl\t_{proc_call.name}");
            // }
        }
        // function epilogue
        sb.AppendLine($"\tldp\tx29, x30, [sp, #{stack_space_to_allocate - 16}]");
        sb.AppendLine($"\tadd\tsp, sp, #{stack_space_to_allocate}");
        sb.AppendLine("\tret");
    }

    static int get_size_of_type(DataType t) {
        if(t.indirection_count > 0) return 8;
        switch(t.type) {
            case DATA_TYPE.VOID:
                return 0;
            case DATA_TYPE.CHAR:
                return 1;
            case DATA_TYPE.UNSIGNED_CHAR:
                return 1;
            case DATA_TYPE.SHORT:
                return 2;
            case DATA_TYPE.UNSIGNED_SHORT:
                return 2;
            case DATA_TYPE.INT:
                return 4;
            case DATA_TYPE.UNSIGNED_INT:
                return 4;
            case DATA_TYPE.LONG:
                return 8;
            case DATA_TYPE.UNSIGNED_LONG:
                return 8;
            case DATA_TYPE.FLOAT:
                return 4;
            case DATA_TYPE.DOUBLE:
                return 8;
            default:
                return 0;
        }
    }
}
