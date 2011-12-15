/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: common.c,v 1.42 2006/03/10 19:36:22 vlad Exp $
*/



#include "proxy.h"


char * copyright = COPYRIGHT;

int paused = 0;
int demon = 0;

int randomizer = 1;
int timetoexit = 0;


#ifndef _WIN32
 pthread_attr_t pa;
#endif

unsigned char **stringtable = NULL;

int myinet_ntoa(struct in_addr in, char * buf){
 unsigned u = ntohl(in.s_addr);
 return sprintf(buf, "%u.%u.%u.%u", 
	((u&0xFF000000)>>24), 
	((u&0x00FF0000)>>16),
	((u&0x0000FF00)>>8),
	((u&0x000000FF)));
}

struct extparam conf = {
	{1, 5, 30, 60, 180, 1800, 15, 60, 0, 0},
	{
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
		NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
	},
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	0,
	-1,
	0,
	0,
	0,
	NULL, NULL,
	NONE,
	NONE,
	NULL,
	0,
	NULL,
	0, 100, INADDR_ANY, INADDR_ANY, 0, 0, 0, 
	0, NULL,
	doconnect, lognone, NULL,
	NULL,
	0,
	NULL
};

int myrand(void * entropy, int len){
	int i;
	unsigned short init;

	init = randomizer;
	for(i=0; i < len/2; i++){
		init ^= ((unsigned short *)entropy)[i];
	}
	srand(init);
	randomizer = rand();
	return rand();
	
}


#ifndef WITH_POLL
int  mypoll(struct mypollfd *fds, unsigned int nfds, int timeout){
	fd_set readfd;
	fd_set writefd;
	fd_set oobfd;
	struct timeval tv;
	unsigned i;
	int num;
	SOCKET maxfd = 0;

	tv.tv_sec = timeout/1000;
	tv.tv_usec = (timeout%1000);
	FD_ZERO(&readfd);
	FD_ZERO(&writefd);
	FD_ZERO(&oobfd);
	for(i=0; i<nfds; i++){
		if((fds[i].events&POLLIN))FD_SET(fds[i].fd, &readfd);
		if((fds[i].events&POLLOUT))FD_SET(fds[i].fd, &writefd);
		if((fds[i].events&POLLPRI))FD_SET(fds[i].fd, &oobfd);
		fds[i].revents = 0;
		if(fds[i].fd > maxfd) maxfd = fds[i].fd;
	}
	if((num = select(maxfd+1, &readfd, &writefd, &oobfd, &tv)) < 1) return num;
	for(i=0; i<nfds; i++){
		if(FD_ISSET(fds[i].fd, &readfd)) fds[i].revents |= POLLIN;
		if(FD_ISSET(fds[i].fd, &writefd)) fds[i].revents |= POLLOUT;
		if(FD_ISSET(fds[i].fd, &oobfd)) fds[i].revents |= POLLPRI;
	}
	return num;
}
#endif

int parsehostname(char *hostname, struct clientparam *param, unsigned short port){
	char *sp;

	if(!hostname || !*hostname)return 1;
	if ( (sp = strchr(hostname, ':')) ) *sp = 0;
	if(param->hostname) myfree(param->hostname);
	param->hostname = (unsigned char *)mystrdup(hostname);
	if(sp){
		port = atoi(sp+1);
		*sp = ':';
	}
	param->sins.sin_port = htons(port);
	return 0;
}

int parseusername(char *username, struct clientparam *param, int extpasswd){
	char *sb = NULL, *se = NULL, *sp = NULL;

	if(!username || !*username) return 1;
	if((sb = strchr(username, ':')) && (se = strchr(sb + 1, ':')) && (!extpasswd || (sp = strchr(se + 1, ':')))){
		*sb = 0;
		*se = 0;
		if(sp) *sp = 0;
		if(*(sb+1)) {
			if(param->password) myfree(param->password);
			param->password = (unsigned char *)mystrdup(sb+1);
		}
		if(*username) {
			if(param->username) myfree(param->username);
			param->username = (unsigned char *)mystrdup(username);
		}
		username = se+1;
	 }
	if(extpasswd){
		if(!sp) sp = strchr(username, ':');
		if(sp){
			*sp = 0;
			if(param->extpassword) myfree(param->extpassword);
			param->extpassword = (unsigned char *) mystrdup(sp+1);
		}
	}
	if(param->extusername) myfree(param->extusername);
	param->extusername = (unsigned char *)mystrdup(username);
	if(sb) *sb = ':';
	if(se) *se = ':';
	if(sp) *sp = ':';
	return 0;
}

int parseconnusername(char *username, struct clientparam *param, int extpasswd, unsigned short port){
	char *sb, *se;
	if(!username || !*username) return 1;
        if ((sb=strchr(username, '@')) == NULL) return 2;
	while ((se=strchr(sb+1, '@')))sb=se;
	*(sb) = 0;
	if(parseusername(username, param, extpasswd)) return 3;
	*(sb) = '@';
	if(parsehostname(sb+1, param, port)) return 4;
	return 0;
}

char* demanddialprog = NULL;

void clearstat(struct clientparam * param) {

#ifdef _WIN32
	struct timeb tb;

	ftime(&tb);
	param->time_start = (time_t)tb.time;
	param->msec_start = (unsigned)tb.millitm;

#else
	struct timeval tv;
	struct timezone tz;
	gettimeofday(&tv, &tz);

	param->time_start = (time_t)tv.tv_sec;
	param->msec_start = (tv.tv_usec / 1000);
#endif
	if(param->parent){
		param->parent->statscli += param->statscli;
		param->parent->statssrv += param->statssrv;
	}
	param->statscli = param->statssrv = 0;
}


int dobuf(struct clientparam * param, unsigned char * buf, const unsigned char *s, const unsigned char * doublec){
	struct tm* tm;
	int i, j;
	time_t sec;
	unsigned msec;
	unsigned delay;
	int timezone;
	int len;
	char * format;

#ifdef _WIN32
	struct timeb tb;

	ftime(&tb);
	sec = (time_t)tb.time;
	msec = (unsigned)tb.millitm;
	timezone = (int) tb.timezone;

#else
	struct timeval tv;
	struct timezone tz;
	gettimeofday(&tv, &tz);

	sec = (time_t)tv.tv_sec;
	msec = tv.tv_usec / 1000;
	timezone = tz.tz_minuteswest;
#endif

	if(!param) return 0;
	if(param->trafcountfunc)(*param->trafcountfunc)(param);
	delay = param->time_start?((unsigned) ((sec - param->time_start))*1000 + msec) - param->msec_start : 0;
	format = (char *)param->logformat;
	if(!format) format = "G%y%m%d%H%M%S.%. %p %E %U %C:%c %R:%r %O %I %h %T";
	tm = (*format == 'G' || *format == 'g')?
		gmtime(&sec) : localtime(&sec);
	*buf = 0;
	for(i=0, j=1; format[j] && i < 4040; j++){
		if(format[j] == '%' && format[j+1]){
			j++;
			switch(format[j]){
				case 'y':
				 sprintf((char *)buf+i, "%.2d", tm->tm_year%100);
				 i+=2;
				 break;
				case 'Y':
				 sprintf((char *)buf+i, "%.4d", tm->tm_year+1900);
				 i+=4;
				 break;
				case 'm':
				 sprintf((char *)buf+i, "%.2d", tm->tm_mon+1);
				 i+=2;
				 break;
				case 'd':
				 sprintf((char *)buf+i, "%.2d", tm->tm_mday);
				 i+=2;
				 break;
				case 'H':
				 sprintf((char *)buf+i, "%.2d", tm->tm_hour);
				 i+=2;
				 break;
				case 'M':
				 sprintf((char *)buf+i, "%.2d", tm->tm_min);
				 i+=2;
				 break;
				case 'S':
				 sprintf((char *)buf+i, "%.2d", tm->tm_sec);
				 i+=2;
				 break;
				case 't':
				 sprintf((char *)buf+i, "%.10u", (unsigned)sec);
				 i+=10;
				 break;
				case 'b':
				 i+=sprintf((char *)buf+i, "%u", delay?(unsigned)(param->statscli * 1000./delay):0);
				 break;
				case 'B':
				 i+=sprintf((char *)buf+i, "%u", delay?(unsigned)(param->statssrv * 1000./delay):0);
				 break;				 
				case 'D':
				 i+=sprintf((char *)buf+i, "%u", delay);
				 break;
				case '.':
				 sprintf((char *)buf+i, "%.3u", msec);
				 i+=3;
				 break;
				case 'z':
				 sprintf((char *)buf+i, "%+.2d%.2u", (int)(-timezone/60), (unsigned)(timezone%60));
				 i+=5;
				 break;
				case 'U':
				 if(param->username){
					for(len = 0; i< 4000 && param->username[len]; len++){
					 buf[i] = param->username[len];
					 if(doublec && strchr((char *)doublec, buf[i])) {
						buf[i+1] = buf[i];
						i++;
					 }
					 if(param->nonprintable &&
						(buf[i] < 32 || strchr((char *)param->nonprintable, buf[i])))
						  buf[i] = param->replace;
					 i++;
					}
				 }
				 else {
					buf[i++] = '-';
				 }
				 break;
				case 'n':
					len = param->hostname? strlen((char *)param->hostname) : 0;
					if(len && (len + i) < 4000) {
						memcpy(buf+i, param->hostname, len);
						i+=len;
					}
					else i += myinet_ntoa(param->sins.sin_addr, (char *)buf + i);
					break;

				case 'N':
				 if(param->service >=0 && param->service < 15) {
					 len = (stringtable)? strlen((char *)stringtable[SERVICES + param->service]) : 0;
					 if(len > 20) len = 20;
					 memcpy(buf+i, (len)?stringtable[SERVICES + param->service]:(unsigned char*)"-", (len)?len:1);
					 i += (len)?len:1;
				 }
				 break;
				case 'E':
				 sprintf((char *)buf+i, "%.03d", param->res);
				 i += 3;
				 break;
				case 'T':
				 if(s){
					for(len = 0; i<4000 && s[len]; len++){
					 buf[i] = s[len];
					 if(doublec && strchr((char *)doublec, buf[i])) {
						buf[i+1] = buf[i];
						i++;
					 }
					 if(param->nonprintable &&
						(buf[i] < 32 || strchr((char *)param->nonprintable, buf[i])))
						  buf[i] = param->replace;
					 i++;
					}
				 }
				 break;
				case 'C':
				 i += myinet_ntoa(param->sinc.sin_addr, (char *)buf + i);
				 break;
				case 'R':
				 i += myinet_ntoa(param->sins.sin_addr, (char *)buf + i);
				 break;
				case 'p':
				 sprintf((char *)buf+i, "%hu", ntohs(param->intport));
				 i += strlen((char *)buf+i);
				 break;
				case 'c':
				 sprintf((char *)buf+i, "%hu", ntohs(param->sinc.sin_port));
				 i += strlen((char *)buf+i);
				 break;
				case 'r':
				 sprintf((char *)buf+i, "%hu", ntohs(param->sins.sin_port));
				 i += strlen((char *)buf+i);
				 break;
				case 'I':
				 sprintf((char *)buf+i, "%lu", param->statssrv);
				 i += strlen((char *)buf+i);
				 break;
				case 'O':
				 sprintf((char *)buf+i, "%lu", param->statscli);
				 i += strlen((char *)buf+i);
				 break;
				case 'h':
				 sprintf((char *)buf+i, "%d", param->redirected);
				 i += strlen((char *)buf+i);
				 break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					{
						int k, pmin=0, pmax=0;
						for (k = j; isnumber(format[k]); k++);
						if(format[k] == '-' && isnumber(format[k+1])){
							pmin = atoi(format + j) - 1;
							k++;
							pmax = atoi(format + k) -1;
							for (; isnumber(format[k]); k++);
							j = k;
						}
						if(!s || format[k]!='T') break;
						for(k = 0, len = 0; s[len] && i < 4000; len++){
							if(isspace(s[len])){
								k++;
								while(isspace(s[len+1]))len++;
								if(k == pmin) continue;
							}
							if(k>=pmin && k<=pmax) {
								buf[i] = s[len];
								if(doublec && strchr((char *)doublec, buf[i])) {
									buf[i+1] = buf[i];
									i++;
				 				}
								if(param->nonprintable &&
									(buf[i] < 32 || strchr((char *)param->nonprintable, buf[i])))
					  			 buf[i] = param->replace;
								i++;
							}
						}
						break;

					}
				default:
				 buf[i++] = format[j];
			}
		}
		else buf[i++] = format[j];
	}
	buf[i] = 0;
	clearstat(param);
	return i;
}

void lognone(struct clientparam * param, const unsigned char *s) {
	if(param->trafcountfunc)(*param->trafcountfunc)(param);
	clearstat(param);
}

FILE * stdlog;

void logstdout(struct clientparam * param, const unsigned char *s) {
	unsigned char buf[4096];

	if(!stdlog)stdlog = stdout;
	dobuf(param, buf, s, NULL);
	if(fprintf(param->stdlog?param->stdlog:stdlog, "%s\n", buf) < 0) {
		perror("printf()");
	};
	if(!stringtable)fflush(param->stdlog?param->stdlog:stdlog);
}
#ifndef _WIN32
void logsyslog(struct clientparam * param, const unsigned char *s) {
	unsigned char buf[4096];

	dobuf(param, buf, s, NULL);
	syslog(LOG_INFO, "%s", buf);
}
#endif

int doconnect(struct clientparam * param){
 SASIZETYPE size = sizeof(param->sins);
 struct sockaddr_in bindsa;
 if (param->remsock == INVALID_SOCKET && param->operation != DNSRESOLVE && param->operation !=ADMIN) {
	struct linger lg;
	int opt = 1;

	if(param->hostname && !param->sins.sin_addr.s_addr){
		if(!(param->sins.sin_addr.s_addr = getip(param->hostname))){
			return 100;
		}
	}
	if ((param->remsock=socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {return (11);}
	setsockopt(param->remsock, SOL_SOCKET, SO_LINGER, (unsigned char *)&lg, sizeof(lg));
	setsockopt(param->remsock, SOL_SOCKET, SO_OOBINLINE, (unsigned char *)&opt, sizeof(int));
	bindsa.sin_family = AF_INET;
	bindsa.sin_port = param->extport;
	bindsa.sin_addr.s_addr = param->extip;
	if (param->targetport && !bindsa.sin_port && ntohs(param->sinc.sin_port) > 1023) bindsa.sin_port = param->sinc.sin_port;
	if(bind(param->remsock, (struct sockaddr*)&bindsa, sizeof(bindsa))==-1) {
		bindsa.sin_port = 0;
		if(bind(param->remsock, (struct sockaddr*)&bindsa, sizeof(bindsa))==-1) {
			return 12;
		}
	}
	
	param->sins.sin_family = AF_INET;
	if(param->operation >= 256 || (param->operation & CONNECT)){
#ifdef _WIN32
		unsigned long ul;
#endif
		if(connect(param->remsock,(struct sockaddr *)&param->sins,sizeof(param->sins))) {return (13);}
#ifdef _WIN32
		ioctlsocket(param->remsock, FIONBIO, &ul);
#else
		fcntl(param->remsock,F_SETFL,O_NONBLOCK);
#endif

	}
	else {
		if(getsockname(param->remsock, (struct sockaddr *)&param->sins, &size)==-1) {return (15);}
	}
 }
 return 0;
}

void freeparam(struct clientparam * param) {
	if(param->res == 2) return;
	if(param->clibuf) myfree(param->clibuf);
	if(param->srvbuf) myfree(param->srvbuf);
	if(param->parent){
		param->parent->statscli += param->statscli;
		param->parent->statssrv += param->statssrv;
		pthread_mutex_lock(param->counter_mutex);
		if(param->prev)	param->prev->next = param->next;
		else param->parent->child = param->next;
		if (param->next) param->next->prev = param->prev;
		(*param->childcount)--;
		pthread_mutex_unlock(param->counter_mutex);
	}
	if(param->hostname) myfree(param->hostname);
	if(param->username) myfree(param->username);
	if(param->password) myfree(param->password);
	if(param->extusername) myfree(param->extusername);
	if(param->extpassword) myfree(param->extpassword);
	if(param->remsock != INVALID_SOCKET) {
		shutdown(param->remsock, SHUT_RDWR);
		closesocket(param->remsock);
	}
	if(param->clisock != INVALID_SOCKET) {
		shutdown(param->clisock, SHUT_RDWR);
		closesocket(param->clisock);
	}
	if(param->ctrlsock != INVALID_SOCKET) {
		shutdown(param->ctrlsock, SHUT_RDWR);
		closesocket(param->ctrlsock);
	}
	myfree(param);
}

int scanaddr(const unsigned char *s, unsigned long * ip, unsigned long * mask) {
	unsigned d1, d2, d3, d4, m;
	int res;
	if ((res = sscanf((char *)s, "%u.%u.%u.%u/%u", &d1, &d2, &d3, &d4, &m)) < 4) return 0;
	if(mask && res == 4) *mask = 0xFFFFFFFF;
	else if (mask) *mask = htonl(0xFFFFFFFF << (32 - m));
	*ip = htonl ((d1<<24) ^ (d2<<16) ^ (d3<<8) ^ d4);
	return res;
}


RESOLVFUNC resolvfunc = NULL;
#ifndef _WIN32
pthread_mutex_t gethostbyname_mutex;
int ghbn_init = 0;
#endif

unsigned long getip(unsigned char *name){
	unsigned long retval;
	int i;
	int ndots = 0;
	struct hostent *hp=NULL;

	if(strlen((char *)name)>255)name[255] = 0;
	for(i=0; name[i]; i++){
		if(name[i] == '.'){
			if(++ndots > 3) break;
			continue;
		}
		if(name[i] <'0' || name[i] >'9') break;
	}
	if(!name[i] && ndots == 3){
		unsigned long ip;
		if(scanaddr(name, &ip, NULL) == 4){
			return ip;
		}
	}
	if(resolvfunc){
		if((retval = (*resolvfunc)(name))) return retval;
		if(demanddialprog) system(demanddialprog);
		return (*resolvfunc)(name);
	}
#ifndef _WIN32
	if(!ghbn_init){
		pthread_mutex_init(&gethostbyname_mutex, NULL);
		ghbn_init++;
	}
	pthread_mutex_lock(&gethostbyname_mutex);
#endif
	hp=gethostbyname((char *)name);
	if (!hp && demanddialprog) {
		system(demanddialprog);
		hp=gethostbyname((char *)name);
	}
	retval = hp?*(unsigned long *)hp->h_addr:0;
#ifndef _WIN32
	pthread_mutex_unlock(&gethostbyname_mutex);
#endif
	return retval;
}
