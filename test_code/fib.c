int printf(char *, ...);
int fib(int n) {
    if(n < 1) return n;
    int fib1 = 0;
    int fib2 = 1;
    for(int i = 1; i < n; ++i) {
        int new_fib = fib1 + fib2;
        fib1 = fib2;
        fib2 = new_fib;
    }
    return fib2;
}
int main(void) {
    int n = 0;
    while(n < 10) {
        printf("%d\n", fib(n++));
    }
}
