/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: handler.c
*/

#include "handler.h"

/* signal handler: everything is signal safe since we just exit .. */
void HandleExit(int signum) {
	char buffer[BUFSIZE], line[PATHSIZE];
	strcpy(buffer, "terminating... dumping existing sprockets ...\n");
	
	while (FIRSTSPROCK != NULL) {
		if (FIRSTSPROCK->func == CLNT)
			sprintf(line, "%-7d CLNT %s requesting %s\n", 
				FIRSTSPROCK->fd, 
				FIRSTSPROCK->ipaddr, FIRSTSPROCK->url);
		else if (FIRSTSPROCK->func == SERV)
			sprintf(line, "%-7d SERV\n", FIRSTSPROCK->fd);
		else if (FIRSTSPROCK->func == HTTP)
			sprintf(line, "%-7d HTTP\n", FIRSTSPROCK->fd); 
		strcat(buffer, line);
		RemoveSprock(FIRSTSPROCK, &FIRSTSPROCK);
	} 
		
	HandleLog(buffer);
	exit(EXIT_SUCCESS);
}

/* signal handler: clean up all zombie processes */
void HandleChildProcs(int signum) {
	while (waitpid(-1, NULL, WNOHANG) > 0);
}

/* die with error */
void HandleError(char *errstr) {
	fprintf(logfile, "%ld FATAL: %s\n", (long)time(NULL), errstr);
	HandleExit(-1);
}

/* live with error */
void HandleProblem(char *errstr) {
	char buffer[PATHSIZE] = "PROBLEM: "; /* strlen = 9 */
	strncat(buffer, errstr, PATHSIZE - 9);
	HandleLog(buffer);
}

/* log something good */
void HandleLog(char *str) {
	fprintf(logfile, "%ld %s\n", (long)time(NULL), str);
	fflush(logfile);
}

/* initialize handlers */
int HandlerInit(void) {
	struct sigaction handler;

	if ((logfile = fopen(LOGFILE, "a")) == NULL) return -1;
		
	if (sigemptyset(&handler.sa_mask) < 0) return -1;
	handler.sa_flags = 0;
	
	handler.sa_handler = SIG_IGN;
	if (sigaction(SIGPIPE, &handler, NULL) < 0) return -1;
	
	handler.sa_handler = HandleChildProcs;
	if (sigaction(SIGCHLD, &handler, NULL) < 0) return -1;

	handler.sa_handler = HandleExit;
	if (sigaction(SIGINT, &handler, NULL) < 0) return -1;
	if (sigaction(SIGTERM, &handler, NULL) < 0) return -1;
	if (sigaction(SIGQUIT, &handler, NULL) < 0) return -1;
	
	return 0;
}
