int main(int argc, char **argv) {
    if(argc < 2) {
        printf("Provide a path\n");
        return 1;
    }
    char *file_path = argv[1];
    Arena *arena = arena_create_size(MEGABYTES(8));
    String file_contents = read_entire_file_as_string(null_term_to_string(file_path), arena);

    String preprocessed_file = preprocess(file_contents, arena);
    printf("%.*s\n", preprocessed_file.length, preprocessed_file.data);

#if 0
    Lexer lexer = { .at = file_contents.data };
    while(next_token(&lexer)) {
        print_token(lexer.token);
    }
#endif
}
