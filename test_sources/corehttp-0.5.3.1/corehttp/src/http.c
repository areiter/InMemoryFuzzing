/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: http.c
*/

#include "http.h"

/* create a file sprocket which processes file parentsprock->buffer */
struct sprock_t *HttpSprockMake(struct sprock_t *parentsprock) {
	struct sprock_t *sprocket;
	char req[PATHSIZE], url[PATHSIZE], status[PATHSIZE], temp[BUFSIZE],
		args[PATHSIZE], ctype[PATHSIZE], ext[PATHSIZE], cmd[PATHSIZE],
		asciinum[3] = {0}, *find;
	int i, c, start, end, pipefd[2];
	FILE *pipetoprog;
	
	cmd[0] = '\0';

	/* general sprocket stuff */
	if ((sprocket = (struct sprock_t *)
		malloc(sizeof(struct sprock_t))) == NULL) return NULL;
	memset(sprocket, 0, sizeof(*sprocket));
	sprocket->func = HTTP;
	sprocket->state = INIT;
	sprocket->lastio = -1;
	parentsprock->child = sprocket;
	sprocket->parent = parentsprock;
   
	/* parse the request in buffer */
	sscanf(parentsprock->buffer, "%[A-Za-z] %s%*[ \t\n]", req, url);
	strncpy(sprocket->parent->url, url, PATHSIZE); /* For logging */
	status[0] = '\0';
	sprintf(temp, "%s/%s", ROOTDIR, url);
	strncpy(url, temp, PATHSIZE);

	/* uppercase request */
	for (i = 0; req[i] != '\0'; i++)
		req[i] = toupper(req[i]);

	if (strcmp(req, "GET") && strcmp(req, "POST") && strcmp(req, "HEAD"))
		strcpy(status, "501 Not Implemented");

	/* de-escape it, turn %xx into a character */
	i = 0;
	while (url[i]) {
		if (url[i] == '+') url[i] = ' ';
		else if (url[i] == '%') {
			asciinum[0] = url[i + 1];
			asciinum[1] = url[i + 2];
			url[i] = strtol(asciinum, NULL, 16);
			c = i + 1;
			do { url[c] = url[c + 2]; } while (url[2 + c++]);
		}
		i++;
	}

	/* get rid of ../ etc */
	for ( ;; ) {
		find = strstr(url, "/..");
		if (find == NULL) break;
		if (find != NULL) {
			strcpy(temp, find + 4);
			strcpy(find, temp);
		}
	}
	for ( ;; ) {
		find = strstr(url, "../");
		if (find == NULL) break;
		if (find != NULL) {
			strcpy(temp, find + 4);
			strcpy(find, temp);
		}
	}

	/* get post/get arguements */
	find = strchr(url, '?');
	if (find == NULL) args[0] = '\0';
	else {
		*find = '\0';
		strcpy(args, find + 1);
	}
   
	/* TODO Note if we make HttpPostVars
		we might need to concatenate the variable strings. */

	/* inferred actions like 404 page, index.*, dir listing ... */      
	switch (HttpFileStat(url)) {
	default:
	case -1: /* not found */
		strcpy(status, "404 Not found");
		strcpy(url, PAGE404);
		break;
	case 0: /* is a directory */
		if (url[strlen(url) - 1] != '/') {
			find = strrchr(url, '/');
			sprintf(sprocket->buffer, 
				"HTTP/1.1 303 See Other\r\n"
				"Server: " VERSION "\r\n"
				"Location: %s/\r\n"
				"Connection: close\r\n\r\n",
				find);
			sprocket->fd = -1;
			return sprocket;
		}
		
		for (i = c = 0; DEFPAGE[i][0] != '\0' && i < SETSIZE; i++) {
			sprintf(temp, "%s/%s", url, DEFPAGE[i]);
			if (HttpFileStat(temp) == 1) {
				c = 1;
				break;
			}
		}
		if (c == 0)  {
			sprintf(cmd, "perl %s %s", DIRLIST, url);
			break;
		}
		/* fall through with index */
	case 1: /* is a file */
		strcpy(url, temp);
		break;
	}

	/* get file extension */
	end = strlen(url) - 1;
	/* strrcspn, bah */
	for (start = end; start >= 0 
		&& url[start] != '.' && url[start] != '/'; start--);
	if (start == 0 || url[start] == '/') ext[0] = '\0';
	else strncpy(ext, url + start + 1, end - start + 1);

	/* get content type */
	if (ext[0] == '\0') 
		strcpy(ctype, "text/plain; charset=iso-8859-1");
	else if (!strcmp(ext, "gif")) 
		strcpy(ctype, "image/gif");
	else if (!strcmp(ext, "jpg") || !strcmp(ext, "jpeg"))
	       	strcpy(ctype, "image/jpg");
	else if (!strcmp(ext, "png")) 
		strcpy(ctype, "image/png");
	else if (!strcmp(ext, "html") || !strcmp(ext, "htm"))
		strcpy(ctype, "text/html; charset=iso-8859-1");
	else if (!strcmp(ext, "txt")) 
		strcpy(ctype, "text/plain");
	else if (!strcmp(ext, "css"))
		strcpy(ctype, "text/css");
	else 
		strcpy(ctype, "application/x-octet-stream");
   
	/* if there isnt a command see if we need one for dynamic content */
	if (cmd[0] == '\0') 
		for (i = 0; CGIEXT[i][0] != '\0' && i < SETSIZE; i++)
			if (!strcmp(ext, CGIEXT[i])) {
				sprintf(cmd, "%s %s %s", CGIBIN[i], url, args);
				break;
			}
   
	/* if its dynamic content */
	if (!strcmp(req, "HEAD")) {
		sprocket->fd = -1;
	} else if (cmd[0] != '\0') {
		pipe(pipefd); /* make pipe then fork */
		c = fork();
		if (c > 0) { /* original, keep going */
			close(pipefd[1]); /* no need to write */
			sprocket->fd = pipefd[0];
			SetNonBlock(sprocket->fd);
			strcpy(ctype, "text/html");
		} else if (c == 0) { /* child, popen */
			close(pipefd[0]); /* no need to read */
			pipetoprog = popen(cmd, "r");
			/* fread should be non-blocking for this to exit fast
			when parent proc closes pipe */
			while ((i = fread(temp, 1, BUFSIZE, pipetoprog)) != 0
				&& write(pipefd[1], temp, i) > 0);
			pclose(pipetoprog);
			close(pipefd[1]);
			exit(EXIT_SUCCESS); /* exit after done */
		} else { /* failed */
			RemoveSprock(sprocket, &FIRSTSPROCK);
			return NULL;
		}

	/* else its static */
	} else if ((sprocket->fd = open(url, O_NONBLOCK | O_RDONLY)) < 0) {
		RemoveSprock(sprocket, &FIRSTSPROCK);
		return NULL;
	}
	
	/* put header */
	if (status[0] == '\0') strcpy(status, "200 OK");
	sprintf(sprocket->buffer, 
		"HTTP/1.1 %s\r\n"
		"Server: " VERSION "\r\n"
		"Content-Type: %s\r\n"
		"Connection: close\r\n\r\n",
		status, ctype);
	return sprocket;
}

/* return -1 error, 0 isdir, 1 isfile */
int HttpFileStat(char *filename) {
	struct stat filestat;
	if (stat(filename, &filestat) < 0) return -1;
	if (S_ISDIR(filestat.st_mode)) return 0;
	return 1;
}
