/*
 * Building: gcc -DWITH_PTHREADS -nostartfiles --shared -fPIC -g -ldl -o log-malloc.so log-malloc.c
 *        or gcc -nostartfiles --shared -fPIC -g -ldl -o log-malloc.so log-malloc.c
 * Usage: LD_PRELOAD=./log-malloc.so command args ...
 * Homepage: http://www.brokestream.com/wordpress/category/projects/log-malloc/
 * Version : 2007-06-01
 *
 * Ivan Tikhonov, http://www.brokestream.com, kefeer@netangels.ru
 *
 * Changes:
 *   2007-06-01: pthread safety patch for Dan Carpenter
 *
 * Notes:
 *   If you want redirect output to file run:
 *   LD_PRELOAD=./log-malloc.so command args ... 200>filename
 * 
 *   You could use addr2line tool from GNU binutils to convert 0x addresses
 *   into source code line numbers.
 *
 */

/*
  Copyright (C) 2011 Andreas Reiter <andreas.reiter@student.tugraz.at>
  Changes:
    * Output gets written to a named pipe defined in the environment variable LOG_MEM_PIPE
      if none is supplied, stderr is used

    * Every log entry has the following format:
      <function name>: args=[<arg name>=value,...return=value] bt[0x<address>,...]
      
      <function name>...malloc, calloc, free,...
      args...specifies all arguments supplied to the corresponding method including its return vale as the last argument, with the name return (seperated by spaces)
      bt...complete backtrace. The IP addresses are listed from the inner frame to te outer, they are preceded by 0x and seperated by spaces

  Original version from Ivan Tikhonov available at http://brokestream.com/log-malloc.html


  Copyright (C) 2007 Ivan Tikhonov

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Ivan Tikhonov, kefeer@brokestream.com

*/


#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/stat.h>
#include <errno.h>
#include <sys/types.h>
#include <ctype.h>
#include <fcntl.h>
#include <stdarg.h>
#include <unwind.h>

#define __USE_GNU
#include <dlfcn.h>

static void *(*real_malloc) (size_t size) = 0;
static void (*real_free) (void *ptr) = 0;
static void *(*real_realloc) (void *ptr, size_t size) = 0;
static void *(*real_calloc) (size_t nmemb, size_t size) = 0;
static int initialized = 0;
static int fdmemlog = 0;

#define BUFFER_LENGTH 1000
struct buffer_info { char buffer[BUFFER_LENGTH]; int write_pos; } output_buffer;

#ifdef WITH_PTHREADS
#include <pthread.h>
static pthread_mutex_t loglock;
#  define LOCK (pthread_mutex_lock(&loglock));
#  define UNLOCK (pthread_mutex_unlock(&loglock));
#else
#  define LOCK ;
#  define UNLOCK ;
#endif

static void init_me()
{
    if(initialized != 0) return;
    char* pipe_name = getenv("LOG_MEM_PIPE");
    errno = 0;

    if(pipe_name != NULL)
      mkfifo(pipe_name, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP);

    if( pipe_name == NULL || (errno != 0 && errno != EEXIST)){
      fdmemlog = fcntl(STDIN_FILENO,  F_DUPFD, 0);
    }
    else{
      errno = 0;
      fdmemlog = open(pipe_name, O_WRONLY);
    }

    if(fdmemlog < 0){ 
      fdmemlog = fcntl(STDIN_FILENO,  F_DUPFD, 0);
    }


#ifdef WITH_PTHREADS
    pthread_mutex_init(&loglock,0);
#endif
    real_malloc = dlsym(RTLD_NEXT, "malloc");
    real_calloc = dlsym(RTLD_NEXT, "calloc");
    real_free   = dlsym(RTLD_NEXT, "free");
    real_realloc = dlsym(RTLD_NEXT, "realloc");
    initialized = 1;
}

static _Unwind_Reason_Code trace_fcn(void *ctx, void *arg){
    struct buffer_info * my_output_buffer = (struct buffer_info *)arg;
  
    //my_output_buffer->write_pos += snprintf( my_output_buffer->buffer+ my_output_buffer->write_pos, BUFFER_LENGTH- my_output_buffer->write_pos, "%p ", (void*)_Unwind_GetIP(ctx));
    dprintf( fdmemlog, "%p ", (void*)_Unwind_GetIP(ctx));
    
    return _URC_NO_REASON;
}


static void output_mem_allocation(char* called_function, char* fmt, ...){
  va_list param_list;
  LOCK;
  va_start(param_list, fmt);
  
  dprintf(fdmemlog, "%s: args=[", called_function);
  vdprintf(fdmemlog, fmt, param_list);
  dprintf(fdmemlog, "%s", "] bt=[");

  /*output_buffer.write_pos = snprintf(output_buffer.buffer, BUFFER_LENGTH, "%s: args=[", called_function);
  output_buffer.write_pos += vsnprintf(output_buffer.buffer+output_buffer.write_pos, BUFFER_LENGTH-output_buffer.write_pos, fmt, param_list);
  output_buffer.write_pos += snprintf(output_buffer.buffer+output_buffer.write_pos, BUFFER_LENGTH-output_buffer.write_pos, "%s", "] bt=[");
*/
  _Unwind_Backtrace((_Unwind_Trace_Fn)trace_fcn, &output_buffer);

  dprintf(fdmemlog, "%s", "]\n");
  //output_buffer.write_pos  += snprintf(output_buffer.buffer+output_buffer.write_pos, BUFFER_LENGTH-output_buffer.write_pos, "%s", "]\n");

  va_end(param_list);
  //write(fdmemlog, &output_buffer.buffer, output_buffer.write_pos+1);
  UNLOCK;
}

void
__attribute__ ((constructor))
_init (void)
{
  init_me();
}


void *malloc(size_t size)
{
    void *p;
    if(!real_malloc) {
	if(!real_malloc) return NULL;
    }
    p = real_malloc(size);

    output_mem_allocation("malloc", "size=%lu return=%p", size, p);
    
    return p;
}

void *calloc(size_t nmemb, size_t size)
{
    void *p;
    if(!real_calloc) {
	if(!real_calloc) return NULL;
    	real_calloc = dlsym(RTLD_NEXT, "calloc");
	return NULL;
    }
    p = real_calloc(nmemb, size);

    output_mem_allocation("calloc", "num=%lu size=%lu return=%p", nmemb, size, p);

    return p;
}

void free(void *ptr)
{
    if(!real_free) {
	if(!real_free) return;
    }
    real_free(ptr);
    output_mem_allocation("free", "ptr=%p", ptr);

}

void *realloc(void *ptr, size_t size)
{
    void *p;
    if(!real_realloc) {
	if(!real_realloc) return NULL;
    }
    p = real_realloc(ptr, size);

    output_mem_allocation("realloc" "ptr=%p size=%lu return=%p", ptr, size, p);

    return p;
}

