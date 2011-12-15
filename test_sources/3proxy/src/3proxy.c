/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: 3proxy.c,v 1.51 2006/03/10 19:25:45 vlad Exp $
*/

#include "proxy.h"

#ifndef DEFAULTCONFIG
#define DEFAULTCONFIG stringtable[20]
#endif

typedef int (*MAINFUNC)(int, char**);

pthread_mutex_t bandlim_mutex;
pthread_mutex_t tc_mutex;
pthread_mutex_t hash_mutex;
pthread_mutex_t acl_mutex;

#ifndef NOODBC
pthread_mutex_t odbc_mutex;
#endif

int readconfig(FILE * fp);


int haveerror = 0;
int linenum = 0;
int needreload = 0;

time_t basetime = 0;

char * conffile = NULL;

struct counter_header {
	unsigned char sig[4];
	time_t updated;
} cheader = {"3CF", (time_t)0};

struct counter_record {
	unsigned long traf;
	unsigned long trafgb;
	time_t cleared;
	time_t updated;
} crecord;

struct child {
	int argc;
	unsigned char **argv;
};


int udpmainfunc (int argc, char** argv);
int tcpmainfunc (int argc, char** argv);

struct proxydef childdef = {NULL, 0, 0, S_NOSERVICE, ""};

#define STRINGBUF 65535
#define NPARAMS	  4096

time_t logtime = 0, t = 0;
unsigned char tmpbuf[1024];
int bandlim_mutex_inited = 0;
FILE *writable;

extern unsigned char *strings[];

#ifndef _WIN32
char *chrootp = NULL;
#endif
char * curconf = NULL;

FILE * confopen(){
	curconf = conffile;
#ifndef _WIN32
	if(chrootp){
		if(strstr(curconf, chrootp) == curconf)
			curconf += strlen(chrootp);
	}
#endif
	if(writable) {
		rewind(writable);
		return writable;
	}
	return fopen(curconf, "r");
}

static void * itfree(void *data, void * retval){
	myfree(data);
	return retval;
}

void clearall(){
 struct bandlim * bl;
 struct bandlim * blout;
 struct trafcount * tc;
 struct passwords *pw;
 struct ace *ac;
 struct ace *acls[256];
 struct iplist *ipl;
 struct portlist *pl;
 struct userlist *ul;
 struct chain *ch;
 struct filemon *fm;
 int counterd, archiverc;
 unsigned char *logname;
 unsigned char **archiver;

 int i;

 pthread_mutex_lock(&bandlim_mutex);
 bl = conf.bandlimiter;
 blout = conf.bandlimiterout;
 conf.bandlimiter = NULL;
 conf.bandlimiterout = NULL;
 pthread_mutex_unlock(&bandlim_mutex);
 pthread_mutex_lock(&tc_mutex);
 tc = conf.trafcounter;
 conf.trafcounter = NULL;
 pthread_mutex_unlock(&tc_mutex);
 counterd = conf.counterd;
 conf.counterd = -1;
 logname = conf.logname;
 conf.logname = NULL;
 archiverc = conf.archiverc;
 conf.archiverc = 0;
 archiver = conf.archiver;
 conf.archiver = NULL;
 fm = conf.fmon;
 conf.fmon = NULL;
 pthread_mutex_lock(&acl_mutex);
 for(i = 0; i < 256; i++) {
	acls[i] = conf.acls[i];
	conf.acls[i] = NULL;
 }
 pw = conf.pwl;
 conf.pwl = NULL;
 pthread_mutex_unlock(&acl_mutex);
 conf.rotate = 0;
 conf.logtype = NONE;
 conf.countertype = NONE;
 conf.aclnum = 0;
 logtime = t = 0;
 conf.authfunc = doconnect;
 conf.bandlimfunc = NULL;
 conf.intip = conf.extip = 0;
 conf.intport = conf.extport = 0;
 conf.singlepacket = 0;
 conf.maxchild = 100;
 resolvfunc = NULL;

 usleep(SLEEPTIME);

 for(; bl; bl = (struct bandlim *) itfree(bl, bl->next));
 for(; blout; blout = (struct bandlim *) itfree(blout, blout->next));
 for(; tc; tc = (struct trafcount *) itfree(tc, tc->next));
 for(; pw; pw = (struct passwords *)itfree(pw, pw->next)){
	if(pw->user) myfree(pw->user);
 }
 for(; fm; fm = (struct filemon *)itfree(fm, fm->next)){
	if(fm->path) myfree(fm->path);
 }
 if(counterd != -1) {
	close(counterd);
 }
 if(logname) {
	myfree(logname);
 }
 if(archiver) {
	for(i = 0; i < archiverc; i++) myfree(archiver[i]);
	myfree(archiver);
 }
 for(i = 0; i < 256; i++) {
	for(ac = acls[i]; ac; ac = (struct ace *) itfree(ac, ac->next)){
		for(ipl = ac->src; ipl; ipl = (struct iplist *)itfree(ipl, ipl->next));
		for(ipl = ac->dst; ipl; ipl = (struct iplist *)itfree(ipl,ipl->next));
		for(pl = ac->ports; pl; pl = (struct portlist *)itfree(pl, pl->next));
		for(ul = ac->users; ul && ul->next; ul = ul->next);
		if(ul) myfree(ul);
		for(ch = ac->chains; ch; ch = (struct chain *) itfree(ch, ch->next)){
			if(ch->extuser) myfree(ch->extuser);
			if(ch->extpass) myfree(ch->extpass);
		}
	}
 }
}

#ifdef _WIN32
OSVERSIONINFO osv;
int service = 0;

void cyclestep(void);
SERVICE_STATUS_HANDLE hSrv;
DWORD dwCurrState;
int SetStatus( DWORD dwState, DWORD dwExitCode, DWORD dwProgress )
{
    SERVICE_STATUS srvStatus;
    srvStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    srvStatus.dwCurrentState = dwCurrState = dwState;
    srvStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_PAUSE_CONTINUE | SERVICE_ACCEPT_SHUTDOWN;
    srvStatus.dwWin32ExitCode = dwExitCode;
    srvStatus.dwServiceSpecificExitCode = 0;
    srvStatus.dwCheckPoint = dwProgress;
    srvStatus.dwWaitHint = 3000;
    return SetServiceStatus( hSrv, &srvStatus );
}

void __stdcall CommandHandler( DWORD dwCommand )
{
    FILE *fp;
    int error;
    switch( dwCommand )
    {
    case SERVICE_CONTROL_STOP:
    case SERVICE_CONTROL_SHUTDOWN:
        SetStatus( SERVICE_STOP_PENDING, 0, 1 );
	paused++;
	timetoexit = 1;
	Sleep(2000);
        SetStatus( SERVICE_STOPPED, 0, 0 );
#ifndef NOODBC
	pthread_mutex_lock(&odbc_mutex);
	close_sql();
	pthread_mutex_unlock(&odbc_mutex);
#endif
        break;
    case SERVICE_CONTROL_PAUSE:
        SetStatus( SERVICE_PAUSE_PENDING, 0, 1 );
	paused++;
        SetStatus( SERVICE_PAUSED, 0, 0 );
        break;
    case SERVICE_CONTROL_CONTINUE:
        SetStatus( SERVICE_CONTINUE_PENDING, 0, 1 );
	clearall();
	fp = confopen();
	if(fp){
		error = readconfig(fp);
		if(error) {
			clearall();
		}
		if(!writable)fclose(fp);
	}
        SetStatus( SERVICE_RUNNING, 0, 0 );
        break;
    default: ;
    }
}


void __stdcall ServiceMain(int argc, unsigned char* argv[] )
{

    hSrv = RegisterServiceCtrlHandler(stringtable[1], (LPHANDLER_FUNCTION)CommandHandler);
    if( hSrv == 0 ) return;

    SetStatus( SERVICE_START_PENDING, 0, 1 );
    SetStatus( SERVICE_RUNNING, 0, 0 );
    cyclestep();
}

#else


void mysigusr1 (int sig){
	needreload = 1;
}

int even = 0;

void mysigpause (int sig){

	paused++;
	even = !even;
	if(!even){
		needreload = 1;
	}
}

void mysigterm (int sig){
	paused++;
	usleep(2000*SLEEPTIME);
#ifndef NOODBC
	pthread_mutex_lock(&odbc_mutex);
	close_sql();
	pthread_mutex_unlock(&odbc_mutex);
#endif
	timetoexit = 1;
}

#endif

int reload (void){
	FILE *fp;
	int error = -2;

	paused++;
	clearall();
	paused++;
	fp = confopen();
	if(fp){
		error = readconfig(fp);
		if(error) {
			clearall();
		}
		if(!writable)fclose(fp);
	}
	return error;
}


#ifdef _WIN32
DWORD WINAPI startsrv(LPVOID data) {
#else
void * startsrv(void * data) {
#endif
  struct child *d = (struct child *)data;
   if(childdef.isudp) udpmainfunc(d->argc, (char **)d->argv);
   else tcpmainfunc(d->argc, (char **)d->argv);
  return 0;
}

int included =0;

int parsestr (unsigned char *str, unsigned char **argm, int nitems, unsigned char * buf, int *inbuf, int *bufsize){
	int argc = 0;
	int space = 1;
	int comment = 0;
	unsigned char * incbegin = 0;
	int fd;
	int res, len;
	int i = 1;
	unsigned char *str1;

	for(;;str++){
	 if(*str == '\"'){
		str1 = str;
		do {
			*str1 = *(str1 + 1);
		}while(*(str1++));
		if(!comment || *str != '\"'){
			comment = !comment;
		}
	 }
         switch(*str){
		case '\0': 
			if(comment) return -1;
			argm[argc] = 0;
			return argc;
		case '$':
			if(!comment && !included){
				incbegin = str;
				*str = 0;
			}
			break;
		case '\r':
		case '\n':
		case '\t':
		case ' ':
			if(!comment){
				*str = 0;
				space = 1;
				i = 0;
				if(incbegin){
					argc--;
					if((fd = open((char *)incbegin+1, O_RDONLY)) <= 0){
						fprintf(stderr, "Failed to open %s\n", incbegin+1);
						break;
					}
					if((*bufsize - *inbuf) <STRINGBUF){
						*bufsize += STRINGBUF;
						if(!(buf = myrealloc(buf, *bufsize))){
							fprintf(stderr, "Failed to allocate memory for %s\n", incbegin+1);
							close(fd);
							break;
						}
					}
					len = 0;
					if(argm[argc]!=(incbegin+1)) {
						len = strlen((char *)argm[argc]);
						memcpy(buf+*inbuf, argm[argc], len);
					}
					if((res = read(fd, buf+*inbuf+len, STRINGBUF-(1+len))) <= 0) {
						perror(incbegin+1);
						close(fd);
						break;
					}
					close(fd);
					buf[*inbuf+res+len] = 0;
					incbegin = buf + *inbuf;
					(*inbuf) += (res + len + 1);
					included++;
					argc+=parsestr(incbegin, argm + argc, nitems - argc, buf, inbuf, bufsize);
					included--;
					incbegin = NULL;

				}
				break;
			}
		default:
			i++;
			if(space) {
				space = 0;
				argm[argc++] = str;
				if(argc >= nitems) return argc;
			}
	 }
	}
}

unsigned char * dologname (unsigned char *buf, const unsigned char *name, const unsigned char *ext, ROTATION lt, time_t t) {
	struct tm *ts;
	ts = localtime(&t);
	switch(lt){
		case NONE:
			sprintf((char *)buf, "%s", name);
			break;
		case ANNUALLY:
			sprintf((char *)buf, "%s.%04d", name, ts->tm_year+1900);
			break;
		case MONTHLY:
			sprintf((char *)buf, "%s.%04d.%02d", name, ts->tm_year+1900, ts->tm_mon+1);
			break;
		case WEEKLY:
			t = t - (ts->tm_wday * (60*60*24));
			ts = localtime(&t);
			sprintf((char *)buf, "%s.%04d.%02d.%02d", name, ts->tm_year+1900, ts->tm_mon+1, ts->tm_mday);
			break;
		case DAILY:
			sprintf((char *)buf, "%s.%04d.%02d.%02d", name, ts->tm_year+1900, ts->tm_mon+1, ts->tm_mday);
			break;
		case HOURLY:
			sprintf((char *)buf, "%s.%04d.%02d.%02d-%02d", name, ts->tm_year+1900, ts->tm_mon+1, ts->tm_mday, ts->tm_hour);
			break;
		case MINUTELY:
			sprintf((char *)buf, "%s.%04d.%02d.%02d-%02d.%02d", name, ts->tm_year+1900, ts->tm_mon+1, ts->tm_mday, ts->tm_hour, ts->tm_min);
			break;
		default:
			break;
	}
	if(ext){
		strcat((char *)buf, ".");
		strcat((char *)buf, (char *)ext);
	}
	return buf;
}

int wday = 0;

int timechanged (time_t oldtime, time_t newtime, ROTATION lt){
	struct tm tmold;
	struct tm *tm;
	tm = localtime(&oldtime);
	memcpy(&tmold, tm, sizeof(tmold));
	tm = localtime(&newtime);
	switch(lt){
		case MINUTELY:
			if(tm->tm_min != tmold.tm_min)return 1;
			break;
		case HOURLY:
			if(tm->tm_hour != tmold.tm_hour)return 1;
			break;
		case DAILY:
			if(tm->tm_yday != tmold.tm_yday)return 1;
			break;
		case MONTHLY:
			if(tm->tm_mon != tmold.tm_mon)return 1;
			break;
		case ANNUALLY:
			if(tm->tm_year != tmold.tm_year)return 1;
			break;
		case WEEKLY:
			if(((newtime - oldtime) > (60*60*24*7))
				|| tm->tm_wday < tmold.tm_wday
				|| (tm->tm_wday == tmold.tm_wday && (newtime - oldtime) > (60*60*24*6))
				)return 1;
			break;
		default:
			break;	
	}
	return 0;
}

void cyclestep(void){
 struct tm *tm;
 time_t minutecounter;

 minutecounter = time(0);
 for(;;){
	usleep(SLEEPTIME*1000);
	
	if(needreload) {
		reload();
		needreload = 0;
	}
	t = time(0);
	fflush(stdlog);
	if(timechanged(minutecounter, t, MINUTELY)) {
		struct filemon *fm;
		struct stat sb;

		for(fm=conf.fmon; fm; fm=fm->next){
			if(!stat(fm->path, &sb)){
				if(fm->sb.st_mtime != sb.st_mtime || fm->sb.st_size != sb.st_size){
					stat(fm->path, &fm->sb);
					needreload = 1;
				}
			}
		}
		
	}
	if(timechanged(basetime, t, DAILY)) {
		tm = localtime(&t);
		wday = (1 << tm->tm_wday);
		tm->tm_hour = tm->tm_min = tm->tm_sec = 0;
		basetime = mktime(tm);
	}
	if(conf.logname) {
		if(timechanged(logtime, t, conf.logtype)) {
			FILE *fp, *fp1;
			fp = fopen((char *)dologname (tmpbuf, conf.logname, NULL, conf.logtype, t), "a");
			if (fp) {
				fp1 = stdlog;
				stdlog = fp;
				if(fp1) fclose(fp1);
			}
			fseek(stdout, 0L, SEEK_END);
			usleep(SLEEPTIME);
			logtime = t;
			if(conf.logtype != NONE && conf.rotate) {
				t = 1;
				switch(conf.logtype){
					case ANNUALLY:
						t = t * 12;
					case MONTHLY:
						t = t * 4;
					case WEEKLY:
						t = t * 7;
					case DAILY:
						t = t * 24;
					case HOURLY:
						t = t * 60;
					case MINUTELY:
						t = t * 60;
					default:
						break;
				}
				dologname (tmpbuf, conf.logname, (conf.archiver)?conf.archiver[1]:NULL, conf.logtype, (logtime - t*conf.rotate));
				remove ((char *) tmpbuf);
				if(conf.archiver) {
					int i;
					*tmpbuf = 0;
					for(i = 2; i < conf.archiverc && strlen((char *)tmpbuf) < 512; i++){
						strcat((char *)tmpbuf, " ");
						if(!strcmp((char *)conf.archiver[i], "%A")){
							strcat((char *)tmpbuf, "\"");
							dologname (tmpbuf + strlen((char *)tmpbuf), conf.logname, conf.archiver[1], conf.logtype, (logtime - t));
							strcat((char *)tmpbuf, "\"");
						}
						else if(!strcmp((char *)conf.archiver[i], "%F")){
							strcat((char *)tmpbuf, "\"");
							dologname (tmpbuf+strlen((char *)tmpbuf), conf.logname, NULL, conf.logtype, (logtime-t));
							strcat((char *)tmpbuf, "\"");
						}
						else
							strcat((char *)tmpbuf, (char *)conf.archiver[i]);
					}
					system((char *)tmpbuf+1);
				}
			}
		}
	}
	if(conf.counterd >= 0) {
		time(&t);
		if(timechanged(cheader.updated, t, MINUTELY)){
			struct trafcount *tl;
			if(conf.countertype && timechanged(cheader.updated, t, conf.countertype)){
				FILE * cfp;
				
				cfp = fopen((char *)dologname(tmpbuf, (unsigned char *)conf.counterfile, NULL, conf.countertype, t), "w");
				if(cfp){
					for(tl = conf.trafcounter; cfp && tl; tl = tl->next){
						if(tl->type >= conf.countertype)
							fprintf(cfp, "%05d %010lu %010lu%s%s\n", tl->number, tl->trafgb, tl->traf, tl->comment?" #" : "", tl->comment? tl->comment : "");
					}
					fclose(cfp);
				}
			}
			for(tl = conf.trafcounter; tl; tl = tl->next){
				if(tl->number){
					if(tl->type!=NEVER && timechanged(tl->cleared, t, tl->type)){
						tl->cleared = t;
						tl->traf = 0;
						tl->trafgb = 0;
					}
					lseek(conf.counterd, 
						sizeof(struct counter_header) + (tl->number - 1) * sizeof(struct counter_record),
						SEEK_SET);
					crecord.traf = tl->traf;
					crecord.trafgb = tl->trafgb;
					crecord.cleared = tl->cleared;
					crecord.updated = tl->updated;
					write(conf.counterd, &crecord, sizeof(struct counter_record));
				}
			}
			cheader.updated = t;
			lseek(conf.counterd, 0, SEEK_SET);
			write(conf.counterd, &cheader, sizeof(struct counter_header));			
		}
	}
	if(timetoexit){
		paused++;
		usleep(SLEEPTIME*3000);
		return;
	}
		
 }
}


#define RETURN(x) {res = x; goto CLEARRETURN;}

char logident[128];


int readconfig(FILE * fp){
 unsigned char ** args = NULL;
 unsigned char * buf = NULL;
  pthread_t thread;
  struct child chdata;
  struct child * ch=&chdata;
  int bufsize = STRINGBUF*2;
  int inbuf = 0;
  int cargc;
  struct ace *acl = NULL;
  struct passwords *pwl = NULL;
  struct chain *chains;
  unsigned char *arg;
  int res = 0;
#ifdef _WIN32
  HANDLE h;
#endif

  if( !(buf = myalloc(bufsize)) || ! (args = myalloc(NPARAMS * sizeof(unsigned char *) + 1)) ) {
		fprintf(stderr, "No memory for configuration");
		return(10);
  }
  ch->argv = args;
  for (linenum = 1; fgets((char *)buf, STRINGBUF, fp); linenum++){
	if(!*buf || isspace(*buf) || (*buf) == '#')continue;
	inbuf = (strlen((char *)buf) + 1);
	cargc = parsestr (buf, args, NPARAMS-1, buf, &inbuf, &bufsize);
	if(cargc < 1) {
		fprintf(stderr, "Parse error line %d\n", linenum);
		return(21);
	}
	ch->argv[cargc] = NULL;
	ch->argc = cargc;
	if(!strcmp((char *)ch->argv[0], "proxy")) {
		childdef.pf = proxychild;
		childdef.port = 3128;
		childdef.isudp = 0;
		childdef.service = S_PROXY;
		childdef.helpmessage = " -n - no NTLM support\n";
	}
	else if(!strcmp((char *)ch->argv[0], "pop3p")) {
		childdef.pf = pop3pchild;
		childdef.port = 110;
		childdef.isudp = 0;
		childdef.service = S_POP3P;
		childdef.helpmessage = "";
	}
	else if(!strcmp((char *)ch->argv[0], "ftppr")) {
		childdef.pf = ftpprchild;
		childdef.port = 21;
		childdef.isudp = 0;
		childdef.service = S_FTPPR;
		childdef.helpmessage = "";
	}
	else if(!strcmp((char *)ch->argv[0], "socks")) {
		childdef.pf = sockschild;
		childdef.port = 1080;
		childdef.isudp = 0;
		childdef.service = S_SOCKS;
		childdef.helpmessage = " -n - no NTLM support\n";
	}
	else if(!strcmp((char *)ch->argv[0], "tcppm")) {
		childdef.pf = tcppmchild;
		childdef.port = 0;
		childdef.isudp = 0;
		childdef.service = S_TCPPM;
		childdef.helpmessage = "";
	}
	else if(!strcmp((char *)ch->argv[0], "udppm")) {
		childdef.pf = udppmchild;
		childdef.port = 0;
		childdef.isudp = 1;
		childdef.service = S_UDPPM;
		childdef.helpmessage = " -s single packet UDP service for request/reply (DNS-like) services\n";
	}
	else if(!strcmp((char *)ch->argv[0], "admin")) {
		childdef.pf = adminchild;
		childdef.port = 80;
		childdef.isudp = 0;
		childdef.service = S_ADMIN;
	}
	else if(!strcmp((char *)ch->argv[0], "dnspr")) {
		childdef.pf = dnsprchild;
		childdef.port = 53;
		childdef.isudp = 1;
		childdef.service = S_DNSPR;
	}
	else if(!strcmp((char *)ch->argv[0], "internal") && ch->argc == 2) {
		conf.intip = getip((unsigned char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "external") && ch->argc == 2) {
		conf.extip = getip((unsigned char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "log") && ch->argc <= 3) {
		conf.logfunc = logstdout;
		if(ch->argc > 1) {
			if(*ch->argv[1]=='@'){
#ifndef _WIN32
				res = (int) strlen((char *)ch->argv[1]+1);
				if(res>127) res = 127;
				memcpy(logident, ch->argv[1]+1, res+1);
				logident[127] = 0;
				openlog(logident, LOG_PID, LOG_DAEMON);
				conf.logfunc = logsyslog;
#endif
			}
#ifndef NOODBC
			else if(*ch->argv[1]=='&'){
				pthread_mutex_lock(&odbc_mutex);
				close_sql();
				init_sql((char *)ch->argv[1]+1);
				pthread_mutex_unlock(&odbc_mutex);
				conf.logfunc = logsql;
			}
#endif
			else {
				FILE *fp, *fp1;
				if(ch->argc > 2) {
					switch(*ch->argv[2]){
					case 'd':
					case 'D':
						conf.logtype = DAILY;
						break;
					case 'w':
					case 'W':
						conf.logtype = WEEKLY;
						break;
					case 'y':
					case 'Y':
						conf.logtype = ANNUALLY;
						break;
					case 'm':
					case 'M':
						conf.logtype = MONTHLY;
						break;
					case 'h':
					case 'H':
						conf.logtype = HOURLY;
						break;
					case 'c':
					case 'C':
						conf.logtype = MINUTELY;
						break;
					default:
						break;
					}
				}
				logtime = time(0);
				conf.logname = (unsigned char *)mystrdup((char *)ch->argv[1]);
				fp = fopen((char *)dologname (tmpbuf, conf.logname, NULL, conf.logtype, logtime), "a");
				if(!fp){
					perror("fopen()");
				}
				else {
					fp1 = stdlog;
					stdlog = fp;
					if(fp1) fclose(fp1);
				}
			}
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "service") && ch->argc == 1) {	
#ifdef _WIN32
		if(osv.dwPlatformId  == VER_PLATFORM_WIN32_NT) service = 1;
		else {
			if(!demon)daemonize();
			demon = 1;
		}
#endif
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "daemon") && ch->argc == 1) {	
		if(!demon)daemonize();
		demon = 1;
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "writable") && ch->argc == 1) {	
		if(!writable){
			writable = freopen(curconf, "r+", fp);
			if(!writable){
				fprintf(stderr, "Unable to reopen config for writing: %s\n", curconf);
				return -28;
			}
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "end") && ch->argc == 1) {	
		break;
	}
	else if(!strcmp((char *)ch->argv[0], "config") && ch->argc == 2) {	
		if(conffile)myfree(conffile);
		conffile = mystrdup((char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "include") && ch->argc == 2) {
		FILE *fp1;

		fp1 = fopen((char *)ch->argv[1], "r");
		if(!fp1){
			fprintf(stderr, "Unable to open included file: %s\n", ch->argv[1]);
			return -28;
		}
		res = readconfig(fp1);
		fclose(fp1);
		if(res) return res;
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "archiver") && ch->argc > 2) {
		int j;

		conf.archiver = myalloc(ch->argc * sizeof(char *));
		if(conf.archiver) {
			conf.archiverc = ch->argc;
			for(j = 0; j < conf.archiverc; j++) conf.archiver[j] = (unsigned char *)mystrdup((char *)ch->argv[j]);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "counter") && ch->argc >= 2) {	
		if(conf.counterd >=0)close(conf.counterd);
		conf.counterd = open((char *)ch->argv[1], O_BINARY|O_RDWR|O_CREAT, 0660);
		if(conf.counterd<0){
			fprintf(stderr, "Unable to open counter file %s, line %d\n", ch->argv[1], linenum);
			return(18);
		}
		if(ch->argc >=4) {
			switch(*ch->argv[2]){
			case 'd':
			case 'D':
				conf.countertype = DAILY;
				break;
			case 'w':
			case 'W':
				conf.countertype = WEEKLY;
				break;
			case 'y':
			case 'Y':
				conf.countertype = ANNUALLY;
				break;
			case 'm':
			case 'M':
				conf.countertype = MONTHLY;
				break;
			case 'h':
			case 'H':
				conf.countertype = HOURLY;
				break;
			case 'c':
			case 'C':
				conf.countertype = MINUTELY;
				break;
			default:
				fprintf(stderr, "Unknown counter type, line: %d\n", linenum);
				return(18);
			}
			conf.counterfile = mystrdup((char *)ch->argv[3]);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "rotate") && ch->argc == 2) {	
		conf.rotate = atoi((char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "logformat") && ch->argc == 2 && strlen((char *)ch->argv[1])) {	
		if(conf.logformat) myfree(conf.logformat);
		conf.logformat = (unsigned char *)mystrdup((char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "timeouts") && ch->argc > 1) {	
		int j;

		for(j = 0; conf.timeouts[j] && j + 1 < ch->argc; j++) {
			if((conf.timeouts[j] = atoi((char *)ch->argv[j + 1])) <= 0 || conf.timeouts[j] > 2000000){
				fprintf(stderr, "Invalid timeout: %s, line %d\n", ch->argv[j + 1], linenum);
				return(19);
			}
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "auth") && ch->argc == 2) {	
		if(!strcmp((char *)ch->argv[1], "none")){
			conf.authfunc = alwaysauth;
		}
		else if (!strcmp((char *)ch->argv[1], "iponly")){
			conf.authfunc = ipauth;
		}
		else if (!strcmp((char *)ch->argv[1], "nbname")){
			conf.authfunc = nbnameauth;
		}
		else if (!strcmp((char *)ch->argv[1], "strong")){
			conf.authfunc = strongauth;
		}
		else {
			fprintf(stderr, "Unknown auth type: '%s' line %d\n", ch->argv[1], linenum);
			return(22);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "maxconn") && ch->argc == 2) {	
		conf.maxchild = atoi((char *)ch->argv[1]);
		if(!conf.maxchild) {
			fprintf(stderr, "Invalid maxconn value, line %d\n", linenum);
			return(22);
		}
		continue;
	}
	else if (!strcmp((char *)ch->argv[0], "users") && ch->argc >= 2) {
		int j;

		for (j = 1; j<ch->argc; j++) {
			if(!conf.pwl)conf.pwl = pwl = myalloc(sizeof(struct passwords));
			else {
				pwl->next = myalloc(sizeof(struct passwords));
				pwl = pwl->next;
			}
			if(!pwl) {
				fprintf(stderr, "No memory for PWL entry, line %d\n", linenum);
				return(22);
			}
			memset(pwl, 0, sizeof(struct passwords));
			pwl->user = (unsigned char *)mystrdup((char *)ch->argv[j]);
			arg = (unsigned char *)strchr((char *)pwl->user, ':');
			if(!arg||!arg[1]||!arg[2]||arg[3]!=':')	{
				continue;
			}
			pwl->password = arg + 4;
			if(arg[1] == 'C' && arg[2] == 'L')pwl->pwtype = CL;
			else if(arg[1] == 'C' && arg[2] == 'R')pwl->pwtype = CR;
			else if(arg[1] == 'N' && arg[2] == 'T'){
				pwl->pwtype = NT;
				fromhex(pwl->password, pwl->password, (strlen((char *)pwl->password)>>1));
			}
			else if(arg[1] == 'L' && arg[2] == 'M')pwl->pwtype = LM;
			else {
				continue;
			}
			*arg = 0;
		}
		continue;
	}
	else if (( (!strcmp((char *)ch->argv[0], "allow") && (res=ALLOW) == ALLOW) || 
				    (!strcmp((char *)ch->argv[0], "deny") && (res=DENY) == DENY) ||
				    (!strcmp((char *)ch->argv[0], "redirect") && ch->argc >= 3 && (res=REDIRECT) ==REDIRECT) ||
				    (!strcmp((char *)ch->argv[0], "bandlimin") && ch->argc >= 2 && (res=BANDLIM) ==BANDLIM) ||
				    (!strcmp((char *)ch->argv[0], "bandlimout") && ch->argc >= 2 && (res=BANDLIM) ==BANDLIM) ||
				    (!strcmp((char *)ch->argv[0], "nobandlimin") && (res=NOBANDLIM) ==NOBANDLIM) ||
				    (!strcmp((char *)ch->argv[0], "nobandlimout") && (res=NOBANDLIM) ==NOBANDLIM) ||
				    (!strcmp((char *)ch->argv[0], "countin") && ch->argc >= 4 && (res=COUNT) ==COUNT) ||
				    (!strcmp((char *)ch->argv[0], "nocountin") && (res=NOCOUNT) ==NOCOUNT)

				  )) {
		int offset = 0;
		struct iplist *ipl=NULL;
		struct portlist *portl=NULL;
		struct userlist *userl=NULL;

		if(res == REDIRECT) offset = 2;
		else if(res == BANDLIM) offset = 1;
		else if(res == COUNT) offset = 3;
		acl = myalloc(sizeof(struct ace));
		if(!acl) {
			fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
			return(22);
		}
		memset(acl, 0, sizeof(struct ace));
		acl->action = res;
		if(res == 2){
			acl->chains = myalloc(sizeof(struct chain));
			if(!acl->chains) {
				fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
				return(22);
			}
			acl->chains->type = R_HTTP;
			acl->chains->redirip = getip(ch->argv[1]);
			acl->chains->redirport = htons((unsigned short)atoi((char *)ch->argv[2]));
			acl->chains->weight = 1000;
			acl->chains->extuser = NULL;
			acl->chains->extpass = NULL;
			acl->chains->next = NULL;
		}
		if(ch->argc - offset >= 2 && strcmp("*", (char *)ch->argv[1 + offset])) {
			arg = (unsigned char *)mystrdup((char *)ch->argv[1 + offset]);
			arg = (unsigned char *)strtok((char *)arg, ",");
			do {
				if(!acl->users) {
					acl->users = userl = myalloc(sizeof(struct userlist));
				}
				else {
					userl->next = myalloc(sizeof(struct userlist));
					userl = userl -> next;
				}
				if(!userl) {
					fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
					return(22);
				}
				memset(userl, 0, sizeof(struct userlist));
				userl->user=arg;
			} while((arg = (unsigned char *)strtok((char *)NULL, ",")));
		}
		if(ch->argc - offset >= 3 && strcmp("*", (char *)ch->argv[2 + offset])) {
			arg = (unsigned char *)strtok((char *)ch->argv[2 + offset], ",");
			do {
				if(!acl->src) {
					acl->src = ipl = myalloc(sizeof(struct iplist));
				}
				else {
					ipl->next = myalloc(sizeof(struct iplist));
					ipl = ipl -> next;
				}
				if(!ipl) {
					fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
					return(22);
				}
				memset(ipl, 0, sizeof(struct iplist));
				if (!scanaddr(arg, &ipl->ip, &ipl->mask)) {
					fprintf(stderr, "Invalid IP or CIDR, line %d\n", linenum);
					return(22);
				}
			} while((arg = (unsigned char *)strtok((char *)NULL, ",")));
		}
		if(ch->argc - offset >= 4 && strcmp("*", (char *)ch->argv[3 + offset])) {
			arg = (unsigned char *)strtok((char *)ch->argv[3 + offset], ",");
			do {
				if(!acl->dst) {
					acl->dst = ipl = myalloc(sizeof(struct iplist));
				}
				else {
					ipl->next = myalloc(sizeof(struct iplist));
					ipl = ipl -> next;
				}
				if(!ipl) {
					fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
					return(22);
				}
				memset(ipl, 0, sizeof(struct iplist));
				if (!scanaddr(arg, &ipl->ip, &ipl->mask)) {
					fprintf(stderr, "Invalid IP or CIDR, line %d\n", linenum);
					return(22);
				}
			} while((arg = (unsigned char *)strtok((char *)NULL, ",")));
		}
		if(ch->argc - offset >= 5 && strcmp("*", (char *)ch->argv[4 + offset])) {
			arg = (unsigned char *)strtok((char *)ch->argv[4 + offset], ",");
			do {
				if(!acl->ports) {
					acl->ports = portl = myalloc(sizeof(struct portlist));
				}
				else {
					portl->next = myalloc(sizeof(struct portlist));
					portl = portl -> next;
				}
				if(!portl) {
					fprintf(stderr, "No memory for ACL entry, line %d\n", linenum);
					return(22);
				}
				memset(portl, 0, sizeof(struct portlist));
				res = sscanf((char *)arg, "%hu-%hu", &portl->startport, &portl->endport);
				if(res < 1) {
					fprintf(stderr, "Invalid port or port range, line %d\n", linenum);
					return(23);
				}
				if (res == 1) portl->endport = portl->startport;
			} while((arg = (unsigned char *)strtok((char *)NULL, ",")));
		}
		if(ch->argc - offset >= 6 && strcmp("*", (char *)ch->argv[5 + offset])) {
			arg = (unsigned char *)strtok((char *)ch->argv[5 + offset], ",");	
			do {
				if(!strcmp((char *)arg, "CONNECT")){
					acl->operation |= CONNECT;
				}
				else if(!strcmp((char *)arg, "BIND")){
					acl->operation |= BIND;
				}
				else if(!strcmp((char *)arg, "UDPASSOC")){
					acl->operation |= UDPASSOC;
				}
				else if(!strcmp((char *)arg, "ICMPASSOC")){
					acl->operation |= ICMPASSOC;
				}
				else if(!strcmp((char *)arg, "HTTP_GET")){
					acl->operation |= HTTP_GET;
				}
				else if(!strcmp((char *)arg, "HTTP_PUT")){
					acl->operation |= HTTP_PUT;
				}
				else if(!strcmp((char *)arg, "HTTP_POST")){
					acl->operation |= HTTP_POST;
				}
				else if(!strcmp((char *)arg, "HTTP_HEAD")){
					acl->operation |= HTTP_HEAD;
				}
				else if(!strcmp((char *)arg, "HTTP_OTHER")){
					acl->operation |= HTTP_OTHER;
				}
				else if(!strcmp((char *)arg, "HTTP_CONNECT")){
					acl->operation |= HTTP_CONNECT;
				}
				else if(!strcmp((char *)arg, "HTTP")){
					acl->operation |= HTTP;
				}
				else if(!strcmp((char *)arg, "HTTPS")){
					acl->operation |= HTTPS;
				}
				else if(!strcmp((char *)arg, "FTP_GET")){
					acl->operation |= FTP_GET;
				}
				else if(!strcmp((char *)arg, "FTP_PUT")){
					acl->operation |= FTP_PUT;
				}
				else if(!strcmp((char *)arg, "FTP_LIST")){
					acl->operation |= FTP_LIST;
				}
				else if(!strcmp((char *)arg, "FTP")){
					acl->operation |= FTP;
				}
				else if(!strcmp((char *)arg, "ADMIN")){
					acl->operation |= ADMIN;
				}
				else {
					fprintf(stderr, "Unknown operation type: %s line %d\n", arg, linenum);
					return(31);
				}
			} while((arg = (unsigned char *)strtok((char *)NULL, ",")));
		}
		if(ch->argc - offset >= 7){
			for(arg = ch->argv[6 + offset]; *arg;){
				int val, val1;

				if(!isnumber(*arg)) {
					arg++;
					continue;
				}
				val1 = val = (*arg - '0');
				arg++;
				if(*arg == '-' && isnumber(*(arg+1)) && (*(arg+1) - '0') > val) {
					val1 = (*(arg+1) - '0');
					arg+=2;
				}
				for(; val<=val1; val++) acl->wdays |= (1 << (val % 7));
			}
			
		}
		if(ch->argc - offset >= 8){
			for(arg = ch->argv[7 + offset]; strlen((char *)arg) >= 17 &&
							isdigit(arg[0]) &&
							isdigit(arg[1]) &&
							isdigit(arg[3]) &&
							isdigit(arg[4]) &&
							isdigit(arg[6]) &&
							isdigit(arg[7]) &&
							isdigit(arg[9]) &&
							isdigit(arg[10]) &&
							isdigit(arg[12]) &&
							isdigit(arg[13]) &&
							isdigit(arg[15]) &&
							isdigit(arg[16])
							; arg+=18){

				int t1, t2;
				struct period *sp;

				t1 = (arg[0] - '0') * 10 + (arg[1] - '0');
				t1 = (t1 * 60) + (arg[3] - '0') * 10 + (arg[4] - '0');
				t1 = (t1 * 60) + (arg[6] - '0') * 10 + (arg[7] - '0');
				t2 = (arg[9] - '0') * 10 + (arg[10] - '0');
				t2 = (t2 * 60) + (arg[12] - '0') * 10 + (arg[13] - '0');
				t2 = (t2 * 60) + (arg[15] - '0') * 10 + (arg[16] - '0');
				if(t2 < t1) break;
				sp = myalloc(sizeof(struct period));
				if(sp){
					sp->fromtime = t1;
					sp->totime = t2;
					sp->next = acl->periods;
					acl->periods = sp;
				}
				if(arg[17]!=',') break;
			}
		}
		if(acl->action == ALLOW || acl->action == DENY || acl->action == REDIRECT) {
			pthread_mutex_lock(&acl_mutex);
			if(!conf.acls[conf.aclnum]){
				conf.acls[conf.aclnum] = acl;
			}
			else {
				struct ace * acei;

				for(acei = conf.acls[conf.aclnum]; acei->next; acei = acei->next);
				acei->next = acl;
			}
			pthread_mutex_unlock(&acl_mutex);
		}
		else if (acl->action == BANDLIM || acl->action == NOBANDLIM) {
			struct bandlim * nbl;

			nbl = myalloc(sizeof(struct bandlim));
			if(!nbl) {
				fprintf(stderr, "No memory to create band limit filter\n");
				return(32);
			}
			memset(nbl, 0, sizeof(struct bandlim));
			nbl->ace = acl;
			if(acl->action == BANDLIM) {
				sscanf((char *)ch->argv[1], "%u", &nbl->rate);
				if(nbl->rate < 300) {
					fprintf(stderr, "Wrong bandwidth specified, line %d\n", linenum);
					return(33);
				}
			}
			pthread_mutex_lock(&bandlim_mutex);
			if(!strcmp((char *)ch->argv[0], "bandlimin")){
				if(!conf.bandlimiter){
					conf.bandlimiter = nbl;
				}
				else {
					struct bandlim * bli;

					for(bli = conf.bandlimiter; bli->next; bli = bli->next);
					bli->next = nbl;
				}
			}
			else {
				if(!conf.bandlimiterout){
					conf.bandlimiterout = nbl;
				}
				else {
					struct bandlim * bli;

					for(bli = conf.bandlimiterout; bli->next; bli = bli->next);
					bli->next = nbl;
				}
			}

			pthread_mutex_unlock(&bandlim_mutex);			
		}
		else if (acl->action == COUNT || acl->action == NOCOUNT) {
			struct trafcount * tl;
			tl = myalloc(sizeof(struct trafcount));
			if(!tl) {
				fprintf(stderr, "No memory to create traffic limit filter\n");
				return(32);
			}
			memset(tl, 0, sizeof(struct trafcount));
			tl->ace = acl;
	
			if(acl->action == COUNT) {
				unsigned long lim;


				tl->comment = mystrdup((char *)ch->argv[1]);
				while(isdigit(*tl->comment))tl->comment++;
				if(*tl->comment== '/')tl->comment++;

				sscanf((char *)ch->argv[1], "%u", &tl->number);
				sscanf((char *)ch->argv[3], "%lu", &lim);
				tl->traflimgb = (lim/1024);
				tl->traflim = ((lim - (tl->traflimgb*1024))*(1024*1024));
				switch(*ch->argv[2]){
					case 'c':
					case 'C':
						tl->type = DAILY;
						break;
					case 'h':
					case 'H':
						tl->type = DAILY;
						break;
					case 'd':
					case 'D':
						tl->type = DAILY;
						break;
					case 'w':
					case 'W':
						tl->type = WEEKLY;
						break;
					case 'y':
					case 'Y':
						tl->type = ANNUALLY;
						break;
					case 'm':
					case 'M':
						tl->type = MONTHLY;
						break;
					case 'n':
					case 'N':
						tl->type = NEVER;
						break;
					default:
						fprintf(stderr, "Unknown rotation type, line: %d\n", linenum);
						return(34);
				}
				if(!tl->traflim && !tl->traflimgb) {
					fprintf(stderr, "Wrong traffic limit specified, line %d\n", linenum);
					return(35);
				}
				if(tl->number != 0 && conf.counterd >= 0) {
					lseek(conf.counterd, 
						sizeof(struct counter_header) + (tl->number - 1) * sizeof(struct counter_record),
						SEEK_SET);
					memset(&crecord, 0, sizeof(struct counter_record));
					read(conf.counterd, &crecord, sizeof(struct counter_record));
					tl->traf = crecord.traf;
					tl->trafgb = crecord.trafgb;
					tl->cleared = crecord.cleared;
					tl->updated = crecord.updated;
				}
			}
			pthread_mutex_lock(&tc_mutex);
			if(!conf.trafcounter){
				conf.trafcounter = tl;
			}
			else {
				struct trafcount * ntl;

				for(ntl = conf.trafcounter; ntl->next; ntl = ntl->next);
				ntl->next = tl;
			}
			pthread_mutex_unlock(&tc_mutex);
/*
			tl->next = conf.trafcounter;
			conf.trafcounter = tl;
*/
			
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "flush") && ch->argc == 1) {	
		if (conf.aclnum<255) conf.aclnum++;
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "parent") && ch->argc >=5) {
		if(!acl || (acl->action && acl->action != 2)) {
			fprintf(stderr, "Chaining error: last ACL entry was not \"allow\" or \"redirect\" on line %d\n", linenum);
			return(41);
		}
		acl->action = 2;
		chains = NULL;
		if(!acl->chains) {
			chains = acl->chains = myalloc(sizeof(struct chain));
		}
		else {
			chains = acl->chains;
			while(chains->next)chains = chains->next;
			chains->next = myalloc(sizeof(struct chain));
			chains = chains->next;
		}
		memset(chains, 0, sizeof(struct chain));
		if(!chains){
			fprintf(stderr, "Chainig error: unable to allocate memory for chain\n");
			return(44);
		}
		chains->weight = (unsigned)atoi((char *)ch->argv[1]);
		if(chains->weight == 0 || chains->weight >1000) {
			fprintf(stderr, "Chaining error: bad chain weight %u line %d\n", chains->weight, linenum);
			return(42);
		}
		if(!strcmp((char *)ch->argv[2], "tcp"))chains->type = R_TCP;
		else if(!strcmp((char *)ch->argv[2], "http"))chains->type = R_HTTP;
		else if(!strcmp((char *)ch->argv[2], "connect"))chains->type = R_CONNECT;
		else if(!strcmp((char *)ch->argv[2], "socks4"))chains->type = R_SOCKS4;
		else if(!strcmp((char *)ch->argv[2], "socks5"))chains->type = R_SOCKS5;
		else if(!strcmp((char *)ch->argv[2], "connect+"))chains->type = R_CONNECTP;
		else if(!strcmp((char *)ch->argv[2], "socks4+"))chains->type = R_SOCKS4P;
		else if(!strcmp((char *)ch->argv[2], "socks5+"))chains->type = R_SOCKS5P;
		else if(!strcmp((char *)ch->argv[2], "socks4b"))chains->type = R_SOCKS4B;
		else if(!strcmp((char *)ch->argv[2], "socks5b"))chains->type = R_SOCKS5B;
		else if(!strcmp((char *)ch->argv[2], "pop3"))chains->type = R_POP3;
		else if(!strcmp((char *)ch->argv[2], "ftp"))chains->type = R_FTP;
		else {
			fprintf(stderr, "Chaining error: bad chain type (%s)\n", ch->argv[2]);
			return(43);
		}
		chains->redirip = getip(ch->argv[3]);
		chains->redirport = htons((unsigned short)atoi((char *)ch->argv[4]));
		if(ch->argc > 5) chains->extuser = (unsigned char *)mystrdup((char *)ch->argv[5]);
		if(ch->argc > 6) chains->extpass = (unsigned char *)mystrdup((char *)ch->argv[6]);
		continue;
		
	}
	else if(!strcmp((char *)ch->argv[0], "nserver") && ch->argc == 2) {
		for(res = 0; nservers[res] && res < MAXNSERVERS; res++);
		if(res < MAXNSERVERS) {
			nservers[res] = getip(ch->argv[1]);
		}
		resolvfunc = myresolver;
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "fakeresolve") && ch->argc == 1) {
		resolvfunc = fakeresolver;
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "nscache") && ch->argc == 2) {
		res = atoi((char *)ch->argv[1]);
		if(res < 256) {
			fprintf(stderr, "Invalid NS cache size: %d\n", res);
		}
		if(inithashtable((unsigned)res)){
			fprintf(stderr, "Failed to initialize NS cache\n");
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "nsrecord") && ch->argc == 3) {
		hashadd((unsigned char *)mystrdup((char *)ch->argv[1]), getip(ch->argv[2]), (time_t)0xffffffff);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "dialer") && ch->argc == 2) {
		demanddialprog = mystrdup((char *)ch->argv[1]);
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "monitor") && ch->argc == 2) {
		struct filemon * fm;
		fm = myalloc(sizeof (struct filemon));
		if(stat(ch->argv[1], &fm->sb)){
			myfree(fm);
			fprintf(stderr, "Warning: file %s doesn't exist on line %d\n", ch->argv[1], linenum);
		}
		else {
			fm->path = mystrdup(ch->argv[1]);
			fm->next = conf.fmon;
			conf.fmon = fm;
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "system") && ch->argc == 2) {	
		if((res = system((char *)ch->argv[1])) == -1){
			fprintf(stderr, "Failed to start %s\n", ch->argv[1]);
			return(33);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "pidfile") && ch->argc == 2) {
		FILE *pidf;
		if(!(pidf = fopen((char *)ch->argv[1], "w"))){
			fprintf(stderr, "Failed to open pid file %s\n", ch->argv[1]);
			return(34);
		}
		fprintf(pidf,"%u", (unsigned)getpid());
		fclose(pidf);
		continue;
	}
#ifndef _WIN32
	else if(!strcmp((char *)ch->argv[0], "setuid") && ch->argc == 2) {	
		res = atoi((char *)ch->argv[1]);
		if(!res || setuid(res)) {
			fprintf(stderr, "Unable to set uid %d", res);
			return(30);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "setgid") && ch->argc == 2) {	
		res = atoi((char *)ch->argv[1]);
		if(!res || setgid(res)) {
			fprintf(stderr, "Unable to set gid %d", res);
			return(31);
		}
		continue;
	}
	else if(!strcmp((char *)ch->argv[0], "chroot") && ch->argc == 2) {	
		if(!chrootp){
			char *p;
			if(chroot((char *)ch->argv[1])) {
				fprintf(stderr, "Unable to chroot %s", ch->argv[1]);
				return(32);
			}
			p = (char *)ch->argv[1] + strlen((char *)ch->argv[1]) ;
			while (p > (char *)ch->argv[1] && p[-1] == '/'){
				p--;
				*p = 0;
			}
			chrootp = strdup((char *)ch->argv[1]);
		}
		continue;
	}
#endif
	else {
		fprintf(stderr, "Unknown command: '%s' line %d\n", ch->argv[0], linenum);
		return(22);
	}
	conf.threadinit = 1;
#ifdef _WIN32
	h = CreateThread((LPSECURITY_ATTRIBUTES )NULL, 0, startsrv, (void *) ch, (DWORD)0, &thread);
	if(h)CloseHandle(h);
#else
	pthread_attr_init(&pa);
	pthread_attr_setstacksize(&pa,PTHREAD_STACK_MIN + 16384);
	pthread_attr_setdetachstate(&pa,PTHREAD_CREATE_DETACHED);
	pthread_create(&thread, &pa, startsrv, (void *)ch);
#endif
	while(conf.threadinit)usleep(SLEEPTIME);
	if(haveerror)  {
		fprintf(stderr, "Service not started on line: %d\n", linenum);
		return(40);
	}

  }
  myfree(buf);
  buf = NULL;
  myfree(args);
  args = NULL;

  return 0;

}


int main(int argc, char * argv[]) {

  int res = 0;
  FILE * fp = NULL;

#ifdef _WIN32
  unsigned char * arg;
  WSADATA wd;

  WSAStartup(MAKEWORD( 1, 1 ), &wd);
  osv.dwOSVersionInfoSize = sizeof(osv);
  GetVersionEx(&osv);
#endif

  stringtable = strings;
#ifdef _WIN32
  if((argc == 2 || argc == 3)&& !strcmp((char *)argv[1], "--install")) {

	sprintf((char *)tmpbuf, "%s will be installed and started.\n"
			"By clicking Yes you confirm you read and accepted License Agreement.\n"
			"You can use Administration/Services to control %s service.", 
			stringtable[1], stringtable[2]);
	if(MessageBox(NULL, (char *)tmpbuf, stringtable[2], MB_YESNO|MB_ICONASTERISK) != IDYES) return 1;

	
	*tmpbuf = '\"';
	if (!(res = SearchPath(NULL, argv[0], ".exe", 256, (char *)tmpbuf+1, (char **)&arg))) {
		perror("Failed to find executable filename");
		RETURN(102);
	}
	strcat((char *)tmpbuf, "\" \"");
	if(!(res = GetFullPathName ((argc == 3)?argv[2]:(char*)DEFAULTCONFIG, 256, (char *)tmpbuf+res+4, (char **)&arg))){
		perror("Failed to find config filename");
		RETURN(103);
	}
	strcat((char *)tmpbuf, "\"");
	if(osv.dwPlatformId  == VER_PLATFORM_WIN32_NT){
		SC_HANDLE sch;
		if(!(sch = OpenSCManager(NULL, NULL, GENERIC_WRITE|SERVICE_START ))){
			perror("Failed to open Service Manager");
			RETURN(101);
		}
		if (!(sch = CreateService(sch, stringtable[1], stringtable[2], GENERIC_EXECUTE, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_IGNORE, (char *)tmpbuf, NULL, NULL, NULL, NULL, NULL))){
			perror("Failed to create service");
			RETURN(103);
		}
		if (!StartService(sch, 0, NULL)) {
			perror("Failed to start service");
			RETURN(103);
		}
	}
	else {
		HKEY runsrv;
		if(RegOpenKeyEx( HKEY_LOCAL_MACHINE, 
				"Software\\Microsoft\\Windows\\CurrentVersion\\RunServices",
				0,
				KEY_ALL_ACCESS,
				&runsrv) != ERROR_SUCCESS){
			perror("Failed to open registry");
			RETURN(104);
		}
		if(RegSetValueEx(  runsrv,
				stringtable[1],
				0,
				REG_EXPAND_SZ,
				(char *)tmpbuf,
				strlen((char *)tmpbuf)+1)!=ERROR_SUCCESS){
			perror("Failed to set registry value");
			RETURN(105);
		}

	}
	return 0;
  }
  if((argc == 2 || argc == 3)&& !strcmp((char *)argv[1], "--remove")) {

	if(osv.dwPlatformId  == VER_PLATFORM_WIN32_NT){
		SC_HANDLE sch;

		if(!(sch = OpenSCManager(NULL, NULL, GENERIC_WRITE))){
			perror("Failed to open Service Manager\n");
			RETURN(106);
		}
		if (!(sch = OpenService(sch, stringtable[1], DELETE))){
			perror("Failed to open service");
			RETURN(107);
		}
		if (!DeleteService(sch)){
			perror("Failed to delete service");
			RETURN(108);
		}
	}
	else {
		HKEY runsrv;
		if(RegOpenKeyEx( HKEY_LOCAL_MACHINE, 
				"Software\\Microsoft\\Windows\\CurrentVersion\\RunServices",
				0,
				KEY_ALL_ACCESS,
				&runsrv) != ERROR_SUCCESS){
			perror("Failed to open registry");
			RETURN(109);
		}
		if(RegDeleteValue(runsrv, stringtable[1]) != ERROR_SUCCESS){
			perror("Failed to clear registry");
			RETURN(110);
		}
	}
	RETURN(0);
  }
#endif
  conffile = mystrdup((argc==2)?argv[1]:(char*)DEFAULTCONFIG);
  if(conffile && *conffile != '-') {
	fp = confopen();
#ifndef _WIN32
	if(!fp) fp = stdin;
#endif
  }
  if(argc > 2 || !(fp)) {

	fprintf(stderr, "Usage: %s [conffile]\n", argv[0]);
#ifdef _WIN32
	fprintf(stderr, "\n\t%s --install\n\tto install as service\n"
			"\n\t%s --remove\n\tto remove service\n", argv[0], argv[0]);
#endif
	fprintf(stderr, "\n%s", copyright);

	return 1;
  }

  pthread_mutex_init(&acl_mutex, NULL);
  pthread_mutex_init(&bandlim_mutex, NULL);
  pthread_mutex_init(&hash_mutex, NULL);
  pthread_mutex_init(&tc_mutex, NULL);
#ifndef NOODBC
  pthread_mutex_init(&odbc_mutex, NULL);
#endif

  res = readconfig(fp);

  if(res) RETURN(res);
  if(!writable)fclose(fp);

#ifdef _WIN32
  
  if(service){
	SERVICE_TABLE_ENTRY ste[] = 
	{
        	{ stringtable[1], (LPSERVICE_MAIN_FUNCTION)ServiceMain},
	        { NULL, NULL }
	};	
 	if(!StartServiceCtrlDispatcher( ste ))cyclestep();
  }
  else {
	cyclestep();
  }
  

#else
	 signal(SIGCONT, mysigpause);
	 signal(SIGTERM, mysigterm);
	 signal(SIGUSR1, mysigusr1);
	 signal(SIGPIPE, SIG_IGN);
	 cyclestep();

#endif

CLEARRETURN:

/*
 if(1) {
   char t[256];
   sprintf(t, "echo result:%d lines: %d>c:\\result.txt", res, linenum);
   system(t);
 }
*/
 return 0;

}
