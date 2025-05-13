int write();
int printf();
int external_function();
void my_func() {
    int fd = 1;
    int length = 4;
    write(fd, "Hi!\n", length);
    printf("Hello...\n");
    external_function();
}
int main() {
    int length = 14;
    int fd = 1;
    write(fd, "Hello, world!\n", length);
    my_func();
    printf("end");
}
