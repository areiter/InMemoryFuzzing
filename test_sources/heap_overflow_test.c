#include <stdlib.h>
#include <stdio.h>

int main(int argc, char** argv){
  printf("Running heap overflow test\n");

  int some_num = 0;  
  char* data = malloc(100);
  int c;
  
  for(c=0; c<1000; c++){
    data[c] = 'a';
  }
  data[1000] = '\0';
  printf("some_num=%d\n",some_num);
}