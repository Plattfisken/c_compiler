#include <useful_things.h>
#include "lexer.h"

int main(int argc, char **argv) {
    if(argc < 2) {
        printf("Provide a path\n");
        return 1;
    }
    char *file_path = argv[1];
    UT_Arena *arena = UT_arena_create_size(MEGABYTES(8));
    char *file = UT_read_entire_file_and_null_terminate_arena(file_path, arena);

    Lexer lexer = { file };
    for(;;) {
        Token token = next_token(&lexer);
        print_token(token);
        if(token.type == T_EOF) break;
    }

}
