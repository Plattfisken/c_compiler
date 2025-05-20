int printf(char *);
int main(void) {
    int a = 5;
    int b = a + 2;
    if(a > b) {
        printf("First statement true\n");
    }
    else
        printf("First statement false\n");
    if(a <= b) {
        printf("Second statement true\n");
    } else if(a)
        printf("Second statement false and third statement true\n");
    else { printf("Second statement and third statement false"); }
    if(a) {
        printf("fourth statement true\n");
    }
    return 0;
}
