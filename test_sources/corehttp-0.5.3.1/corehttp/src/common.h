/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: common.h
*/

#ifndef COMMON_H
#define COMMON_H

/*
	all constant global variables defined and set here
*/
#define VERSION		"corehttp-0.5.3"
#define BUFSIZE		2048
#define PATHSIZE	256
#define	SETSIZE		16

#ifndef GLOBALS_DEFINED

/*
	all changeable global variables defined here
*/
extern int TIMEOUT, PORT[SETSIZE], BACKLOG;
extern char *ROOTDIR, **DEFPAGE, *PAGE404, *LOGFILE, *DIRLIST,
       **CGIEXT, **CGIBIN;
#include "sprock.h"
extern struct sprock_t *FIRSTSPROCK;

#endif

/*
	initializer
*/
void InitGlobals(int num, char **args);

#endif
