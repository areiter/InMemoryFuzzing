/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: udppm.c,v 1.13 2006/03/10 19:25:53 vlad Exp $
*/

#include "proxy.h"

#ifndef PORTMAP
#define PORTMAP
#endif
#ifndef UDP
#define UDP
#endif
#define RETURN(xxx) { param->res = xxx; goto CLEANRET; }


void * udppmchild(void * data) {
#define param ((struct clientparam*)data)
 unsigned long ip;
 unsigned char *buf = NULL;
 int res, i;
#ifdef _WIN32
 SASIZETYPE size;
 unsigned long ul;
#endif
 ip=getip(param->target);
 if (! ip) {
	param->srvfds->events = POLLIN;
	RETURN (100);
 }
 if(!param->clibuf && (!(param->clibuf=myalloc(UDPBUFSIZE)) || !(param->clibufsize = UDPBUFSIZE))){
	param->srvfds->events = POLLIN;
	RETURN (21);
 }
 param->cliinbuf = param->clioffset = 0;
 i = sockrecvfrom(param->srvsock, &param->sinc, param->clibuf, param->clibufsize, 0);
 if(i<=0){
	param->srvfds->events = POLLIN;
	RETURN (214);
 }
 param->cliinbuf = i;
 param->statscli+=i;

#ifdef _WIN32
	if((param->clisock=socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == INVALID_SOCKET) {
		RETURN(818);
	}
	if(setsockopt(param->clisock, SOL_SOCKET, SO_REUSEADDR, (unsigned char *)&ul, sizeof(int))) {RETURN(820);};
	ioctlsocket(param->clisock, FIONBIO, &ul);
	size = sizeof(struct sockaddr_in);
	if(getsockname(param->srvsock, (struct sockaddr *)&param->sins, &size)) {RETURN(21);};
	if(bind(param->clisock,(struct sockaddr *)&param->sins,sizeof(struct sockaddr_in))) {
		RETURN(822);
	}
#else
	param->clisock = param->srvsock;
#endif

 param->sins.sin_family = AF_INET;
 param->sins.sin_port = htons(0);
 param->sins.sin_addr.s_addr = param->extip;
 if ((param->remsock=socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == INVALID_SOCKET) {RETURN (11);}
 if(bind(param->remsock,(struct sockaddr *)&param->sins,sizeof(param->sins))) {RETURN (12);}
#ifdef _WIN32
	ioctlsocket(param->remsock, FIONBIO, &ul);
#else
	fcntl(param->remsock,F_SETFL,O_NONBLOCK);
#endif
 param->sins.sin_addr.s_addr = ip;
 param->sins.sin_port = param->targetport;

 param->operation = UDPASSOC;
 if((res = (*param->authfunc)(param))) {RETURN(res);}
 if(param->singlepacket) {
	param->srvfds->events = POLLIN;
 }

 param->res = sockmap(param, conf.timeouts[(param->singlepacket)?SINGLEBYTE_L:STRING_L]);
 if(!param->singlepacket) {
	param->srvfds->events = POLLIN;
 }

CLEANRET:

 if(buf)myfree(buf);
 (*param->logfunc)(param, NULL);
#ifndef _WIN32
 param->clisock = INVALID_SOCKET;
#endif
 freeparam(param);
 return (NULL);
}

#ifdef WITHMAIN
struct proxydef childdef = {
	udppmchild,
	0,
	1,
	S_UDPPM,
	" -s single packet UDP service for request/reply (DNS-like) services\n"
};
#include "proxymain.c"
#endif
