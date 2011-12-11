/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: handler.h
*/

#ifndef HANDLER_H
#define HANDLER_H

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include <sys/wait.h>
#include "common.h"
#include "sprock.h"

/*
	logfile things and signal handlers. all logging is blocking
*/
FILE *logfile;
void HandleChildProcs(int signum);
void HandleExit(int signum);
void HandleError(char *errstr);
void HandleProblem(char *errstr);
void HandleLog(char *str);
int HandlerInit(void);

#endif
