/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: ftppr.c,v 1.21 2006/03/10 19:25:48 vlad Exp $
*/

#include "proxy.h"

#define RETURN(xxx) { param->res = xxx; goto CLEANRET; }
#define BUFSIZE 2048

void * ftpprchild(void * data) {
#define param ((struct clientparam*)data)
 int i=0, res;
 unsigned char buf[BUFSIZE];
 unsigned char *se;
 int status = 0;
 int inbuf;
 int pasv = 0;
 SOCKET sc = INVALID_SOCKET, ss = INVALID_SOCKET, clidatasock = INVALID_SOCKET;
 SASIZETYPE sasize;
 char * req = NULL;
 struct linger lg;
 struct pollfd fds;

 param->ctrlsock = param->clisock;
 param->operation = CONNECT;
 lg.l_onoff = 1;
 lg.l_linger = conf.timeouts[STRING_L];;
 if(socksend(param->clisock, (unsigned char *)"220 Ready\r\n", 11, conf.timeouts[STRING_S])!=11) {RETURN (801);}
 for(;;){
	i = sockgetlinebuf(param, CLIENT, buf, BUFSIZE - 10, '\n', conf.timeouts[CONNECTION_S]);
	if(!i) {
		RETURN(0);
	}
	if(i<4) {RETURN(802);}
	buf[i] = 0;
	if ((se=(unsigned char *)strchr((char *)buf, '\r'))) *se = 0;
	if (req) myfree (req);
	req = NULL;

	if (!strncasecmp((char *)buf, "OPEN ", 5)){
		if(param->hostname) myfree(param->hostname);
		if(parsehostname((char *)buf+5, param, 21)){RETURN(803);}
		if(param->remsock != INVALID_SOCKET) {
			shutdown(param->remsock, SHUT_RDWR);
			closesocket(param->remsock);
			param->remsock = INVALID_SOCKET;
		}
		if((res = (*param->authfunc)(param))) {RETURN(res);}
		if(socksend(param->ctrlsock, (unsigned char *)"220 Ready\r\n", 11, conf.timeouts[STRING_S])!=11) {RETURN (801);}
		status = 1;
	}
	else if (!strncasecmp((char *)buf, "USER ", 5)){
		if(!param->hostname && param->remsock == INVALID_SOCKET) {
			if(parseconnusername((char *)buf +5, param, 0, 21)){RETURN(804);}
		}
		else if(parseusername((char *)buf + 5, param, 0)) {RETURN(805);}
		if(!status){
			if((res = (*param->authfunc)(param))) {RETURN(res);}
		}
		if(socksend(param->ctrlsock, (unsigned char *)"331 ok\r\n", 8, conf.timeouts[STRING_S])!=8) {RETURN (807);}
		status = 2;

	}
	else if (!strncasecmp((char *)buf, "PASS ", 5)){
		param->extpassword = (unsigned char *)mystrdup((char *)buf+5);
		inbuf = BUFSIZE;
		res = ftplogin(param, (char *)buf, &inbuf);
		param->res = res;
		if(inbuf && inbuf != BUFSIZE && socksend(param->ctrlsock, buf, inbuf, conf.timeouts[STRING_S])!=inbuf) {RETURN (807);}
		if(!res) status = 3;
		sprintf((char *)buf, "%.64s@%.128s%c%hu", param->extusername, param->hostname, (ntohs(param->sins.sin_port)==21)?0:':', ntohs(param->sins.sin_port));
		req = mystrdup((char *)buf);
	}
	else if (status >= 3 && (
			(!strncasecmp((char *)buf, "PASV", 4) && (pasv = 1)) ||
			(!strncasecmp((char *)buf, "PORT ", 5) && !(pasv = 0))
		)){
		if(sc != INVALID_SOCKET) {
			shutdown(sc, SHUT_RDWR);
			closesocket(sc);
			sc = INVALID_SOCKET;
		}
		if(ss != INVALID_SOCKET) {
			shutdown(ss, SHUT_RDWR);
			closesocket(ss);
			ss = INVALID_SOCKET;
		}
		if ((clidatasock=socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {RETURN(821);}
		sasize = sizeof(struct sockaddr_in);
		if (pasv) {
			if(getsockname(param->clisock, (struct sockaddr *)&param->sinc, &sasize)){RETURN(824);}
			param->sinc.sin_port = 0;
			
			if(bind(clidatasock, (struct sockaddr *)&param->sinc, sasize)){RETURN(822);}
			if(listen(clidatasock, 1)) {RETURN(823);}
			if(getsockname(clidatasock, (struct sockaddr *)&param->sinc, &sasize)){RETURN(824);}
			sprintf((char *)buf, "227 OK (%u,%u,%u,%u,%u,%u)\r\n",
				 (unsigned)(((unsigned char *)(&param->sinc.sin_addr.s_addr))[0]),
				 (unsigned)(((unsigned char *)(&param->sinc.sin_addr.s_addr))[1]),
				 (unsigned)(((unsigned char *)(&param->sinc.sin_addr.s_addr))[2]),
				 (unsigned)(((unsigned char *)(&param->sinc.sin_addr.s_addr))[3]),
				 (unsigned)(((unsigned char *)(&param->sinc.sin_port))[0]),
				 (unsigned)(((unsigned char *)(&param->sinc.sin_port))[1])
				);
			if(socksend(param->clisock, buf, (int)strlen((char *)buf), conf.timeouts[STRING_S])!=(int)strlen((char *)buf)) {RETURN (825);}
		}
		else {
			unsigned long b1, b2, b3, b4;
			unsigned short b5, b6;

			if(sscanf((char *)buf+5, "%lu,%lu,%lu,%lu,%hu,%hu", &b1, &b2, &b3, &b4, &b5, &b6)!=6) {RETURN(828);}
			param->sinc.sin_family = AF_INET;
			param->sinc.sin_port = htons((unsigned short)((b5<<8)^b6));
			param->sinc.sin_addr.s_addr = htonl((b1<<24)^(b2<<16)^(b3<<8)^b4);
			if(connect(clidatasock, (struct sockaddr *)&param->sinc, sasize)) {
				closesocket(clidatasock);
				clidatasock = INVALID_SOCKET;
				RETURN(826);
			}
			if(socksend(param->clisock, (unsigned char *)"200 OK\r\n", 8, conf.timeouts[STRING_S]) != 8) {RETURN (827);}
		}
		status = 4;
	}
	else if (status == 4 && (
		!strncasecmp((char *)buf, "RETR ", 5) ||
		!strncasecmp((char *)buf, "LIST", 4) ||
		!strncasecmp((char *)buf, "STOR ", 5)
	)){
		int arg = (buf[4] && buf[5])? 1:0;


		if(pasv){

			fds.fd = clidatasock;
			fds.events = POLLIN;

			res = poll (&fds, 1, conf.timeouts[STRING_L]*1000);
			if(res != 1) {RETURN(826);}
			sasize = sizeof(struct sockaddr_in);
			ss = accept(clidatasock, (struct sockaddr *)&param->sinc, &sasize);
			shutdown(clidatasock, SHUT_RDWR);
			closesocket(clidatasock);
			clidatasock = ss;
			ss = INVALID_SOCKET;
		}
		if(clidatasock == INVALID_SOCKET){RETURN(828);}
		req = mystrdup((char *)buf);
		buf[4] = 0;
		status = 3;
		ss = ftpcommand(param, buf, arg? buf+5 : NULL);
		if (ss == INVALID_SOCKET) {
			if(socksend(param->ctrlsock, (unsigned char *)"425 err\r\n", 9, conf.timeouts[STRING_S])!=9) {RETURN (831);}
			continue;
		}
		sc = param->remsock;
		param->remsock = ss;
		setsockopt(param->remsock, SOL_SOCKET, SO_LINGER, (unsigned char *)&lg, sizeof(lg));
		setsockopt(clidatasock, SOL_SOCKET, SO_LINGER, (unsigned char *)&lg, sizeof(lg));

		if(socksend(param->clisock, (unsigned char *)"101 data\r\n", 10, conf.timeouts[STRING_S]) != 10) {
			param->remsock = INVALID_SOCKET;
			RETURN (832);
		}
		param->clisock = clidatasock;
		res = sockmap(param, conf.timeouts[CONNECTION_S]);
		param->clisock = param->ctrlsock;
		if(param->remsock != INVALID_SOCKET) {
			shutdown (param->remsock, SHUT_RDWR);
			closesocket(param->remsock);
		}
		param->remsock = sc;
		sc = INVALID_SOCKET;
		ss = INVALID_SOCKET;
		if(clidatasock != INVALID_SOCKET){
			shutdown (clidatasock, SHUT_RDWR);
			closesocket(clidatasock);
			clidatasock = INVALID_SOCKET;
		}
		while((i = sockgetlinebuf(param, SERVER, buf, BUFSIZE, '\n', conf.timeouts[STRING_L])) > 3){
			param->statssrv += i;
			if(socksend(param->clisock, buf, i, conf.timeouts[STRING_S])!=i) {RETURN(833);}
			if(isnumber(*buf) && buf[3] != '-') break;
		}
		if(i < 3) {RETURN(834);}
	}
	else {
		if(status < 3) {
			if(socksend(param->remsock, (unsigned char *)"530 login\r\n", 11, conf.timeouts[STRING_S])!=1) {RETURN (810);}
			continue;
		}
		if(!strncasecmp((char *)buf, "QUIT", 4)) status = 5;
		if(!strncasecmp((char *)buf, "CWD ", 4)) req = mystrdup((char *)buf);
		i = strlen((char *)buf);
		buf[i++] = '\r';
		buf[i++] = '\n';
		if(socksend(param->remsock, buf, i, conf.timeouts[STRING_S])!=i) {RETURN (811);}
		param->statscli += i;
		while((i = sockgetlinebuf(param, SERVER, buf, BUFSIZE, '\n', conf.timeouts[STRING_L])) > 0){
			param->statssrv += i;
			if(socksend(param->clisock, buf, i, conf.timeouts[STRING_S])!=i) {RETURN (812);}
			if(i > 4 && isnumber(*buf) && buf[3] != '-') break;
		}
		if(status == 5) {RETURN (0);}
		if(i < 3) {RETURN (813);}
	}
	sasize = sizeof(struct sockaddr_in);
	if(getpeername(param->ctrlsock, (struct sockaddr *)&param->sinc, &sasize)){RETURN(819);}
	if(req && (param->statscli || param->statssrv)){
		(*param->logfunc)(param, (unsigned char *)req);
	}
 }

CLEANRET:

 if(sc != INVALID_SOCKET) {
	shutdown(sc, SHUT_RDWR);
	closesocket(sc);
 }
 if(ss != INVALID_SOCKET) {
	shutdown(ss, SHUT_RDWR);
	closesocket(ss);
 }
 if(clidatasock != INVALID_SOCKET) {
	shutdown(clidatasock, SHUT_RDWR);
	closesocket(clidatasock);
 }
 sasize = sizeof(struct sockaddr_in);
 getpeername(param->ctrlsock, (struct sockaddr *)&param->sinc, &sasize);
 if(param->res != 0 || param->statscli || param->statssrv ){
	(*param->logfunc)(param, (unsigned char *)((req && (param->res > 802))? req:NULL));
 }
 if(req) myfree(req);
 freeparam(param);
 return (NULL);
}

#ifdef WITHMAIN
struct proxydef childdef = {
	ftpprchild,
	21,
	0,
	S_FTPPR,
	""
};
#include "proxymain.c"
#endif
