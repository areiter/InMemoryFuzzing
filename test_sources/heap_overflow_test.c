#include <stdlib.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char** argv){
  printf("Running heap overflow test press enter to continue\n");
 
  //getc(stdin);

  int some_num = 0;  
  char* data = malloc(100);
  
  memset(data, 0, 150);

  printf("some_num=%d\n",some_num);
}
