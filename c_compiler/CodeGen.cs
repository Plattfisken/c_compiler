using System.Text;

namespace c_compiler;
public class CodeGen {
    Dictionary<string, string> str_lits_dict = new();

    Dictionary<string, ProcedureDef> proc_def_dict = new();

    Dictionary<string, bool> scratch_regs_availability = new(){
        {"8", true},
        {"9", true},
        {"10", true},
        {"11", true},
        {"12", true},
        {"13", true},
        {"14", true},
        {"15", true},
    };

    int label_count = 0;
    int proc_count = 0;

    public string code_gen(TranslationUnit root_node) {
        StringBuilder sb = new();
        sb.Append(top_level_statements(root_node));
        foreach(var str_lit in str_lits_dict) {
            sb.AppendLine($"{str_lit.Value}:\t.asciz {str_lit.Key}");
        }

        return sb.ToString();
    }

    string top_level_statements(TranslationUnit node) {
        StringBuilder sb = new();
        foreach(var child in node.children) {
            switch(child) {
                case ProcedureDecl p: {
                    // Annoying, should find a better way
                    var fake_proc_def_to_put_in_dict = new ProcedureDef(p.name, p.return_type, p.parameters, [], p.variable_length, []);
                    proc_def_dict[p.name] = fake_proc_def_to_put_in_dict;
                } break;
                case ProcedureDef p:
                    sb.Append(procedure_def(p));
                    break;
                case VarDecl v:
                    Compiler.todo();
                    break;
            }
        }
        return sb.ToString();
    }

    public int total_var_args_in_tree(AstNode node) {
        int count = 0;
        if(node is ProcedureCall) {
            if(proc_def_dict[((ProcedureCall)node).name].variable_length) {
                int num_var_args = ((ProcedureCall)node).args?.Length ?? 0 - proc_def_dict[((ProcedureCall)node).name].parameters.Length;
                if(num_var_args > 0) return num_var_args;
            }
        }

        foreach(var child in node.children) {
            count += total_var_args_in_tree(child);
        }
        return count;
    }

    public int total_bytes_of_vars_in_tree(AstNode node) {
        int count = 0;
        if(node is VarDecl)
            return get_size_of_type(((VarDecl)node).type);

        foreach(var child in node.children) {
            count += total_bytes_of_vars_in_tree(child);
        }
        return count;
    }

    string procedure_def(ProcedureDef node) {
        proc_def_dict[node.name] = node;
        StringBuilder sb = new();
        sb.AppendLine($".global _{node.name}");
        sb.AppendLine(".align 4");

        int required_stack_space =
            total_bytes_of_vars_in_tree(node) +
            node.parameters.Select(p => get_size_of_type(p)).Sum() +
            total_var_args_in_tree(node)*8;

        int stack_space_to_allocate = 32;
        while(stack_space_to_allocate - 16 < required_stack_space)
            stack_space_to_allocate += 16;

        sb.AppendLine($"_{node.name}:");
        // function prologue
        sb.AppendLine($"\tsub\tsp, sp, #{stack_space_to_allocate}");
        int stack_offset = stack_space_to_allocate - 16;
        sb.AppendLine($"\tstp\tx29, x30, [sp, #{stack_offset}]");
        sb.AppendLine($"\tadd\tx29, sp, #{stack_offset}");

        Dictionary<string, int> var_offsets = new();

        var ret_label = get_label();

        // store args on the stack
        for(int i = 0; i < node.parameters.Length; ++i) {
            if(node.parameters[i].type != DATA_TYPE.VOID || node.parameters[i].indirection_count > 0) {
                stack_offset -= get_size_of_type(node.parameters[i]);
                Compiler.assert(node.param_names[i] is not null, "Cannot have nameless parameter in function definition");
                var_offsets[node.param_names[i]!] = stack_offset;
                var reg_type = get_size_of_type(node.parameters[i]) > 4 ? "x" : "w";
                sb.AppendLine($"\tstr\t{reg_type}{i}, [sp, #{var_offsets[node.param_names[i]!]}]");
            }
        }

        void generate_statement_code(AstNode statement) {
            switch(statement) {
                case Label l: {
                    sb.AppendLine(l.name + ":");
                } break;
                case VarDecl v: {
                    stack_offset -= get_size_of_type(v.type);
                    var_offsets[v.name] = stack_offset;
                    if(v.init_expression is not null) {
                        var should_be_32_bit_reg = true;
                        var reg = get_reg(should_be_32_bit_reg);
                        sb.Append(gen_expression_instructions(v.init_expression, reg, var_offsets));
                        sb.AppendLine($"\tstr\t{reg}, [sp, #{var_offsets[v.name]}]");
                        free_reg(reg);
                    }
                } break;
                case CompoundStatement c: {
                    foreach(var child in c.statements)
                        generate_statement_code(child);
                } break;
                case IfStatement i: {
                    var should_be_32_bit_reg = true;
                    var reg = get_reg(should_be_32_bit_reg);
                    var true_label = get_label();
                    var false_label = get_label();
                    var out_label = get_label();
                    sb.Append(gen_expression_instructions(i.condition, reg, var_offsets));
                    sb.AppendLine($"\ttbnz\t{reg}, #0, {true_label}");
                    free_reg(reg);
                    if(i.else_case is not null) {
                        sb.AppendLine($"\tb\t{false_label}");
                        sb.AppendLine($"{false_label}:");
                        generate_statement_code(i.else_case);
                    }
                    sb.AppendLine($"\tb\t{out_label}");
                    sb.AppendLine($"{true_label}:");
                    generate_statement_code(i.if_case);
                    sb.AppendLine($"\tb\t{out_label}");
                    sb.AppendLine($"{out_label}:");
                } break;
                case WhileStatement w: {
                    var condition_label = get_label();
                    var true_label = get_label();
                    var out_label = get_label();

                    var should_be_32_bit_reg = true;
                    var condition_reg = get_reg(should_be_32_bit_reg);
                    sb.AppendLine($"{condition_label}:");
                    sb.Append(gen_expression_instructions(w.condition, condition_reg, var_offsets));
                    sb.AppendLine($"\ttbnz\t{condition_reg}, #0, {true_label}");
                    free_reg(condition_reg);

                    sb.AppendLine($"\tb\t{out_label}");

                    sb.AppendLine($"{true_label}:");
                    generate_statement_code(w.body);
                    sb.AppendLine($"\tb\t{condition_label}");
                    sb.AppendLine($"{out_label}:");
                } break;
                case DoStatement d: {
                    var loop_label = get_label();
                    sb.AppendLine($"{loop_label}:");
                    generate_statement_code(d.body);
                    var should_be_32_bit_reg = true;
                    var reg = get_reg(should_be_32_bit_reg);
                    sb.Append(gen_expression_instructions(d.condition, reg, var_offsets));
                    sb.AppendLine($"\ttbnz\t{reg}, #0, {loop_label}");
                    free_reg(reg);
                } break;
                case ForStatement f: {
                    generate_statement_code(f.before);
                    var condition_label = get_label();
                    var true_label = get_label();
                    var out_label = get_label();
                    var should_be_32_bit_reg = true;
                    var condition_reg = get_reg(should_be_32_bit_reg);
                    sb.AppendLine($"{condition_label}:");
                    sb.Append(gen_expression_instructions(f.condition, condition_reg, var_offsets));
                    sb.AppendLine($"\ttbnz\t{condition_reg}, #0, {true_label}");
                    free_reg(condition_reg);

                    sb.AppendLine($"\tb\t{out_label}");

                    sb.AppendLine($"{true_label}:");
                    generate_statement_code(f.body);

                    should_be_32_bit_reg = true;
                    var reg = get_reg(should_be_32_bit_reg);
                    sb.Append(gen_expression_instructions(f.each_iter, reg, var_offsets));
                    free_reg(reg);

                    sb.AppendLine($"\tb\t{condition_label}");
                    sb.AppendLine($"{out_label}:");
                } break;
                case GotoStatement g: {
                    sb.AppendLine($"\tb\t{g.label_name}");
                } break;
                case ReturnStatement r: {
                    if(r.return_expression is not null) {
                            var return_type_size = get_size_of_type(node.return_type);
                            var should_be_32_bit_reg = return_type_size <= 4;
                            var reg = get_reg(should_be_32_bit_reg);
                            sb.Append(gen_expression_instructions(r.return_expression, reg, var_offsets));
                            sb.AppendLine($"\tmov\tw0, {reg}");
                            free_reg(reg);
                    }
                    sb.AppendLine($"\tb\t{ret_label}");

                } break;
                default: {
                    var should_be_32_bit_reg = true;
                    var reg = get_reg(should_be_32_bit_reg);
                    sb.Append(gen_expression_instructions(statement, reg, var_offsets));
                    free_reg(reg);
                } break;
            }
        }
        foreach(AstNode child in node.children) {
            generate_statement_code(child);
        }
        // function epilogue
        sb.AppendLine($"{ret_label}:");
        sb.AppendLine($"\tldp\tx29, x30, [sp, #{stack_space_to_allocate - 16}]");
        sb.AppendLine($"\tadd\tsp, sp, #{stack_space_to_allocate}");
        sb.AppendLine("\tret");

        proc_count++;
        label_count = 0;

        return sb.ToString();
    }

    static bool is_operator_or_proc_call(AstNode node) {
        if(node is InfixOperator or PrefixOperator or PostfixOperator or TernaryOperator or ProcedureCall)
            return true;
        return false;
    }

    string instructions_for_arithmetic_logical_ops(AstNode node, string reg1, string reg2) {
        Compiler.assert(is_operator_or_proc_call(node), "Node wasn't an operator");
        var sb = new StringBuilder();
        switch(node) {
            case InfixOperator i: {
                Compiler.assert(reg2 is not null, "need to registers for infix operator");
                switch(i.type) {
                    case TOKEN_TYPE.PLUS: {
                        sb.AppendLine($"\tadd\t{reg1}, {reg1}, {reg2}");
                    } break;
                    case TOKEN_TYPE.MINUS: {
                        sb.AppendLine($"\tsub\t{reg1}, {reg1}, {reg2}");
                    } break;
                    case TOKEN_TYPE.STAR: {
                        sb.AppendLine($"\tmul\t{reg1}, {reg1}, {reg2}");
                    } break;
                    case TOKEN_TYPE.SLASH: {
                        sb.AppendLine($"\tsdiv\t{reg1}, {reg1}, {reg2}");
                    } break;
                    case TOKEN_TYPE.SMALLER: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, lt");
                    } break;
                    case TOKEN_TYPE.SMALLER_EQUALS: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, le");
                    } break;
                    case TOKEN_TYPE.GREATER: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, gt");
                    } break;
                    case TOKEN_TYPE.GREATER_EQUALS: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, ge");
                    } break;
                    case TOKEN_TYPE.EXCLAM_EQUALS: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, ne");
                    } break;
                    case TOKEN_TYPE.EQUALS_EQUALS: {
                        sb.AppendLine($"\tsubs\t{reg1}, {reg1}, {reg2}");
                        sb.AppendLine($"\tcset\t{reg1}, eq");
                    } break;
                    default: {
                        Compiler.todo();
                    } break;
                             // case TOKEN_TYPE.EQUALS:
                    // case TOKEN_TYPE.GREATER_GREATER:
                    // case TOKEN_TYPE.COMMA:
                    // case TOKEN_TYPE.PLUS_EQUALS:                      // +=
                    // case TOKEN_TYPE.MINUS_EQUALS:                     // -=
                    // case TOKEN_TYPE.STAR_EQUALS:                      // *=
                    // case TOKEN_TYPE.SLASH_EQUALS:                     // /=
                    // case TOKEN_TYPE.PROCENT_EQUALS:                   // %=
                    // case TOKEN_TYPE.RIGHT_SHIFT_EQUALS:               // >>=
                    // case TOKEN_TYPE.LEFT_SHIFT_EQUALS:                // <<=
                    // case TOKEN_TYPE.AND_EQUALS:                       // &=
                    // case TOKEN_TYPE.OR_EQUALS:                        // |=
                    // case TOKEN_TYPE.XOR_EQUALS:
                    // case TOKEN_TYPE.QUESTION_MARK:
                    // case TOKEN_TYPE.OR_OR:
                    // case TOKEN_TYPE.AND_AND:
                    // case TOKEN_TYPE.OR:
                    // case TOKEN_TYPE.XOR:
                    // case TOKEN_TYPE.AND:
                    // case TOKEN_TYPE.SMALLER_SMALLER:
                    // case TOKEN_TYPE.PROCENT:
                    // case TOKEN_TYPE.DOT:
                    // case TOKEN_TYPE.ARROW:
                }
            } break;
            case PrefixOperator: {
                Compiler.todo();
            } break;
            case PostfixOperator: {
                Compiler.todo();
            } break;
            case TernaryOperator: {
                Compiler.todo();
            } break;
            default:
                break;
        }
        return sb.ToString();
    }

    public string gen_expression_instructions(AstNode node, string dest_reg, Dictionary<string, int> var_offsets) {
        var sb = new StringBuilder();

        switch(node) {
            case InfixOperator i: {
                if(i.type == TOKEN_TYPE.EQUALS) {
                    sb.Append(gen_expression_instructions(i.right, dest_reg, var_offsets));
                    Compiler.assert(i.left is Var, "Can only assign to variable");
                    sb.AppendLine($"\tstr\t{dest_reg}, [sp, #{var_offsets[((Var)i.left).name]}]");
                }
                else {
                    sb.Append(gen_expression_instructions(i.left, dest_reg, var_offsets));
                    var reg = get_reg(true);
                    sb.Append(gen_expression_instructions(i.right, reg, var_offsets));
                    sb.Append(instructions_for_arithmetic_logical_ops(i, dest_reg, reg));
                    free_reg(reg);
                }
            } break;
            case PrefixOperator p: {
                switch(p.type) {
                    case TOKEN_TYPE.PLUS_PLUS: {
                        Compiler.assert(p.operand is Var, "can only increment var");
                        sb.AppendLine($"\tldr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                        sb.AppendLine($"\tadd\t{dest_reg}, {dest_reg}, #1");
                        sb.AppendLine($"\tstr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                    } break;
                    case TOKEN_TYPE.MINUS_MINUS: {
                        Compiler.assert(p.operand is Var, "can only decrement var");
                        sb.AppendLine($"\tldr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                        sb.AppendLine($"\tsub\t{dest_reg}, {dest_reg}, #1");
                        sb.AppendLine($"\tstr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                    } break;
                    case TOKEN_TYPE.MINUS: {
                        sb.Append(gen_expression_instructions(p.operand, dest_reg, var_offsets));
                        var should_be_32_bit_reg = true;
                        var reg = get_reg(should_be_32_bit_reg);
                        sb.AppendLine($"\tmov\t{reg}, #0");
                        sb.AppendLine($"\tsub\t{dest_reg}, {reg}, {dest_reg}");
                        free_reg(reg);
                    } break;
                    case TOKEN_TYPE.PLUS:
                        break;
                    default:
                     Compiler.todo();
                     break;
                }
                // sb.Append(gen_expression_instructions(p.operand, dest_reg, var_offsets));
            } break;
            case PostfixOperator p:
                switch(p.type) {
                    case TOKEN_TYPE.PLUS_PLUS: {
                        Compiler.assert(p.operand is Var, "can only increment var");
                        sb.AppendLine($"\tldr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                        var should_be_32_bit_reg = true;
                        var reg = get_reg(should_be_32_bit_reg);
                        sb.AppendLine($"\tadd\t{reg}, {dest_reg}, #1");
                        sb.AppendLine($"\tstr\t{reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                    } break;
                    case TOKEN_TYPE.MINUS_MINUS: {
                        Compiler.assert(p.operand is Var, "can only decrement var");
                        sb.AppendLine($"\tldr\t{dest_reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                        var should_be_32_bit_reg = true;
                        var reg = get_reg(should_be_32_bit_reg);
                        sb.AppendLine($"\tsub\t{reg}, {dest_reg}, #1");
                        sb.AppendLine($"\tstr\t{reg}, [sp, {var_offsets[((Var)p.operand).name]}]");
                    } break;
                    default:
                     Compiler.todo();
                     break;
                }
                // sb.Append(gen_expression_instructions(p.operand, dest_reg, var_offsets));
                break;
            case TernaryOperator:
                Compiler.todo();
                break;
            case ProcedureCall p: {
                Compiler.assert(p.children.Length <= 8, "Only support 8 args");
                int variadic_arg_stack_offset = 0;
                for(int i = 0; i < p.args?.Length; ++i) {
                    // TODO: x or w register depending on type
                    var proc_def = proc_def_dict[p.name];
                    // For variable length
                    if(i >= proc_def.parameters.Length) {
                        // Let's just assume it's this for now
                        // TODO: Should we try to infer the type of an expression instead?
                        if(!proc_def.variable_length) {
                            Compiler.err_and_die($"Too many args passed to {p.name}");
                        }
                        var should_be_32_bit_reg = false;
                        var reg = get_reg(should_be_32_bit_reg);
                        sb.Append(gen_expression_instructions(p.args[i], reg, var_offsets));
                        sb.AppendLine($"\tstr\t{reg}, [sp, #{variadic_arg_stack_offset}]");
                        variadic_arg_stack_offset += 8;
                        free_reg(reg);
                    }
                    else {
                        string reg_type = get_size_of_type(proc_def.parameters[i]) > 4 ? "x" : "w";
                        sb.Append(gen_expression_instructions(p.args[i], $"{reg_type}{i}", var_offsets));
                    }
                }
                sb.AppendLine($"\tbl\t_{p.name}");
                // TODO: x or w register depending on type
                sb.AppendLine($"\tmov\t{dest_reg}, w0");
            } break;
            case Var v: {
                sb.AppendLine($"\tldr\t{dest_reg}, [sp, #{var_offsets[v.name]}]");
            } break;
            case IntLiteral i: {
                sb.AppendLine($"\tmov\t{dest_reg}, #{i.value}");
            } break;
            case StringLiteral s: {
                // NOTE: assumes the dest_reg is an x register
                str_lits_dict.TryAdd(s.value, $".L.str.{str_lits_dict.Count}");
                sb.AppendLine($"\tadr\t{dest_reg}, {str_lits_dict[s.value]}");
            } break;
            default:
                Compiler.todo();
                break;
        }
        return sb.ToString();
    }

    string get_label() {
        return $".LBB{proc_count}_{label_count++}";
    }

    string get_reg(bool get_w_version) {
        foreach(var reg in scratch_regs_availability) {
            if(reg.Value) {
                scratch_regs_availability[reg.Key] = false;
                return get_w_version ? "w" + reg.Key : "x" + reg.Key;
            }
        }
        // How to handle this case?
        Compiler.assert(false, "No available register");
        return "";
    }

    void free_reg(string reg) {
        reg = reg[1..];
        scratch_regs_availability[reg] = true;
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
