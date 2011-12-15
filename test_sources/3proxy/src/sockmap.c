/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: sockmap.c,v 1.36 2006/07/07 21:24:57 vlad Exp $
*/

#include "proxy.h"

#define BUFSIZE ((param->service == S_UDPPM)?UDPBUFSIZE:TCPBUFSIZE)


int sockmap(struct clientparam * param, int timeo){
 int res=0, sent=0, received=0;
 SASIZETYPE sasize;
 struct pollfd fds[2];
 int sleeptime = 0, stop = 0;
 unsigned minsize;
 unsigned bufsize;

 bufsize = (param->bufsize)? param->bufsize : BUFSIZE;

 minsize = (param->service == S_UDPPM)? bufsize - 1 : 0;

 fds[0].fd = param->clisock;
 fds[1].fd = param->remsock;

 if(!param->waitclient){
	if(!param->srvbuf && (!(param->srvbuf=myalloc(bufsize)) || !(param->srvbufsize = bufsize))){
		return (21);
	}
 }
 if(!param->waitserver){
	if(!param->clibuf && (!(param->clibuf=myalloc(bufsize)) || !(param->clibufsize = bufsize))){
		return (21);
	}
 }

 for (;!stop;){
	sasize = sizeof(struct sockaddr_in);
	if(param->maxtraf && param->statssrv >= param->maxtraf){
		return (10);
	}
	fds[0].events = fds[1].events = 0;
	if(param->srvinbuf > param->srvoffset && !param->waitclient) {
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "will send to client");
#endif
		fds[0].events |= POLLOUT;
	}
	if((param->srvbufsize - param->srvinbuf) > minsize && !param->waitclient) {
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "Will recv from server");
#endif
		fds[1].events |= POLLIN;
	}

	if(param->cliinbuf > param->clioffset && !param->waitserver) {
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "Will send to server");
#endif
		fds[1].events |= POLLOUT;
	}
    	if((param->clibufsize - param->cliinbuf) > minsize  && !param->waitserver &&(!param->singlepacket || param->service != S_UDPPM)) {
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "Will recv from client");
#endif
		fds[0].events |= POLLIN;
	}
	res = poll(fds, 2, timeo*1000);
	if(res < 0 && (errno == EAGAIN || errno == EINTR)) continue;
	if(res < 1){
		return res?91:92;
	}
	if( (fds[0].revents & (POLLERR|POLLHUP|POLLNVAL )) ||
	    (fds[1].revents & (POLLERR|POLLHUP|POLLNVAL )) )
		return 90;
	if((fds[0].revents & POLLOUT)){
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "send to client");
#endif
		if(param->bandlimfunc) {
			sleeptime = (*param->bandlimfunc)(param, param->srvinbuf - param->srvoffset, 0);
		}
		res = sendto(param->clisock, param->srvbuf + param->srvoffset, param->srvinbuf - param->srvoffset, 0, (struct sockaddr*)&param->sinc, sasize);
		if(res < 0) {
			if(errno != EAGAIN) return 96;
			continue;
		}
		param->srvoffset += res;
		param->statssrv += res;
		received += res;
		if(param->srvoffset == param->srvinbuf) param->srvoffset = param->srvinbuf = 0;
		if(param->waitserver && param->waitserver<= received){
			return (98);
		}
		if(param->service == S_UDPPM && param->singlepacket) {
			stop = 1;
		}
	}
	if((fds[1].revents & POLLOUT)){
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "send to server");
#endif
		if(param->bandlimfunc) {
			int sl1;

			sl1 = (*param->bandlimfunc)(param, 0, param->cliinbuf - param->clioffset);
			if(sl1 > sleeptime) sleeptime = sl1;
		}
		res = sendto(param->remsock, param->clibuf + param->clioffset, param->cliinbuf - param->clioffset, 0, (struct sockaddr*)&param->sins, sasize);
		if(res < 0) {
			if(errno != EAGAIN) return 97;
			continue;
		}
		param->clioffset += res;
		sent += res;
		param->statscli += res;
		if(param->clioffset == param->cliinbuf) param->clioffset = param->cliinbuf = 0;
		if(param->waitclient && param->waitclient<= sent) {
			return (99);
		}
	}
	if ((fds[0].revents & POLLIN)) {
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "recv from client");
#endif
		res = recvfrom(param->clisock, param->clibuf + param->cliinbuf, param->clibufsize - param->cliinbuf, 0, (struct sockaddr *)&param->sinc, &sasize);
		if (res==0) {
			shutdown(param->clisock, SHUT_RDWR);
			closesocket(param->clisock);
			fds[0].fd = param->clisock = INVALID_SOCKET;
			stop = 1;
		}
		else {
			if (res < 0 && errno != EAGAIN) {
				return (94);
			}
			param->cliinbuf += res;
		}
	}
	if (!stop && (fds[1].revents & POLLIN)) {
		struct sockaddr_in sin;
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "recv from server");
#endif

		sasize = sizeof(sin);
		res = recvfrom(param->remsock, param->srvbuf + param->srvinbuf, param->srvbufsize - param->srvinbuf, 0, (struct sockaddr *)&sin, &sasize);
		if (res==0) {
			shutdown(param->remsock, SHUT_RDWR);
			closesocket(param->remsock);
			fds[1].fd = param->remsock = INVALID_SOCKET;
			stop = 2;
		}
		else {
			if (res < 0 && errno != EAGAIN) {
				return (93);
			}
			param->srvinbuf += res;
		}
	}

	if(sleeptime > 0) {
		if(sleeptime > (timeo * 1000)){return (95);}
		usleep(sleeptime * SLEEPTIME);
		sleeptime = 0;
	}
 }

#if DEBUGLEVEL > 2
(*param->logfunc)(param, "finished with mapping");
#endif
 while(!param->waitclient && param->srvinbuf > param->srvoffset && param->clisock != INVALID_SOCKET){
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "flushing buffer to client");
#endif
	res = socksendto(param->clisock, &param->sinc, param->srvbuf + param->srvoffset, param->srvinbuf - param->srvoffset, conf.timeouts[STRING_S] * 1000);
	if(res > 0){
		param->srvoffset += res;
		param->statssrv += res;
		if(param->srvoffset == param->srvinbuf) param->srvoffset = param->srvinbuf = 0;
	}
	else break;
 } 
 while(!param->waitserver && param->cliinbuf > param->clioffset && param->remsock != INVALID_SOCKET){
#if DEBUGLEVEL > 2
(*param->logfunc)(param, "flushing buffer to server");
#endif
	res = socksendto(param->remsock, &param->sins, param->clibuf + param->clioffset, param->cliinbuf - param->clioffset, conf.timeouts[STRING_S] * 1000);
	if(res > 0){
		param->clioffset += res;
		param->statscli += res;
		if(param->cliinbuf == param->clioffset) param->cliinbuf = param->clioffset = 0;
	}
	else break;
 } 
 return 0;
}
