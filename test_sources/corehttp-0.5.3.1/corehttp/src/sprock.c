/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: sprock.c
*/

#include "sprock.h"
#include "watch.h"
#include "http.h"

/* non blocking return accepted client given serversprock, NULL on failure */
struct sprock_t *ClntSprockAccept(struct sprock_t *parentsprock) {
	int clntsock;
	struct sockaddr_in clntaddr;
	size_t clntlen;
	struct sprock_t *sprocket;
	
	clntlen = sizeof(clntaddr);
	if ((clntsock = accept(parentsprock->fd,
		(struct sockaddr *)&clntaddr, &clntlen)) < 0) return NULL;
	if (SetNonBlock(clntsock) < 0) return NULL;

	if ((sprocket = (struct sprock_t *)
		malloc(sizeof(struct sprock_t))) == NULL) return NULL;
	memset(sprocket, 0, sizeof(struct sprock_t));
	sprocket->ipaddr = inet_ntoa(clntaddr.sin_addr);
	sprocket->fd = clntsock;
	sprocket->func = CLNT;
	sprocket->state = INIT;
	sprocket->lastio = time(NULL);
	parentsprock->child = sprocket;
	sprocket->parent = parentsprock;

	return sprocket;
}

/* create non blocking listener sprocket on port, null on failure */
struct sprock_t *ServSprockMake(unsigned short port) {
	int sock;
	struct sockaddr_in servaddr;
	struct sprock_t *sprocket;
	
	if ((sock = socket(PF_INET, SOCK_STREAM, IPPROTO_TCP)) < 0) return NULL;
	memset(&servaddr, 0, sizeof(servaddr));
	servaddr.sin_family = AF_INET;
	servaddr.sin_addr.s_addr = htonl(INADDR_ANY);
	servaddr.sin_port = htons(port);

	if (bind(sock, (struct sockaddr *)&servaddr,
		sizeof(servaddr)) < 0) return NULL;
	if (listen(sock, BACKLOG) < 0) return NULL;
	if (SetNonBlock(sock) < 0) return NULL;   

	if ((sprocket = (struct sprock_t *)
		malloc(sizeof(struct sprock_t))) == NULL) return NULL;
	memset(sprocket, 0, sizeof(*sprocket));
	sprocket->fd = sock;
	sprocket->func = SERV;
	sprocket->state = INIT;
	sprocket->lastio = -1;

	return sprocket;
}

/* send data to a file desc, buffered. upos is num bytes written,
	bpos is total bytes we need to write. if called when upos=bpos, do
	nothing. upos and bpos should both = 0 when this is first called. */
int SprockBufSend(struct sprock_t *sprocket) {
	int i;
	if (sprocket == NULL) return -1;
	i = write(sprocket->fd, sprocket->buffer + sprocket->upos,
		sprocket->bpos - sprocket->upos);
	if ((sprocket->upos += i) < 0) return -1;
	if (i > 0) sprocket->lastio = time(NULL);
	return sprocket->upos;
}

/* receive data from a file desc, buffered. bpos is total bytes of
	buffer used. bpos should equal 0 when this is first called. */
int SprockBufRecv(struct sprock_t *sprocket) {
	int i;
	if (sprocket == NULL) return -1;
	i = read(sprocket->fd, sprocket->buffer + sprocket->bpos,
		BUFSIZE - sprocket->bpos);
        if ((sprocket->bpos += i) < 0) return -1;   
	if (i > 0) sprocket->lastio = time(NULL);
	return sprocket->bpos;
}

/* get the first sprocket in a linked list, given an element in it */
struct sprock_t *FirstSprock(struct sprock_t *sprocket) {
	if (sprocket == NULL) return NULL;
	while (sprocket->prev != NULL) sprocket = sprocket->prev;
	return sprocket;
}

/* return the sprocket after the one we were sent */
struct sprock_t *NextSprock(struct sprock_t *sprocket) {
	if (sprocket == NULL) return NULL;
	return sprocket->next;
}

/* add this sprocket to the linked list of FIRSTSPROCK.
   if there is no "list" just pass two sprockets you want to join
   as a list. adds the sprocket in ascending order list. */
void AddSprock(struct sprock_t *sprocket, struct sprock_t **firstptr) {
	struct sprock_t *first = *firstptr;
	if (first == NULL || sprocket == NULL) return;
	while (sprocket->fd > first->fd) 
		if (first->next != NULL) first = first->next;
		else break;
	if (sprocket->fd <= first->fd) { /* insert before */
		if (first->prev != NULL) first->prev->next = sprocket;
		else *firstptr = sprocket; /* new first */
		sprocket->prev = first->prev;
		first->prev = sprocket;
		sprocket->next = first;
	} else { /* insert after */
		if (first->next != NULL) first->next->prev = sprocket;
		sprocket->next = first->next;
		first->next = sprocket;
		sprocket->prev = first;
	}
}

/* remove this sprocket, from list if needed */
void RemoveSprock(struct sprock_t *sprocket, struct sprock_t **firstptr) {
	if (sprocket == NULL) return;

	/* right-left, we need to re-link after delete */
	if (sprocket->prev != NULL || sprocket->next != NULL) {
		if (sprocket->prev == NULL) {
			sprocket->next->prev = NULL;
			*firstptr = sprocket->next; /* new first */
		} else sprocket->prev->next = sprocket->next;
		if (sprocket->next == NULL) sprocket->prev->next = NULL;
		else sprocket->next->prev = sprocket->prev;
	} else *firstptr = NULL; /* this is the last element */

	/* up-down, we just sever the link totally */
	if (sprocket->child != NULL) sprocket->child->parent = NULL;
	if (sprocket->parent != NULL) sprocket->parent->child = NULL;

	close(sprocket->fd);
	free(sprocket);
}
