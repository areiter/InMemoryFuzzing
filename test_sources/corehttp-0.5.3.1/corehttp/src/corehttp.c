/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: corehttp.c
*/

#include "corehttp.h"

int main(int argc, char **argv) {
	struct sprock_t *sprocket, *newsprocket;
	struct watch_t *watcher;
	char buffer[BUFSIZE], tinybuffer[SETSIZE]; /* for logging ... */
	int blockflag, i;
	time_t curtime;
	
	/* get command line args */
	InitGlobals(argc, argv);
	
	/* detach */
	/*daemon(0, 0);*/
   	
	/* initialize handlers */
	if (HandlerInit() < 0) HandleExit(-1);
	sprintf(buffer, VERSION " started. TIMEOUT=%d BACKLOG=%d ROOTDIR=%s"
		" PAGE404=%s LOGFILE=%s DIRLIST=%s\n", 
		TIMEOUT, BACKLOG, ROOTDIR, PAGE404, LOGFILE, DIRLIST);
	strcat(buffer, "PORTs: ");
	for (i = 0; PORT[i] != -1 && i < SETSIZE; i++) {
		sprintf(tinybuffer, "%d ", PORT[i]);
		strcat(buffer, tinybuffer);
	}
	strcat(buffer, "\nDEFPAGEs: ");
	for (i = 0; DEFPAGE[i][0] != '\0' && i < SETSIZE; i++) {
		sprintf(tinybuffer, "%s ", DEFPAGE[i]);
		strcat(buffer, tinybuffer);
	}
	strcat(buffer, "\nCGIEXTs/CGIBINs: ");
	for (i = 0; CGIEXT[i][0] != '\0' && i < SETSIZE; i++) {
		sprintf(tinybuffer, "(%s)-(%s) ", CGIEXT[i], CGIBIN[i]);
		strcat(buffer, tinybuffer);
	}
	HandleLog(buffer);
 
	/* initialize watch */
	if ((watcher = WatchMake()) == NULL) 
		HandleError("couldn't initialize polling system");

	/* the first server sprocket */
	if ((FIRSTSPROCK = ServSprockMake(PORT[0])) == NULL) 
		HandleError("couldn't make initial server socket");
	
	/* the other server sprockets */
	for (i = 1; PORT[i] != -1 && i < SETSIZE; i++) {
		if ((sprocket = ServSprockMake(PORT[i])) == NULL) 
			HandleProblem("couldn't make multiplex server socket");
		AddSprock(sprocket, &FIRSTSPROCK);
	}

	/* main loop */
	for ( ;; ) {
		
		/* don't block if there is anything INITing or TERMing */
		sprocket = FIRSTSPROCK;
		blockflag = 1;
		curtime = time(NULL);
		do {
			/* if socket has a timeout, and it is expired, or it
			   has wrapped around the max size of an int */
			if (sprocket->lastio >= 0
				&& ( (curtime-sprocket->lastio >= TIMEOUT) 
				|| (curtime-sprocket->lastio < 0) )) {
				sprocket->state = TERM;
			}
			if (sprocket->state == TERM
				|| sprocket->state == INIT) {
				blockflag = 0;
				break;
			}
			
		} while ((sprocket = NextSprock(sprocket)) != NULL);
		if (UpdateWatch(watcher, blockflag) < 0) {
			HandleProblem("temporary problem polling?");
			continue;
		}

		/* cycle through and handle each sprocket */
		sprocket = FIRSTSPROCK; 

		/* big switch - cycle through each sprocket and handle */
		do {

switch (sprocket->func) {
case SERV: /* SERVer */
	switch (sprocket->state) {
	case INIT: /* add listening watch for socket */
		AddReadWatch(sprocket, watcher);
		sprocket->state = RECV;
		break;
	case RECV: /* listen for a connection */
		if (ReadableWatch(sprocket, watcher) == 0) break;
		if ((newsprocket = ClntSprockAccept(sprocket)) == NULL)
			HandleProblem("botched client accept");
		else AddSprock(newsprocket, &FIRSTSPROCK);
		break;
	case TERM: /* delete this sprocket */
	default:
		RemoveWatch(sprocket, watcher);
		RemoveSprock(sprocket, &FIRSTSPROCK);
	}
	break;
               
case CLNT: /* CLieNT */
	switch (sprocket->state) {
	case INIT: /* add a read watch for this socket */
		AddReadWatch(sprocket, watcher);
		sprocket->state = RECV;
		/* upos and bpos are both 0, as is required for sprockbufrecv */
		break;
	case RECV: /* wait until we recieve anything */
		if (ReadableWatch(sprocket, watcher) == 0) break;
		if (SprockBufRecv(sprocket) <= 0) {
			HandleProblem("failed to sockrecv when watch allowed");
			sprocket->state = TERM;
		}

		/* only a malicious request is that big */
		if (sprocket->bpos >= BUFSIZE) sprocket->state = TERM;
		
		/* is the request done? */
		if (strstr(sprocket->buffer, "\r\n\r\n") == NULL) break;
		RemoveWatch(sprocket, watcher);
		AddWriteWatch(sprocket, watcher);
                           
		/* make an http sprocket and load its buffer with the request */
		if ((newsprocket = HttpSprockMake(sprocket)) == NULL) {
			HandleProblem("couldn't make HTTP processor");
			sprocket->state = TERM;
		} else AddSprock(newsprocket, &FIRSTSPROCK);
		sprintf(buffer, "%s requests '%s'", sprocket->ipaddr,
			sprocket->url);
		HandleLog(buffer);
                           
		/* make SEND wait until httpsprock has a chunk to send */
		sprocket->upos = sprocket->bpos = 0;
		sprocket->state = SEND;
		break;
	case SEND: /* read from file sprocket, send over socket */
		if (sprocket->upos >= sprocket->bpos) { /* we finished chunk */
			if (sprocket->child == NULL)  /* no more chunks */
				sprocket->state = TERM;
			else if (sprocket->child->bpos > 0) { /* more chunks */
				sprocket->bpos = sprocket->child->bpos;
				sprocket->upos = 0;
				memcpy(sprocket->buffer,
					sprocket->child->buffer, 
					sprocket->child->bpos);
				/* tell child to read more */
				sprocket->child->bpos = 0;
			} /* else chunk isn't ready yet */ 
		} else if (WriteableWatch(sprocket, watcher) == 1)
			if (SprockBufSend(sprocket) < 0) {
				HandleProblem("failed to socksend when watches"
					" allowed");
				sprocket->state = TERM;
			}
			break;
	case TERM: /* terminate */
	default:
		RemoveWatch(sprocket, watcher);
		RemoveSprock(sprocket, &FIRSTSPROCK);
		break;
	}
	break;

case HTTP: /* http processor */
	switch (sprocket->state) {
	case INIT: /* parse request and set up for send */
		sprocket->bpos = strlen(sprocket->buffer);
		sprocket->state = IDLE; /* send header */ 
		break;
	case RECV: /* read from file */
		if (sprocket->parent == NULL) sprocket->state = TERM;
		
		/* flow control: if buffer is full, IDLE for client to read 
			buffer and stop watching for read on this FD */
		if (sprocket->bpos >= BUFSIZE) {
			RemoveWatch(sprocket, watcher);
			sprocket->state = IDLE;
			break;
		}
		
		if (ReadableWatch(sprocket, watcher) == 0) break;
		/* so now we got some stuff to write to clnt. add write watch to
			parent if we JUST came from IDLE */
		if (sprocket->bpos == 0)
			AddWriteWatch(sprocket->parent, watcher);
		if (SprockBufRecv(sprocket) < 0) {
			HandleProblem("failed to buffered receive file");
			sprocket->state = TERM;
			break;
		}
		/* buffer empty after successful read, we're done */
		if (sprocket->bpos == 0) sprocket->state = TERM;
		break;
	case IDLE: /* wait for client to get contents of buffer */
		if (sprocket->parent == NULL) sprocket->state = TERM;
		/* client just emptied buffer - read more, add read watch */
		if (sprocket->bpos == 0) {
			if (sprocket->fd < 0) { /* no body, just send head */
				sprocket->state = TERM;
				break;
			}
			/* flow control: no write watch for
				parent because nothing to write */
			RemoveWatch(sprocket->parent, watcher);
			AddReadWatch(sprocket, watcher);
			sprocket->state = RECV;
		}
		break;
	case TERM: /* terminate */
	default:
		RemoveWatch(sprocket, watcher);
		RemoveSprock(sprocket, &FIRSTSPROCK);
		break;
	}
	break;
}

		} while ((sprocket = NextSprock(sprocket)) != NULL);
	}
	/* unreachable */
}
