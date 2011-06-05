#include <stdio.h>

int xyz;

int bar (int bar_var, int bar_var2)
{
  long long local_bar_val;
  local_bar_val = 123;
  xyz = bar_var; /* break in bar */
  return 1;
}

int foo ()
{
  int foo_var;
  foo_var = 1;
  xyz = 1; /* break in foo */
  return bar (456,0 );
}

int main ()
{
int abc;
abc = 1;
/*char* text = (char*) malloc(10000);
int i;
for(i=0; i< 150000; i++)
    text[i] = 'a';
*/
  xyz = 0;      /* break in main */
  puts("bubu");
  foo ();
  puts("huhu");

  return (xyz == 2 ? 2 : 1);
}
              /* end of main */
