/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: watch.c
*/

#include "watch.h"

/* set a file descriptor as non-blocking, non-negative on success. */
int SetNonBlock(int fd) {
	return fcntl(fd, F_SETFL, O_NONBLOCK);
}

/* allocate and initialize a new watch structure */
struct watch_t *WatchMake(void) {
	struct watch_t *watch;

	if ((watch = (struct watch_t *)
		malloc(sizeof(struct watch_t))) == NULL) return NULL;
	memset(watch, 0, sizeof(*watch));
	/* FD_ZERO(&watch->bread); 
	FD_ZERO(&watch->bwrite); */
	watch->highfd = -1;

	return watch;
}

/* delete a watch structure */
void DeleteWatch(struct watch_t *watch) {
	if (watch == NULL) return;
	free(watch);
}

/* call select() to block poll all open file descriptors, return
	non-negative on success - number of useable file descriptors. */
int UpdateWatch(struct watch_t *watch, int blockflag) {
	struct timeval timeout;

	if (watch == NULL) return -1;

	if (blockflag == 0) {
		timeout.tv_sec = 0;
		timeout.tv_usec = 0;
	} else {
		timeout.tv_sec = TIMEOUT;
		timeout.tv_usec = 0;
	}
	watch->read = watch->bread;
	watch->write = watch->bwrite;

	return select(watch->highfd + 1,
		&watch->read, &watch->write, NULL, &timeout);
}

/* add a new file descriptor to read fdset */
void AddReadWatch(struct sprock_t *sprocket, struct watch_t *watch) {
	if (watch == NULL || sprocket == NULL) return;
	FD_SET(sprocket->fd, &watch->bread);
	if (sprocket->fd > watch->highfd) watch->highfd = sprocket->fd;
}

/* add a new file descriptor to write fdset */
void AddWriteWatch(struct sprock_t *sprocket, struct watch_t *watch) {
	if (watch == NULL || sprocket == NULL) return;
	FD_SET(sprocket->fd, &watch->bwrite);
	if (sprocket->fd > watch->highfd) watch->highfd = sprocket->fd;
}

/* remove a file descriptor from all fdsets */
void RemoveWatch(struct sprock_t *sprocket, struct watch_t *watch) {
	if (watch == NULL || sprocket == NULL || sprocket->fd < 0) return;
	FD_CLR(sprocket->fd, &watch->bread);
	FD_CLR(sprocket->fd, &watch->bwrite);
   
	/* need to change the high file descriptor? sprock list is in
	   ascending order. */
	if (sprocket->fd == watch->highfd) {
		if (sprocket->prev != NULL) watch->highfd = sprocket->prev->fd;
		else watch->highfd = -1;
	}
}

/* 1 if can read from sprocket->fd, 0 if not */
int ReadableWatch(struct sprock_t *sprocket, struct watch_t *watch) {
	if (watch == NULL || sprocket == NULL) return -1;
	return FD_ISSET(sprocket->fd, &watch->read);
}

/* 1 if can write to sprocket->fd, 0 if not */
int WriteableWatch(struct sprock_t *sprocket, struct watch_t *watch) {
	if (watch == NULL || sprocket == NULL) return -1;
	return FD_ISSET(sprocket->fd, &watch->write);
}
