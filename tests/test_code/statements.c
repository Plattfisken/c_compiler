int printf(char *, ...);
int main(void) {
    int x = 5;
    if(x == 5) {
        while(x < 100)
            ++x;
    }
    else if(x < 0) do { x--; } while(x > -100);
    for(int i = 0; i < 10; ++i)
        printf("%d", x);
    return x;
}
