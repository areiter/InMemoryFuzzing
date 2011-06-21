#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/stat.h>
#include <errno.h>
#include <sys/types.h>
#include <ctype.h>
#include <fcntl.h>



int main(int argc, char** argv){
  char* pipe_name = getenv("LOG_MEM_PIPE");
  
  if(pipe_name == NULL){
    fprintf(stderr, "Error: could not find pipe name");
    return -10;
  }
  
  errno = 0;
  mkfifo(pipe_name, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP);

  if(errno != 0 && errno != EEXIST){
    fprintf(stderr, "Error: could not create pipe (errorcode: %d)", errno);
    return -11;
  }

  errno = 0;
  int fdmemlog = open(pipe_name, O_RDONLY);

  if(fdmemlog < 0){ 
    fprintf(stderr, "Error: could not open pipe for reading (errorcode: %d)", errno);
    return -12;
  }

  char buffer[1001];
  int read_bytes = 0;
  
  do{
    read_bytes = read(fdmemlog, &buffer, 1000);
   
    if(read_bytes > 0){
      buffer[read_bytes] = '\0';
      //puts(buffer);
      fprintf(stdout, "%s", buffer);
    }
  } while(read_bytes > 0);

}
