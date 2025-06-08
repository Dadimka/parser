#include <stdio.h>
int square(int x)
{
    return x * x;
}
int main()
{
    int a = 5;
    int b = square(a);
    printf("%d\n", b);
    return 0;
}