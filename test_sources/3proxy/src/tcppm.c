/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: tcppm.c,v 1.5 2006/03/10 19:25:53 vlad Exp $
*/

#include "proxy.h"

#ifndef PORTMAP
#define PORTMAP
#endif
#define RETURN(xxx) { param->res = xxx; goto CLEANRET; }

void * tcppmchild(void * data) {
#define param ((struct clientparam*)data)
 int res;

 param->hostname = param->target;
 param->sins.sin_port = param->targetport;
 param->operation = CONNECT;
 res = (*param->authfunc)(param);
 param->hostname = NULL;
 if(res) {RETURN(res);}
 RETURN (sockmap(param, conf.timeouts[CONNECTION_L]));
CLEANRET:
 
 (*param->logfunc)(param, NULL);
 freeparam(param);
 return (NULL);
}

#ifdef WITHMAIN
struct proxydef childdef = {
	tcppmchild,
	0,
	0,
	S_TCPPM,
	""
};
#include "proxymain.c"
#endif
