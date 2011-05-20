#include <stdio.h>

int xyz;

int bar ()
{
  xyz = 2; /* break in bar */
  return 1;
}

int foo ()
{
  xyz = 1; /* break in foo */
  return bar ();
}

int main ()
{
//  char* text = (char*) malloc(10000);
//  int i;
//  for(i=0; i< 150000; i++)
//    text[i] = 'a';

  xyz = 0;      /* break in main */
  puts("bubu");
  foo ();
  puts("huhu");

  return (xyz == 2 ? 2 : 1);
}
              /* end of main */
