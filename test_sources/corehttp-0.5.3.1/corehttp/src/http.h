/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: http.h
*/

#ifndef HTTP_H
#define HTTP_H

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <unistd.h>
#include <sys/stat.h>
#include "common.h"
#include "sprock.h"
#include "watch.h"

/*
   http functions.
   TODO: read http protocol information like mimetype from settings file
*/
struct sprock_t *HttpSprockMake(struct sprock_t *parentsprock);
int HttpFileStat(char *filename);

#endif
