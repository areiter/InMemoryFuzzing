#include <stdio.h>
#include <stdlib.h>

int xyz;

int bar (char* mydata)
{
  printf("%s \n", mydata);
  fflush(stdout);
  return 1;
}

int foo ()
{
  int foo_var;
  foo_var = 1;
  xyz = 1; /* break in foo */
  return bar ("ORIGINAL_DATA" );
}

int main ()
{
int abc;
abc = 1;
//char* text = "hello world";
//*text = 'H';
//  getchar();
  xyz = 0;      /* break in main */
  puts("bubu");
  foo ();
  puts("huhu");

  return (xyz == 2 ? 2 : 1);
}
              /* end of main */
