/*
  Copyright (C) 2011 Andreas Reiter <andreas.reiter@student.tugraz.at>

  This file is part of the in-memory fuzzer for embedded devices and represents the control program on the target.

  It listens for connections on a tcp socket at the configured port and controls the remote side of the fuzzer
    * Controls (starts) the gdbserver
    * Connects to the memory logging pipe and send all received data to the remote host
    * May contain other "helper" methods


  The protocol is a simple packet based protocol.
  
  <FUZZ><receiver 4chars><length 2byte><data>

  Each packet starts with the magic number FUZZ (4 bytes) followed the receiver with 4 characters length
  Each receiver specifies a subclass of packets and is freely defined as needed. The receiver also specifies the structure of 
  <data>.
  The maximum length of a single packet may not exceed 10k, so the data may habe a maximum length of 10K-10

  Currently specified receivers:
    ECHO    data: Any string to echo
    PIPE    Listens on a specified pipe and sends all data received to the remote host. For data description see handler_request_pipe
    EXEC    Executes the specified application. For data description see handler_exec
    PROC    Lists all running processes and ther pids and associated user. For data description see handler_proc 
    
*/


#define DEBUG_REQUEST_PIPE

#include <sys/socket.h>
#include <netinet/in.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <pthread.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <proc/readproc.h>

typedef unsigned char byte;

/** Handler method prototypes */
void handler_echo(const byte* data, int16_t data_length);
void handler_request_pipe(const byte* data, int16_t data_length);
void handler_exec(const byte* data, int16_t data_length);
void handler_proc(const byte* data, int16_t data_length);
/**
 *  Structure that contains information about running threads
 */
struct running_thread_info{
  pthread_t thread;

  struct running_thread_info* next;
};


/** Structure that contains information needed by pipe request threads
 */
struct pipe_request_info{
  byte* pipe_name;
  int pipe_id;
  
  struct running_thread_info* request_pipe_running_thread_info;
  
  /** Callback gets called before the pipe-thread exits to and should simple_free all allocated memopre (in pipe_request_info)
      and should call delete_running_thread_info(request_pipe_threads, request_pipe_running_thread_info)
   */
  void (* destroy_callback)(struct pipe_request_info* request_pipe_thread_info);
};


/**
 *  Structure that contains information about registered command handlers
 */ 
struct cmd_callback_info{
  byte * receiver_name;
  void (*handlerfn)(const byte* data, int16_t data_length);
};

struct cmd_callback_info cmd_handlers[] = {
  {"ECHO", handler_echo},
  {"PIPE", handler_request_pipe},
  {"EXEC", handler_exec},
  {"PROC", handler_proc},
  {NULL, NULL}
};

void destroy_thread_request_pipe(struct pipe_request_info* thread_info);

void send_packet(byte *receiver, byte* data, int16_t data_length);
struct running_thread_info* start_thread(struct running_thread_info** threads, void *(*start_routine)(void*), void * arg);
void delete_running_thread_info(struct running_thread_info** first_element, struct running_thread_info* victim);

/** Handler specific thread method prototypes */
void *thread_request_pipe(void* arg);

/** Buffer helper prototypes */
void write_int16_t(byte* buffer, int16_t value);
int16_t read_int16_t(const byte* buffer);
void write_int32_t(byte* buffer, int32_t value);
int32_t read_int32_t(const byte* buffer);
int read_string(byte* write_buffer, int max_length, const byte* data_buffer);
int read_string_malloc(byte** write_buffer, const byte* data_buffer);
int write_string(byte* write_buffer, int max_length, const byte* data_buffer, int16_t length);
int read_string_list(byte*** list, int* read_bytes, const byte* data, int data_length, int extra_items, int start_index);
void free_string_list(byte** list);
void simple_free(void* p);

/** Mutex to lock socket write operations */
pthread_mutex_t socket_mutex = PTHREAD_MUTEX_INITIALIZER;

int connection_fd;


/** List of all threads currently handling pipe inputs */
struct running_thread_info* request_pipe_threads = NULL;

/** Id for the next registered pipe */
int16_t next_pipe_id = 0;




void handler_echo(const byte* data, int16_t data_length){
  byte* output = malloc(data_length + 1);

  strncpy(output, data, data_length);
  output[data_length] = '\0';
  printf("ECHO: %s\n", output);
}


/** BEGIN PROC HANDLERS */

/** 
 *  Handles a PROC request
 *
 *  data: <none>
 *
 *  Reads all running processes, pids and ther users and sends it to the remote side
 *
 *  Returns a RPRC (Response PROC) packet to the remote side with the following data
 *  data: <number of processes int32>[<number of values for single process int16><length int16><value byte*>,...]
 *  
 *  values always have the form of key=value where only the first '=' is of interest
 *  Unknown key are ignored
 *  Known keys are: user:user_id, cmd:command executed, pid:processid
 **/
void
handler_proc(const byte* data, int16_t data_length){
  PROCTAB* proc = openproc(PROC_FILLMEM | PROC_FILLSTAT | PROC_FILLSTATUS | PROC_FILLCOM);
    
  byte output_buffer[9000];

  //Skip the length, will be written later
  int32_t current_buffer_offset = 4;
  int32_t proc_counter = 0;


  proc_t proc_info;
  memset(&proc_info, 0, sizeof(proc_info));
  while (readproc(proc, &proc_info) != NULL) 
  { 
    if(proc_info.cmdline == NULL)
      continue;

    write_int16_t(&output_buffer[current_buffer_offset], 2);
    current_buffer_offset += sizeof(int16_t);

    int cmd_len = strlen(proc_info.cmdline[0]);
    byte* local_buffer = malloc(5+cmd_len);
    snprintf(local_buffer, 5+cmd_len, "cmd=%s", proc_info.cmdline[0]);
    local_buffer[4+cmd_len] = '\0';

    //printf("%d %s %s\n", cmd_len, proc_info.cmdline[0], local_buffer);
    
    write_string(&output_buffer[current_buffer_offset], 9000 - current_buffer_offset, local_buffer, 4+cmd_len); 
    free(local_buffer);
    current_buffer_offset += 4+cmd_len + 2;

    local_buffer = malloc(200);
    int len = snprintf(local_buffer, 200, "pid=%d", proc_info.tid); 
    local_buffer[len] = '\0';
    len = write_string(&output_buffer[current_buffer_offset], 9000 - current_buffer_offset, local_buffer, len);    
    current_buffer_offset += len + 2;
    proc_counter++;
    free(local_buffer);
  }  
  closeproc(proc);


  write_int32_t(output_buffer, proc_counter);
  send_packet("RPRC", output_buffer, current_buffer_offset);
  
}

/** END PROC HANDLERS */


/** BEGIN EXEC HANDLERS */

/**
 *  data: <program_name_length int16><program_name><program_path_length int16><program_path byte*>
 *        <argument_count int16>[<arg1_length int16><arg1 byte*>, <arg2_length int16><arg2 byte*>,...]
 *        <env_count int16>[<env_name1_length int16><env_name1 byte*>,...]
 *
 *  This handler executes the specified program using fork/execve and sets the specified arguments and environment variables
 *  The program_name field can be freely chosen and is included in every response concerning this particular program instance.
 *
 *  Once the process is forked a response is sent containing the new process id
 *  REXS (Response exec status)
 *  data: <program_name_length int16><program_name><pid int32><status int32>
 *
 *  if successful status is 0, otherwise status is an error code and pid is invalid
 */
void
handler_exec(const byte* data, int16_t data_length){
  
  int current_buffer_offset = 0;
  int counter;
  byte* program_name = NULL;
  byte* program_path = NULL;

  if(data_length < sizeof(int16_t))
    goto handler_exec_exit;

  int program_name_length = read_string_malloc(&program_name, data);
  current_buffer_offset = sizeof(int16_t)+program_name_length;  

  if(data_length < current_buffer_offset + sizeof(int16_t))
    goto handler_exec_exit;

  int program_path_length = read_string_malloc(&program_path, &data[current_buffer_offset]);
  current_buffer_offset += sizeof(int16_t) + program_path_length;

  if(data_length < current_buffer_offset + sizeof(int16_t))
    goto handler_exec_exit;

  byte** spawn_argv = NULL;
  int read_bytes = 0;
  int arg_count = read_string_list(&spawn_argv, &read_bytes, &data[current_buffer_offset], data_length-current_buffer_offset, 1, 1);
  current_buffer_offset += read_bytes;
  //First argument is always the name of the executable, as it was launched
  spawn_argv[0] = strdup(program_path);

  if(arg_count < 0)
    goto handler_exec_exit;

  if(data_length < current_buffer_offset + sizeof(int16_t))
    goto handler_exec_exit;

  byte** spawn_envp = NULL;
  int env_count = read_string_list(&spawn_envp, &read_bytes, &data[current_buffer_offset], data_length-current_buffer_offset, 0, 0);
  
  if(env_count < 0)
    goto handler_exec_exit;  

  pid_t pid;
  int spawn_result = posix_spawn(&pid, program_path, NULL, NULL, spawn_argv, spawn_envp);

  byte* status_response = malloc(sizeof(int16_t) + program_name_length  + 2* sizeof(int32_t));
  if(spawn_result != 0)
  {
    write_string(status_response, sizeof(int16_t) + program_name_length  + 2* sizeof(int32_t), program_name, program_name_length);
    write_int32_t(&status_response[program_name_length  + sizeof(int16_t)], 0);
    write_int32_t(&status_response[program_name_length  + sizeof(int16_t) + sizeof(int32_t)], spawn_result);
    send_packet("REXS", status_response, sizeof(int16_t) + program_name_length + 2* sizeof(int32_t));
  }
  else
  {
    write_string(status_response, sizeof(int16_t) + program_name_length  + 2* sizeof(int32_t), program_name, program_name_length);
    write_int32_t(&status_response[program_name_length  + sizeof(int16_t)], pid);
    write_int32_t(&status_response[program_name_length  + sizeof(int16_t) + sizeof(int32_t)], spawn_result);
    send_packet("REXS", status_response, sizeof(int16_t) + program_name_length + 2* sizeof(int32_t));
  }

handler_exec_exit:
  if(program_name != NULL)
    simple_free(program_name);

  if(program_path != NULL)
    simple_free(program_path);
  
  if(spawn_argv != NULL)
    free_string_list(spawn_argv);

  if(spawn_envp != NULL)
    free_string_list(spawn_envp);

}


/** END EXEC HANDLERS */

/** BEGIN REQUEST PIPE HANDLERS */

/**
 *  data: <pipe_name byte*>
 *
 *  This handler sends an immediate response packet
 *  RPIR (Reponse pipe registered)
 *  data: int16 (2byte) pipe_id
 *
 *  Opens the specified pipe for reading and sends all received data in 
 *  RPIP (Response pipe) packets.
 *  data: <int16 pipe_id><pipe_data>
 *  The process is started in a seperate thread to ensure that other requests can be handled simultanious
 */
void 
handler_request_pipe(const byte* data, int16_t data_length){

  struct pipe_request_info* thread_info = (struct pipe_request_info*)malloc(sizeof(struct pipe_request_info));
  thread_info->pipe_id = next_pipe_id;
  next_pipe_id++;

  thread_info->pipe_name = (byte*)malloc(data_length+1);
  strncpy(thread_info->pipe_name, data, data_length);
  thread_info->pipe_name[data_length] = '\0';

  thread_info->destroy_callback = destroy_thread_request_pipe;

#ifdef DEBUG_REQUEST_PIPE
  printf("DEBUG_REQUEST_PIPE - handler_request_pipe: Client registered for new pipe with name '%s' and id '%d'\n", thread_info->pipe_name, thread_info->pipe_id);
#endif

  byte buffer[2];
  write_int16_t(buffer, thread_info->pipe_id);

#ifdef DEBUG_REQUEST_PIPE
  printf("DEBUG_REQUEST_PIPE - handler_request_pipe: Sending response pipe registered (RPIR) packet\n", thread_info->pipe_name, thread_info->pipe_id);
#endif

  send_packet("RPIR", buffer, 2);  
  thread_info->request_pipe_running_thread_info = 
    start_thread(&request_pipe_threads, &thread_request_pipe, (void*)thread_info);
}

void* 
thread_request_pipe(void* arg){
  struct pipe_request_info* thread_info = (struct pipe_request_info*)arg;

#ifdef DEBUG_REQUEST_PIPE
  printf("Openning pipe '%s' for reading\n", thread_info->pipe_name);
#endif

  if(thread_info->pipe_name == NULL || strlen(thread_info->pipe_name) == 0){
    fprintf(stderr, "Error thread_request_pipe: could not find pipe name");
    return;
  }
  
  mkfifo(thread_info->pipe_name, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP);

  if(errno != 0 && errno != EEXIST){
    fprintf(stderr, "Error thread_request_pipe: could not create pipe (errorcode: %d)", errno);
    return;
  }

  int fdmemlog = open(thread_info->pipe_name, O_RDONLY);

  if(fdmemlog < 0){ 
    fprintf(stderr, "Error thread_request_pipe: could not open pipe for reading (errorcode: %d)", errno);
    return;
  }

  byte buffer[1003];

  write_int16_t(buffer, thread_info->pipe_id);

  int read_bytes = 0;  
  do{
    read_bytes = read(fdmemlog, &buffer[2], 1000);

    if(read_bytes > 0){
      buffer[read_bytes+2] = '\0';

#ifdef DEBUG_REQUEST_PIPE
      printf("DEBUG_REQUEST_PIPE - thread_request_pipe: Sending [pipeid: %d, #%d bytes]: %s\n", thread_info->pipe_id, read_bytes+2, &buffer[2]);
#endif

      send_packet("RPIP", buffer, read_bytes+2);
    }
  } while(read_bytes > 0);

  //Pipe has been closed, send RPIC Response Pipe closed packet
  send_packet("RPIC", buffer, 2);

  thread_info->destroy_callback(thread_info);
  
  pthread_exit(NULL);
}

void 
destroy_thread_request_pipe(struct pipe_request_info* thread_info)
{
  simple_free(thread_info->pipe_name);
  delete_running_thread_info(&request_pipe_threads, thread_info->request_pipe_running_thread_info);
}

/** END REQUEST PIPE HANDLERS */

/** Starts a new thread and adds it to the specified thread list */
struct running_thread_info*
start_thread(struct running_thread_info** threads, void *(*start_routine)(void*), void * arg){
  struct running_thread_info *current_thread_info = *threads;  
  while(current_thread_info != NULL && current_thread_info->next != NULL)
    current_thread_info = current_thread_info->next;

  struct running_thread_info* new_thread_info = (struct running_thread_info*)malloc(sizeof(struct running_thread_info));
  new_thread_info->next = NULL;
  pthread_create(&(new_thread_info->thread), NULL, start_routine, arg);

  if(current_thread_info == NULL)
    *threads = new_thread_info;
  else
    current_thread_info->next = new_thread_info;

  return new_thread_info;
}

/** Detaches victim from the list specified by first_element and simple_free the memory used by victim
 */
void
delete_running_thread_info(struct running_thread_info** first_element, struct running_thread_info* victim){
  struct running_thread_info* prev_element, *current_element;
  prev_element = current_element = NULL;
  do{
    if(current_element == NULL)
      current_element = *first_element;
    else{
      if(current_element->next == NULL)
        break;

      prev_element = current_element;
      current_element = prev_element->next;
    }
  }while(current_element != NULL && current_element != victim);

  if(current_element == victim){
    if(prev_element == NULL)
      //Victim is the first element
      *first_element = current_element->next;
    else
      //Victim is any element after the first
      prev_element->next = current_element->next;

    //And delete all allocated memory
    simple_free(current_element);

  }
}

/**
 *  Copies the string in the buffer from the specified  start_index and of 
 *  the specified length to the beginning of the buffer
 */
void copy_to_start(byte* buffer, int start_index, int length){
  memmove(buffer, buffer+start_index, length);
}

int16_t read_int16_t(const byte* buffer){
  int16_t val = (int16_t)buffer[0] +
        (int16_t)(buffer[1]<<8);

  return val;  
}

void write_int16_t(byte* buffer, int16_t value){
  buffer[0] = (byte)(value & 0x00FF);
  buffer[1] = (byte)((value & 0xFF00)>>8);
}

int32_t read_int32_t(const byte* buffer){
  int32_t val = (int32_t)buffer[0] +
        (int32_t)(buffer[1]<<8) + 
        (int32_t)(buffer[2]<<16) + 
        (int32_t)(buffer[3]<<24);

  return val;  
}

void write_int32_t(byte* buffer, int32_t value){
  buffer[0] = (byte)(value & 0x000000FF);
  buffer[1] = (byte)((value & 0x0000FF00)>>8);
  buffer[2] = (byte)((value & 0x00FF0000)>>16);
  buffer[3] = (byte)((value & 0xFF000000)>>24);
}

/** Reads an int16 length and <length> bytes from data_buffer.
 *  String is written to write_buffer and gets null terminate
 */
int read_string(byte* write_buffer, int max_length, const byte* data_buffer){
  int16_t string_length = read_int16_t(data_buffer);
  
  if(max_length < string_length + 1)
    return -1;
  
  strncpy(write_buffer, &data_buffer[2],string_length);
  write_buffer[string_length] = '\0';
  return string_length;
}

int read_string_malloc(byte** write_buffer, const byte* data_buffer){
  int16_t string_length = read_int16_t(data_buffer);
  
  *write_buffer = malloc(sizeof(byte) * (string_length + 1));

  strncpy(*write_buffer, &data_buffer[2],string_length);
  (*write_buffer)[string_length] = '\0';
  return string_length;
}

/** Reads the specified amount of chars from data_buffer and writes it to write_buffer
    <length int16><data>
    If the buffer is too small -1 is returned, otherwise the number of chars written
    if length is < 0, strlen is called on data_buffer
 */
int write_string(byte* write_buffer, int max_length, const byte* data_buffer, int16_t length){
  if(length < 0)
    length = strlen(data_buffer);

  if(max_length < length + 2)
    return -1;

  write_int16_t(write_buffer, length);
  memcpy(&write_buffer[2], data_buffer, length);
  return length;
}

int 
read_string_list(byte*** list, int* read_bytes, const byte* data, int data_length, int extra_items, int start_index){
  int current_buffer_offset = 0;
  int counter;
  int error = 0;

  if(extra_items < start_index)
    return -1;

  int16_t arg_count = read_int16_t(&data[current_buffer_offset]);
  current_buffer_offset += sizeof(int16_t);
  
  *list = malloc(sizeof(byte*) * (arg_count + extra_items + 1));
  
  (*list)[arg_count + extra_items] = NULL;

  for(counter = 0; counter<arg_count; counter++){
    if(data_length < current_buffer_offset + sizeof(int16_t)){
      error = 1;
      break;
    }

    byte* current_arg = NULL;
    int16_t arg_length = read_string_malloc(&current_arg, &data[current_buffer_offset]);
    
    current_buffer_offset += sizeof(int16_t) + arg_length;
    (*list)[counter + start_index] = current_arg;
  }

  if(error != 0){
    for(;counter>=0;counter--){
      simple_free((*list)[counter + start_index]);
    }
    simple_free(*list);
    *read_bytes = 0;
    return 0;  
  }

  *read_bytes = current_buffer_offset;
  return arg_count;
}

void 
free_string_list(byte** list){
  if(list == NULL)
    return;

  int counter = 0;
  byte* list_item = list[0];
  while(list_item != NULL){
    counter++;

    simple_free(list_item);
    list_item = list[counter];
  }
  simple_free(list);
}

void
simple_free(void* p)
{
  free(p);
}


void 
process_packet(const byte* receiver, const byte* data, int16_t data_length){
  printf("Processing packet for receiver '%s'\n", receiver);

  int handler_counter = 0;
  struct cmd_callback_info* handler = NULL;

  while(1){
    if(cmd_handlers[handler_counter].receiver_name == NULL)
      break;

    if(strncmp(cmd_handlers[handler_counter].receiver_name, receiver, 4) == 0){
      handler = &(cmd_handlers[handler_counter]);
      break;
    }
    handler_counter++;
  }
  
  if(handler != NULL)
    handler->handlerfn(data, data_length);
}


void extract_packet(byte *buffer, int start_offset, int16_t data_length){
  byte receiver[5];

  //extract_receiver
  strncpy(receiver, buffer+start_offset+4, 4);
  receiver[4] = '\0';

  process_packet(receiver, buffer+start_offset+10, data_length);
  
} 

void send_packet(byte *receiver, byte* data, int16_t data_length){
  if(strlen(receiver) != 4){
    printf("Error: receiver needs to be of length 4");
    exit(-1);
  }

  pthread_mutex_lock(&socket_mutex);
  write(connection_fd, "FUZZ", 4);
  write(connection_fd, receiver, 4);

  byte data_length_buffer[2];
  write_int16_t(data_length_buffer, data_length);
  write(connection_fd, data_length_buffer, 2);
  write(connection_fd, data, data_length);

  pthread_mutex_unlock(&socket_mutex);
}

/**
 *  Receives packets from the remote side as long as the connection remains open
 *  @param fd socket file descriptor
 */
void receive_packet_loop(int fd){
  int current_read_bytes = 0;
  int write_pos = 0;
  byte* buffer = malloc(10001);
  int c = 0;

  do{
    current_read_bytes = read(fd, buffer+write_pos, 10000 - write_pos);
    printf("received #%dbytes\n", current_read_bytes);
    if(current_read_bytes > 0){
      write_pos += current_read_bytes;
      buffer[write_pos] = '\0';
      //Look for the first appearance of the magic word
      for(c=0; c<write_pos-3; c++)
      {
        if(buffer[c] == 'F' &&
           buffer[c+1] == 'U' &&
           buffer[c+2] == 'Z' &&
           buffer[c+3] == 'Z')
        {
          //we found the magic word, let's see if the packet is already complete
          printf("Found magic word at '%d' writepos=%d \n", c, write_pos);
          //First check if the minimum length of 10 bytes is reached (FUZZ + receiver + data length)
          if(write_pos-c >= 10)
          {
            printf("Minimum packet length available\n");
            int read_index = c + 8;
            int16_t data_length = read_int16_t(&buffer[read_index]);
            read_index += sizeof(data_length);

            printf("\tdata_length=%d\n", data_length);
            if(write_pos-c >= 10+data_length){
              extract_packet(buffer, c, data_length);
              copy_to_start(buffer, c+10+data_length, write_pos - c - 10 - data_length);
              write_pos=write_pos - c - 10 - data_length;
              c=-1;
            }
            else{
              printf("Packet not complete, copy it to the buffer start ___the second___\n");
              //It's sure...the packet is not complete, move it to the beginning of the buffer and wait
              copy_to_start(buffer, c, write_pos-c);
              write_pos -= c;
              break;
            }

              

          }
          else if(c > 0)
          {
            printf("Packet not complete, copy it to the buffer start\n");
            //It's sure...the packet is not complete, move it to the beginning of the buffer and wait
            copy_to_start(buffer, c, write_pos-c);
            write_pos -= c;
            break;
          }
          else
          {
            printf("Packet not complete, already at start\n");
            break;
          }
        }
      }

    }
    
  }while(current_read_bytes > 0);

  simple_free(buffer);
}

int main(int argc, byte**argv){
   int listenfd,n;
   struct sockaddr_in servaddr,cliaddr;
   socklen_t clilen;
   pid_t     childpid;


   listenfd=socket(AF_INET,SOCK_STREAM,0);
  
   int optval = 1;
   setsockopt(listenfd, SOL_SOCKET, SO_REUSEADDR, &optval, sizeof(optval));

   bzero(&servaddr,sizeof(servaddr));
   servaddr.sin_family = AF_INET;
   servaddr.sin_addr.s_addr=htonl(INADDR_ANY);
   servaddr.sin_port=htons(8899);

   int bind_result = bind(listenfd,(struct sockaddr *)&servaddr,sizeof(servaddr));
  
   if(bind_result != 0)
     printf("bind result=%d errno=%d error=%s\n", bind_result, errno, strerror(errno) );


   int listen_result = listen(listenfd,1024);

   if(listen_result != 0)
     printf("listen result=%d errno=%d error=%s\n", listen_result, errno, strerror(errno) );

   //accept() loop
   for(;;)
   {
      clilen=sizeof(cliaddr);
      connection_fd = accept(listenfd,(struct sockaddr *)&cliaddr,&clilen);
      printf("acceppted client connection with fd=%d\n", connection_fd);
      receive_packet_loop(connection_fd );

      close(connection_fd );
   }
}


