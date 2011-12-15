/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: proxymain.c,v 1.36 2006/03/10 19:25:51 vlad Exp $
*/

#include "proxy.h"
#ifndef MODULEMAINFUNC
#define MODULEMAINFUNC main
#define STDMAIN
#else
 extern int linenum;
 extern int haveerror;
#endif

int MODULEMAINFUNC (int argc, char** argv){

 SOCKET sock = INVALID_SOCKET;
 int i=0;
 SASIZETYPE size;
 pthread_t thread;
 struct clientparam defparam;
 int demon=0;
 struct clientparam * newparam;
 char *s;
 int error = 0;
 unsigned sleeptime;
 struct extparam myconf;
 unsigned char buf[256];
 struct pollfd fds;
 int opt = 1;
 PROXYFUNC pf;
 FILE *fp = NULL;
 int maxchild;
 int silent = 0;
 int nlog = 5000;
 char loghelp[] =
#ifdef STDMAIN
	" -d go to background (daemon)\n"
#endif
	" -fFORMAT logging format (see documentation)\n"
	" -l log to stderr\n"
	" -lFILENAME log to FILENAME\n"
	" -bBUFSIZE size of network buffer (default 4096 for TCP, 16384 for UDP)\n"
#ifndef _WIN32
	" -l@IDENT log to syslog IDENT\n"
#endif
	" -t be silenT (do not log service start/stop)\n"
	" -iIP ip address or internal interface (clients are expected to connect)\n"
	" -eIP ip address or external interface (outgoing connection will have this)\n";

 int childcount=0;
 pthread_mutex_t counter_mutex;


#ifdef _WIN32
 unsigned long ul;
#endif
#ifndef UDP
 int new_sock = INVALID_SOCKET;
 struct linger lg;
#endif
#ifdef _WIN32
 HANDLE h;
#endif
#ifdef STDMAIN
#ifdef _WIN32
 WSADATA wd;
 WSAStartup(MAKEWORD( 1, 1 ), &wd);
#else
 signal(SIGPIPE, SIG_IGN);

 pthread_attr_init(&pa);
 pthread_attr_setstacksize(&pa,PTHREAD_STACK_MIN + 16384);
 pthread_attr_setdetachstate(&pa,PTHREAD_CREATE_DETACHED);
#endif
#endif


 pf = childdef.pf;
 memcpy(&myconf, &conf, sizeof(myconf));
 memset(&defparam, 0, sizeof(struct clientparam));
 defparam.version = paused;
 defparam.childcount = &childcount;
 defparam.logfunc = myconf.logfunc;
 defparam.authfunc = myconf.authfunc;
 defparam.aclnum = myconf.aclnum;
 defparam.service = childdef.service;
 defparam.usentlm = 1;
 defparam.stdlog = NULL;
 defparam.time_start = time(NULL);
 maxchild = myconf.maxchild;

#ifndef STDMAIN
 if(!conf.services){
	conf.services = &defparam;
 }
 else {
	defparam.next = conf.services;
	conf.services = conf.services->prev = &defparam;
 }
#endif

 pthread_mutex_init(defparam.counter_mutex = &counter_mutex, NULL);

 for (i=1; i<argc; i++) {
	if(*argv[i]=='-') {
		switch(argv[i][1]) {
		 case 'd': 
			if(!demon)daemonize();
			demon = 1;
			break;
		 case 'l':
			defparam.logfunc = logstdout;
			defparam.logtarget = (unsigned char*)mystrdup(argv[i]);
			if(argv[i][2]) {
#ifdef STDMAIN
				if(argv[i][2]=='@'){
#ifndef _WIN32
					openlog(argv[i]+3, LOG_PID, LOG_DAEMON);
					defparam.logfunc = logsyslog;
#endif
				}
				else 
#endif
				{
					fp = fopen(argv[i] + 2, "a");
					if (fp) {
						defparam.stdlog = fp;
						fseek(fp, 0L, SEEK_END);
					}
				}

			}
			break;
		 case 'i':
			myconf.intip = getip((unsigned char *)argv[i]+2);
			break;
		 case 'e':
			myconf.extip = getip((unsigned char *)argv[i]+2);
			break;
		 case 'p':
			myconf.intport = atoi(argv[i]+2);
			break;
		 case 'b':
			myconf.bufsize = atoi(argv[i]+2);
			break;
		 case 'n':
			defparam.usentlm = 0;
			break;
		 case 'f':
			defparam.logformat = (unsigned char *)argv[i] + 2;
			break;
		 case 't':
			silent = 1;
			break;
		case 's':
		case 'a':
			myconf.singlepacket = 1 + atoi(argv[i]+2);
			break;
		 default:
			error = 1;
			break;
		}
	}
	else break;
 }


#ifndef STDMAIN
 if(childdef.port) {
#endif
#ifndef PORTMAP
	if (error || i!=argc) {
#ifndef STDMAIN
		haveerror = 1;
		conf.threadinit = 0;
#endif
		fprintf(stderr, "Usage: %s options\n"
			"Available options are:\n"
			"%s"
			" -pPORT - service port to accept connections\n"
			"%s"
			"\tExample: %s -i127.0.0.1\n\n"
			"%s", 
			argv[0], loghelp, childdef.helpmessage, argv[0],
#ifdef STDMAIN
			copyright
#else
			""
#endif
		);

		return (1);
	}
#endif
#ifndef STDMAIN
 }
 else {
#endif
#ifndef NOPORTMAP
	if (error || argc != i+3 || *argv[i]=='-'|| (myconf.intport = atoi(argv[i]))==0 || (defparam.targetport = htons((unsigned short)atoi(argv[i+2])))==0) {
#ifndef STDMAIN
		haveerror = 1;
		conf.threadinit = 0;
#endif
		fprintf(stderr, "Usage: %s options"
			" [-e<external_ip>] <port_to_bind>"
			" <target_hostname> <target_port>\n"
			"Available options are:\n"
			"%s"
			"%s"
			"\tExample: %s -d -i127.0.0.1 6666 serv.somehost.ru 6666\n\n"
			"%s", 
			argv[0], loghelp, childdef.helpmessage, argv[0],
#ifdef STDMAIN
			copyright
#else
			""
#endif
		);
		return (1);
	}
	defparam.target = (unsigned char *)mystrdup(argv[i+1]);
#endif
#ifndef STDMAIN
 }
#endif
 if(!defparam.logformat){
	defparam.logformat = myconf.logformat;
 }
 if(defparam.logformat){
	if(*defparam.logformat == '-' && (s = strchr((char *)defparam.logformat + 1, '+')) && s[1]){
		*s = 0;
		defparam.nonprintable = (unsigned char *)mystrdup((char *)defparam.logformat + 1);
		defparam.replace = s[1];
		defparam.logformat = (unsigned char *)mystrdup(s + 2);
		*s = '+';
	}
	else defparam.logformat = (unsigned char *)mystrdup((char *)defparam.logformat);
 }
 defparam.sinc.sin_addr.s_addr = defparam.intip = myconf.intip;
 if(!myconf.intport)myconf.intport = childdef.port;
 defparam.sinc.sin_port = defparam.intport = htons(myconf.intport);
 defparam.sins.sin_addr.s_addr = defparam.extip = myconf.extip;
 defparam.sins.sin_port = defparam.extport = htons(myconf.extport);
 defparam.remsock = defparam.clisock = defparam.ctrlsock = INVALID_SOCKET;
 defparam.sins.sin_family = defparam.sinc.sin_family = AF_INET;
 defparam.singlepacket = myconf.singlepacket;
 defparam.bufsize = myconf.bufsize;
#ifndef STDMAIN
 conf.threadinit = 0;
#endif

#ifndef UDP
 lg.l_onoff = 1;
 lg.l_linger = conf.timeouts[STRING_L];
 if( (sock=socket(PF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {
#else
 if( (sock=socket(PF_INET, SOCK_DGRAM, IPPROTO_UDP)) == INVALID_SOCKET) { 
#endif
	perror("socket()");
	return -2;
 }
#ifdef _WIN32
	ioctlsocket(sock, FIONBIO, &ul);
#else
	fcntl(sock,F_SETFL,O_NONBLOCK);
#endif
 defparam.srvsock = sock;
 if(setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (unsigned char *)&opt, sizeof(int)))perror("setsockopt()");

 size = sizeof(defparam.sinc);
 for(sleeptime = SLEEPTIME * 100; bind(sock, (struct sockaddr*)&defparam.sinc, size)==-1; usleep(sleeptime)) {
	sprintf((char *)buf, "bind(): %s", strerror(errno));
	(*defparam.logfunc)(&defparam, buf);	
	sleeptime = (sleeptime<<1);	
	if(!sleeptime) {
		closesocket(sock);
		return -3;
	}
 }
#ifndef UDP
 if(listen (sock, 1 + (maxchild>>4))==-1) {
	sprintf((char *)buf, "listen(): %s", strerror(errno));
	(*defparam.logfunc)(&defparam, buf);
	return -4;
 }
#else
 defparam.srvfds = &fds;
 defparam.clisock = sock;
#endif

 defparam.threadid = (unsigned)pthread_self();
 if(!silent){
	sprintf((char *)buf, "Accepting connections [%u/%u]", (unsigned)getpid(), (unsigned)pthread_self());
	(*defparam.logfunc)(&defparam, buf);
 }
 defparam.sinc.sin_addr.s_addr = defparam.sins.sin_addr.s_addr = 0;
 defparam.sinc.sin_port = defparam.sins.sin_port = 0;

 fds.fd = sock;
 fds.events = POLLIN;
 

 for (;;) {
	for(;;){
		while((paused == defparam.version && childcount >= myconf.maxchild)){
			nlog++;			
			if(nlog > 5000) {
				sprintf((char *)buf, "Warning: too many connected clients (%d/%d)", childcount, myconf.maxchild);
				(*defparam.logfunc)(&defparam, buf);
				nlog = 0;
			}
			usleep(SLEEPTIME);
		}
		if (paused != defparam.version) break;
		if (fds.events & POLLIN) {
			error = poll(&fds, 1, 1000);
		}
		else {
			usleep(SLEEPTIME);
			continue;
		}
		if (error >= 1) break;
		if (error == 0) continue;
		sprintf((char *)buf, "poll(): %s/%d", strerror(errno), errno);
		(*defparam.logfunc)(&defparam, buf);
		if(errno != EAGAIN) break;
		continue;
	}
	if(paused != defparam.version) break;
#ifndef UDP
	size = sizeof(defparam.sinc);
	new_sock = accept(sock, (struct sockaddr*)&defparam.sinc, &size);
	if(new_sock == INVALID_SOCKET){
		sprintf((char *)buf, "accept(): %s", strerror(errno));
		(*defparam.logfunc)(&defparam, buf);
		continue;
	}
#ifdef _WIN32
	ioctlsocket(new_sock, FIONBIO, &ul);
#else
	fcntl(new_sock,F_SETFL,O_NONBLOCK);
#endif

	setsockopt(new_sock, SOL_SOCKET, SO_LINGER, (unsigned char *)&lg, sizeof(lg));
	setsockopt(new_sock, SOL_SOCKET, SO_OOBINLINE, (unsigned char *)&opt, sizeof(int));
#else

	fds.events = 0;
#endif
	if(! (newparam = myalloc (sizeof(defparam)))){
#ifndef UDP
		closesocket(new_sock);
#endif
		defparam.res = 21;
		(*defparam.logfunc)(&defparam, (unsigned char *)"Memory Allocation Failed");
		usleep(SLEEPTIME);
		continue;
	};
	memcpy(newparam, &defparam, sizeof(defparam));
	clearstat(newparam);
#ifndef UDP
	newparam->clisock = new_sock;
#endif
	newparam->child = newparam->prev = newparam->next = NULL;
	newparam->parent = &defparam;
	pthread_mutex_lock(&counter_mutex);
	if(!defparam.child)defparam.child = newparam;
	else {
		newparam->next = defparam.child;
		defparam.child = defparam.child->prev = newparam;
	}
#ifdef _WIN32
	h = CreateThread((LPSECURITY_ATTRIBUTES )NULL, 16384, (LPTHREAD_START_ROUTINE)pf, (void *) newparam, (DWORD)0, &thread);
	newparam->threadid = (unsigned)thread;
	if (h) {
		childcount++;
		CloseHandle(h);
	}
	else {
		myfree(newparam);
	}
#else
	if((error = pthread_create(&thread, &pa, pf, (void *)newparam))){
		sprintf((char *)buf, "pthread_create(): %s", strerror(error));
		(*defparam.logfunc)(&defparam, buf);
		freeparam(newparam);
	}
	else {
		childcount++;
		newparam->threadid = (unsigned)thread;
	}
#endif
	pthread_mutex_unlock(&counter_mutex);
	memset(&defparam.sinc, 0, sizeof(defparam.sinc));
#ifdef UDP
	while(!fds.events)usleep(SLEEPTIME);
#endif
 }
 if(defparam.srvsock != INVALID_SOCKET) closesocket(defparam.srvsock);
 if(!silent) defparam.logfunc(&defparam, (unsigned char *)"Exiting thread");
 defparam.service = S_ZOMBIE;
 while(defparam.child) usleep(SLEEPTIME * 100);
 defparam.threadid = 0;
 if(fp) fclose(fp);
 if(defparam.target) myfree(defparam.target);
 if(defparam.logtarget) myfree(defparam.logtarget);
 if(defparam.logformat) myfree(defparam.logformat);
 if(defparam.nonprintable) myfree(defparam.nonprintable);
#ifndef STDMAIN
 if(defparam.next)defparam.next->prev = defparam.prev;
 if(defparam.prev)defparam.prev->next = defparam.next;
 else conf.services = defparam.next;
#endif
 pthread_mutex_destroy(&counter_mutex);
 return 0;
}
