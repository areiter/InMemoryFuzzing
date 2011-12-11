/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: sprock.h
*/

#ifndef SPROCK_H
#define SPROCK_H

#include <stdlib.h>
#include <stdio.h>
#include <time.h>
#include <string.h>
#include <unistd.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include "common.h"

/*
	sprocket structures, functions
*/
enum sprockstate_t { INIT, RECV, SEND, IDLE, TERM };
enum sprockfunc_t { SERV, CLNT, HTTP };

/* create a linked list of these and handle the ones select() says are ready */
struct sprock_t {
	enum sprockstate_t state;
	enum sprockfunc_t func;
	time_t lastio;
	int fd, /* file descriptor */
		bpos, upos; /* bytes in buffer, handled offset */
	struct sprock_t *next, *prev, *parent, *child; /* linked list/tree */
	char buffer[BUFSIZE], 
		*ipaddr, url[PATHSIZE]; /* for logging purposes */
};

struct sprock_t *ServSprockMake(unsigned short port);
struct sprock_t *ClntSprockAccept(struct sprock_t *parentsprock);

int SprockBufSend(struct sprock_t *sprocket);
int SprockBufRecv(struct sprock_t *sprocket);

/* linked list functions - give them any element within the list as param */
struct sprock_t *FirstSprock(struct sprock_t *sprocket);
struct sprock_t *NextSprock(struct sprock_t *sprocket);
void AddSprock(struct sprock_t *sprocket, struct sprock_t **firstptr);
void RemoveSprock(struct sprock_t *sprocket, struct sprock_t **firstptr);

#endif
