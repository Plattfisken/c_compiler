int write();
int printf();
void my_func() {
    int fd = 1;
    int length = 4;
    write(fd, "Hi!\n", length);
    printf("Hello...\n");
}
int main() {
    int length = 14;
    int fd = 1;
    write(fd, "Hello, world!\n", length);
    my_func();
    printf("end");
}
