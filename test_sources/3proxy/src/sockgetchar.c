/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement
*/

#include "proxy.h"
#define BUFSIZE ((param->service == S_UDPPM)?UDPBUFSIZE:TCPBUFSIZE)


int socksend(SOCKET sock, unsigned char * buf, int bufsize, int to){
 int sent = 0;
 int res;
 struct pollfd fds;

 fds.fd = sock;
 fds.events = POLLOUT;
 do {
	if(timetoexit) return 0;
	res = poll(&fds, 1, to*1000);
	if(res < 0 && (errno == EAGAIN || errno == EINTR)) continue;
	if(res < 1) break;
	res = send(sock, buf + sent, bufsize - sent, 0);
	if(res < 0) {
		if(errno == EAGAIN || errno == EINTR) continue;
		break;
	}
	sent += res;
 } while (sent < bufsize);
 return sent;
}


int socksendto(SOCKET sock, struct sockaddr_in * sin, unsigned char * buf, int bufsize, int to){
 int sent = 0;
 int res;
 struct pollfd fds;

 fds.fd = sock;
 do {
	if(timetoexit) return 0;
	fds.events = POLLOUT;
 	res = poll(&fds, 1, to);
	if(res < 0 && (errno == EAGAIN || errno == EINTR)) continue;
	if(res < 1) break;
	res = sendto(sock, buf + sent, bufsize - sent, 0,  (struct sockaddr *)sin, sizeof(struct sockaddr_in));
	if(res < 0) {
		if(errno !=  EAGAIN) break;
		continue;
	}
	sent += res;
 } while (sent < bufsize);
 return sent;
}

int sockrecvfrom(SOCKET sock, struct sockaddr_in * sin, unsigned char * buf, int bufsize, int to){
	struct pollfd fds;
	SASIZETYPE sasize;
	int res;

	fds.fd = sock;
	fds.events = POLLIN;
	if(timetoexit) return EOF;
	if (poll(&fds, 1, to)<1) return 0;
	sasize = sizeof(struct sockaddr_in);
	do {
		res = recvfrom(sock, buf, bufsize, 0, (struct sockaddr *)sin, &sasize);
	} while (res < 0 && (errno == EAGAIN || errno == EINTR));
	return res;
}

int sockgetcharcli(struct clientparam * param, int timeosec, int timeousec){
	int len;

	if(!param->clibuf){
		if(!(param->clibuf = myalloc(BUFSIZE))) return 0;
		param->clibufsize = BUFSIZE;
		param->clioffset = param->cliinbuf = 0;
	}
	if(param->cliinbuf && param->clioffset < param->cliinbuf){
		return (int)param->clibuf[param->clioffset++];
	}
	param->clioffset = param->cliinbuf = 0;
	if ((len = sockrecvfrom(param->clisock, &param->sinc, param->clibuf, param->clibufsize, timeosec*1000 + timeousec))<=0) return EOF;
	param->cliinbuf = len;
	param->clioffset = 1;
	return (int)*param->clibuf;
}

int sockgetcharsrv(struct clientparam * param, int timeosec, int timeousec){
	int len;

	if(!param->srvbuf){
		if(!(param->srvbuf = myalloc(BUFSIZE))) return 0;
		param->srvbufsize = BUFSIZE;
		param->srvoffset = param->srvinbuf = 0;
	}
	if(param->srvinbuf && param->srvoffset < param->srvinbuf){
		return (int)param->srvbuf[param->srvoffset++];
	}
	param->srvoffset = param->srvinbuf = 0;
	if ((len = sockrecvfrom(param->remsock, &param->sins, param->srvbuf, param->srvbufsize, timeosec*1000 + timeousec))<=0) return EOF;
	param->srvinbuf = len;
	param->srvoffset = 1;
	return (int)*param->srvbuf;
}

int sockgetlinebuf(struct clientparam * param, DIRECTION which, unsigned char * buf, int bufsize, int delim, int to){
 int c;
 int i=0, tos, tou;
 if(bufsize<2) return 0;
 c = (which)?sockgetcharsrv(param, to, 0):sockgetcharcli(param, to, 0);
 if (c == EOF) {
	return 0;
 }
 tos = to/16;
 tou = ((to * 1000) / bufsize)%1000;
 do {
	buf[i++] = c;
	if(delim != EOF && c == delim) break;
 }while(i < bufsize && (c = (which)?sockgetcharsrv(param, to, 0):sockgetcharcli(param, to, 0)) != EOF);
 return i;
}

