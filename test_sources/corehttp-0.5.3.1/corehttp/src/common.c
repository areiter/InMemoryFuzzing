/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: common.c
*/

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

/*
	all changeable globals
*/
#define GLOBALS_DEFINED
#include "common.h"
int TIMEOUT, PORT[SETSIZE], BACKLOG;
char *ROOTDIR, *LOGFILE, *PAGE404, *DIRLIST, **DEFPAGE, **CGIBIN, **CGIEXT;
struct sprock_t *FIRSTSPROCK;
	
/*
	initializing them by parsing settings file
*/
void InitGlobals(int num, char **args) {
	char var[PATHSIZE], val[PATHSIZE], str[PATHSIZE];
	FILE* cfgfile;
	int porti, defpagei, cgibini, cgiexti;
	
	if (num != 2 || !strcmp(args[1], "--help")) {
		printf("usage: %s [--help] CfgFileAbsPath\n", args[0]);
		exit(EXIT_FAILURE);
	}
	if ((cfgfile = fopen(args[1], "r")) == NULL) {
		printf("unable to open config file!\n");
		exit(EXIT_FAILURE);
	}
	
	TIMEOUT = BACKLOG = -1;
	memset(PORT, -1, SETSIZE * sizeof(int));
	ROOTDIR = (char *)malloc(PATHSIZE * sizeof(char));
	LOGFILE = (char *)malloc(PATHSIZE * sizeof(char));
	PAGE404 = (char *)malloc(PATHSIZE * sizeof(char));
	DIRLIST = (char *)malloc(PATHSIZE * sizeof(char));
	DEFPAGE = (char **)malloc(SETSIZE * sizeof(char *));
	CGIEXT = (char **)malloc(SETSIZE * sizeof(char *));
	CGIBIN = (char **)malloc(SETSIZE * sizeof(char *));
	for (defpagei = 0; defpagei < SETSIZE; defpagei++) {
		DEFPAGE[defpagei] = (char *)malloc(PATHSIZE * sizeof(char));
		memset(DEFPAGE[defpagei], 0, sizeof(PATHSIZE * sizeof(char)));
		CGIEXT[defpagei] = (char *)malloc(PATHSIZE * sizeof(char));
		memset(CGIEXT[defpagei], 0, sizeof(PATHSIZE * sizeof(char)));
		CGIBIN[defpagei] = (char *)malloc(PATHSIZE * sizeof(char));
		memset(CGIBIN[defpagei], 0, sizeof(PATHSIZE * sizeof(char)));
	}
	ROOTDIR[0] = PAGE404[0] = LOGFILE[0] = '\0';
	porti = defpagei = cgiexti = cgibini = 0;
	FIRSTSPROCK = NULL;
	
	while (feof(cfgfile) == 0) {
		if (fscanf(cfgfile, "%[^\n]\n", str) < 1)
			continue;
		if (sscanf(str, "%s%*[ \t\n]%s", var, val) < 2)
			continue;
			
		/* integers */
		if (!strcmp(var, "TIMEOUT")) 
			TIMEOUT = (int)strtol(val, NULL, 0);
		else if (!strcmp(var, "PORT") && porti < SETSIZE) 
			PORT[porti++] = (int)strtol(val, NULL, 0);
		else if (!strcmp(var, "BACKLOG")) 
			BACKLOG = (int)strtol(val, NULL, 0);
		
		/* strings */
		sscanf(val, "\"%[^\"]\"", str);
		if (!strcmp(var, "ROOTDIR")) 
			strcpy(ROOTDIR, str);
		else if (!strcmp(var, "DEFPAGE") && defpagei < SETSIZE) 
			strcpy(DEFPAGE[defpagei++], str);
		else if (!strcmp(var, "PAGE404")) 
			strcpy(PAGE404, str);
		else if (!strcmp(var, "LOGFILE"))
			strcpy(LOGFILE, str);
		else if (!strcmp(var, "CGIEXT"))
			strcpy(CGIEXT[cgiexti++], str);
		else if (!strcmp(var, "CGIBIN"))
			strcpy(CGIBIN[cgibini++], str);
		else if (!strcmp(var, "DIRLIST"))
			strcpy(DIRLIST, str);
	}
	
	if (TIMEOUT == -1 || PORT[0] == -1 || BACKLOG == -1
		|| ROOTDIR[0] == '\0' || DEFPAGE[0][0] == '\0' 
		|| PAGE404[0] == '\0' || LOGFILE[0] == '\0'
		|| cgibini != cgiexti) {
		printf("bad config file!\n");
		exit(EXIT_FAILURE);
	}
	fclose(cfgfile);
}
