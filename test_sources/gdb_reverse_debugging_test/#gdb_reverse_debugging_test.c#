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
  xyz = 0;      /* break in main */
  foo ();
  return (xyz == 2 ? 0 : 1);
} 
              /* end of main */
