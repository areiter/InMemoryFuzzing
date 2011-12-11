/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: watch.h
*/

#ifndef WATCH_H
#define WATCH_H

#include <stdlib.h>
#include <stdio.h>
#include <fcntl.h>
#include <sys/select.h>
#include "common.h"
#include "sprock.h"

/*
	watch structures, functions
	the structure gets passed around and modified by the watch functions
*/
struct watch_t {
	fd_set read, write, bread, bwrite;
	int highfd;
};

int SetNonBlock(int fd);
struct watch_t *WatchMake(void);
void DeleteWatch(struct watch_t *watch);
int UpdateWatch(struct watch_t *watch, int blockflag);
void AddReadWatch(struct sprock_t *sprocket, struct watch_t *watch);
void AddWriteWatch(struct sprock_t *sprocket, struct watch_t *watch);
void RemoveWatch(struct sprock_t *sprocket, struct watch_t *watch);
int ReadableWatch(struct sprock_t *sprocket, struct watch_t *watch);
int WriteableWatch(struct sprock_t *sprocket, struct watch_t *watch);

#endif
