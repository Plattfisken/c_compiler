#define META_PROGRAMMING

#define USEFUL_THINGS_IMPLEMENTATION
#define USEFUL_THINGS_STDLIB
#define USEFUL_THINGS_STRIP_PREFIX
#include <useful_things.h>
#include "../lexer.c"
#include <stdio.h>
#include <dirent.h>
#include <string.h>

typedef struct {
    String *data;
    int count;
    int capacity;
    UT_Arena *string_store;
} Strings;
// void list_dir(char *dir_path, Strings *dir_names, UT_Arena *dir_name_store) {
//     DIR *d;
//     struct dirent *ep;
//     d = opendir(dir_path);
//     if(d) {
//         while((ep = readdir(d))) {
//             String dir_name = make_string(ep->d_name, strlen(ep->d_name), dir_name_store);
//             da_append(*dir_names, &dir_name);
//         }
//     }
// }

bool consume_until_specific_token_type(Lexer *lexer, TOKEN_TYPE t) {
    while(next_token(lexer)) {
        if(lexer->token.type == t) return true;
    }
    return false;
}

void print_string(String s) {
    printf("%.*s", (int)s.length, s.data);
}

int main(void) {
    Arena *arena = arena_create();
    size_t directory_entries_length = 0;
    String *directory_entries = list_directory("./", &directory_entries_length, arena);
    // list_dir("./", &directory_entries, arena);
    FILE *generated_file = fopen("generated/generated.c", "w");
    for(int i = 0; i < directory_entries_length; ++i) {
        String file_name = directory_entries[i];
        // if(strncmp(&s.data[s.length-2], ".c", 2) == 0 || strncmp(&s.data[s.length-2], ".h", 2) == 0) {
        if(strncmp(&file_name.data[file_name.length-2], ".h", 2) == 0) {
            char *file_content = UT_read_entire_file_and_null_terminate(file_name.data);
            if(file_content) {
                fprintf(generated_file, "// procedures generated from %s\n", file_name.data);
                fprintf(generated_file, "#include \"../%s\"\n", file_name.data);
                Lexer lexer = { file_content };

                // NOTE: this only works for typedefed enums that don't have a name before the body, e.g typedef enum { member1, member2 } foo;
                // we need a slightly more advanced parser to do it for regular enums too like enum foo { member1, member2 }
                Strings enum_members = {0};
                while(consume_until_specific_token_type(&lexer, T_IDENTIFIER)) {
                    if(strncmp(lexer.token.loc, "enum", lexer.token.len) == 0) {
                        // {
                        next_token(&lexer);
                        while(next_token(&lexer)) {
                            if(lexer.token.type == '}') break;
                            String enum_member = { .data = lexer.token.loc, .length = lexer.token.len};
                            da_append(&enum_members, enum_member);
                            while(next_token(&lexer)) {
                                if(lexer.token.type == ',' || lexer.token.type == '}') break;
                            }
                            if(lexer.token.type == '}') break;
                        }
                        next_token(&lexer);
                        String enum_name = { .data = lexer.token.loc, .length = lexer.token.len };
                        fprintf(generated_file, "char *%.*s_to_string(%.*s enum_member) {\n", (int)enum_name.length, enum_name.data, (int)enum_name.length, enum_name.data);
                        fprintf(generated_file, "    switch(enum_member) {\n");
                        for(int i = 0; i < enum_members.count; ++i) {
                            fprintf(generated_file, "        case %.*s: return \"%.*s\";\n", (int)enum_members.data[i].length, enum_members.data[i].data, (int)enum_members.data[i].length, enum_members.data[i].data);
                        }
                        fprintf(generated_file, "        default: return NULL;\n");
                        fprintf(generated_file, "    }\n");
                        fprintf(generated_file, "}\n");
                    }
                    enum_members.count = 0;
                }
            }
        }
    }
    fclose(generated_file);
}
