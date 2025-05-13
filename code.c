int write();
int main() {
    int length = 14;
    int fd = 1;
    write(fd, "Hello, world!\n", length);
}
