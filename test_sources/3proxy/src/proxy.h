/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: proxy.h,v 1.45 2006/03/10 19:36:30 vlad Exp $
*/

#ifndef _3PROXY_H_
#define _3PROXY_H_
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <ctype.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/timeb.h>


#define MAXUSERNAME 128
#define _PASSWORD_LEN 256
#define MAXNSERVERS 5
#define MAXBANDLIMS 10

#define ALLOW		0
#define DENY		1
#define REDIRECT	2
#define BANDLIM		3
#define NOBANDLIM	4
#define COUNT		5
#define NOCOUNT		6

#define UDPBUFSIZE 16384
#define TCPBUFSIZE  4096


#ifdef _WIN32
#include <io.h>
#include <winsock2.h>
#define SASIZETYPE int
#define SHUT_RDWR SD_BOTH
#else
#ifndef FD_SETSIZE
#define FD_SETSIZE 4096
#endif
#include <errno.h>
#include <signal.h>
#include <sys/uio.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <pthread.h>
#include <syslog.h>
#define SASIZETYPE socklen_t
#define SOCKET int
#define INVALID_SOCKET  (-1)
#endif

#ifdef __CYGWIN__
#include <windows.h>
#define daemonize() FreeConsole()
#define SLEEPTIME 1000
#undef _WIN32
#elif _WIN32
#ifdef errno
#undef errno
#endif
#define errno WSAGetLastError()
#define EAGAIN WSAEWOULDBLOCK
#define EINTR WSAEWOULDBLOCK
#define SLEEPTIME 1
#define usleep Sleep
#define pthread_self GetCurrentThreadId
#define getpid GetCurrentProcessId
#define pthread_t DWORD
#define daemonize() FreeConsole()
#define socket(x, y, z) WSASocket(x, y, z, NULL, 0, 0)
#define accept(x, y, z) WSAAccept(x, y, z, NULL, 0)
#define pthread_mutex_t CRITICAL_SECTION
#define pthread_mutex_init(x, y) InitializeCriticalSection(x)
#define pthread_mutex_lock(x) EnterCriticalSection(x)
#define pthread_mutex_unlock(x) LeaveCriticalSection(x)
#define pthread_mutex_destroy(x) DeleteCriticalSection(x)
#define ftruncate chsize
#else
#include <pthread.h>
#ifndef PTHREAD_STACK_MIN
#define PTHREAD_STACK_MIN 32768
#endif
#define daemonize() daemon(1,1)
#define SLEEPTIME 1000
#ifndef O_BINARY
#define O_BINARY 0
#endif
#endif

#ifndef NOODBC
#ifndef _WIN32
#include <sqltypes.h>
#endif
#include <sql.h>
#include <sqlext.h>
#endif

#ifdef WITH_POLL
#include <poll.h>
#else
struct mypollfd {
 SOCKET    fd;       /* file descriptor */
 short  events;   /* events to look for */
 short  revents;  /* events returned */
};
int  mypoll(struct mypollfd *fds, unsigned int nfds, int timeout);
#ifndef POLLIN
#define POLLIN 1
#endif
#ifndef POLLOUT
#define POLLOUT 2
#endif
#ifndef POLLPRI
#define POLLPRI 4
#endif
#ifndef POLLERR
#define POLLERR 8
#endif
#ifndef POLLHUP
#define POLLHUP 16
#endif
#ifndef POLLNVAL
#define POLLNVAL 32
#endif
#define pollfd mypollfd
#define poll mypoll

#endif

#ifndef _WIN32
#define closesocket close
extern pthread_attr_t pa;
#endif

#ifndef SOCKET_ERROR
#define SOCKET_ERROR -1
#endif

#ifndef isnumber
#define isnumber(n) (n >= '0' && n <= '9')
#endif

#ifndef ishex
#define ishex(n) ((n >= '0' && n <= '9') || (n >= 'a' && n<='f') || (n >= 'A' && n <= 'F'))
#endif

#define isallowed(n) ((n >= '0' && n <= '9') || (n >= 'a' && n <= 'z') || (n >= 'A' && n <= 'Z') || (n >= '*' && n <= '/') || n == '_')

#ifdef _WIN32
extern int strnicmp( const char *string1, const char *string2, size_t count );
extern int stricmp( const char *string1, const char *string2 );
#define strcasecmp stricmp
#define strncasecmp strnicmp
#endif

typedef enum {
	CLIENT,
	SERVER
} DIRECTION;

typedef enum {
	S_NOSERVICE,
	S_PROXY,
	S_TCPPM,
	S_POP3P,
	S_SOCKS4 = 4,	/* =4 */
	S_SOCKS5 = 5,	/* =5 */
	S_UDPPM,
	S_SOCKS,
	S_SOCKS45,
	S_ADMIN,
	S_DNSPR,
	S_FTPPR,
	S_ZOMBIE
}PROXYSERVICE;

struct clientparam;
typedef void (*LOGFUNC)(struct clientparam * param, const unsigned char *);
typedef int (*AUTHFUNC)(struct clientparam * param);
typedef void * (*REDIRECTFUNC)(void *);
typedef unsigned long (*RESOLVFUNC)(unsigned char *);
typedef unsigned (*BANDLIMFUNC)(struct clientparam * param, unsigned nbytesin, unsigned nbytesout);
typedef void (*TRAFCOUNTFUNC)(struct clientparam * param);

extern RESOLVFUNC resolvfunc;

struct iplist {
	struct iplist *next;
	unsigned long ip;
	unsigned long mask;
};

struct portlist {
	struct portlist * next;
	unsigned short startport;
	unsigned short endport;
};

struct userlist {
	struct userlist * next;
	unsigned char * user;
};

typedef enum {
	SYS,
	CL,
	CR,
	NT,
	LM
}PWTYPE;

struct passwords {
	struct passwords *next;
	unsigned char * user;
	unsigned char * password;
	int pwtype;
};

typedef enum {
	R_TCP,
	R_CONNECT,
	R_SOCKS4,
	R_SOCKS5,
	R_HTTP,
	R_POP3,
	R_FTP,
	R_CONNECTP,
	R_SOCKS4P,
	R_SOCKS5P,
	R_SOCKS4B,
	R_SOCKS5B
} REDIRTYPE;

struct chain {
	struct chain * next;
	int type;
	unsigned long redirip;
	unsigned short redirport;
	unsigned short weight;
	unsigned char * extuser;
	unsigned char * extpass;
};

struct period {
	int fromtime;
	int totime;
	struct period *next;
};

extern int wday;
extern time_t basetime;
extern int timetoexit;



#define CONNECT 	0x00000001
#define BIND		0x00000002
#define UDPASSOC	0x00000004
#define ICMPASSOC	0x00000008	/* reserved */
#define HTTP_GET	0x00000100
#define HTTP_PUT	0x00000200
#define HTTP_POST	0x00000400
#define HTTP_HEAD	0x00000800
#define HTTP_CONNECT	0x00001000
#define HTTP_OTHER	0x00008000
#define HTTP		0x0000EF00	/* all except HTTP_CONNECT */
#define HTTPS		HTTP_CONNECT
#define FTP_GET		0x00010000
#define FTP_PUT		0x00020000
#define FTP_LIST	0x00040000
#define FTP_DATA	0x00080000
#define FTP		0x000F0000
#define DNSRESOLVE	0x00100000
#define ADMIN		0x01000000

struct ace {
	struct ace *next;
	int action;
	int operation;
	int wdays;
	struct period *periods;
	struct userlist *users;
	struct iplist *src, *dst;
	struct portlist *ports;
	struct chain *chains;
};

struct bandlim {
	struct bandlim *next;
	struct ace *ace;
	unsigned basetime;
	unsigned rate;
	unsigned nexttime;
};

typedef enum {NONE, MINUTELY, HOURLY, DAILY, WEEKLY, MONTHLY, ANNUALLY, NEVER} ROTATION;

struct trafcount {
	struct trafcount *next;
	struct ace *ace;
	unsigned number;
	ROTATION type;
	unsigned long traf;
	unsigned long trafgb;
	unsigned long traflim;
	unsigned long traflimgb;
	time_t cleared;
	time_t updated;
	char * comment;
	int disabled;
};

typedef enum {
	PASS,
	CHANGED,
	SKIP,
	REJECT
} FILTER_ACTION;


struct clientparam {
	struct clientparam *next;
	struct clientparam *prev;
	struct clientparam *child;
	struct clientparam *parent;
	PROXYSERVICE service;
	LOGFUNC logfunc;
	AUTHFUNC authfunc;
	REDIRECTFUNC redirectfunc;
	BANDLIMFUNC bandlimfunc;
	TRAFCOUNTFUNC trafcountfunc;
	SOCKET srvsock;
	int * childcount;
	SOCKET clisock;
	SOCKET remsock;
	SOCKET ctrlsock;
	REDIRTYPE redirtype;
	FILE *stdlog;
	int version;
	int redirected;
	int operation;
	unsigned char * target;
	unsigned short targetport;
	struct pollfd * srvfds;
	unsigned long intip;
	unsigned long extip;
	unsigned short intport;
	unsigned short extport;
	int aclnum;
	unsigned char * hostname;
	unsigned char * username;
	unsigned char * password;
	unsigned char * extusername;
	unsigned char * extpassword;
	unsigned char * clibuf;
	unsigned char * srvbuf;
	unsigned cliinbuf;
	unsigned srvinbuf;
	unsigned clioffset;
	unsigned srvoffset;
	unsigned clibufsize;
	unsigned srvbufsize;
	unsigned bufsize;
	struct sockaddr_in sinc;
	struct sockaddr_in sins;
	int res;
	unsigned long statscli;
	unsigned long statssrv;
	int singlepacket;
	int waitclient;
	int waitserver;
	unsigned char * logformat;
	unsigned char * logtarget;
	pthread_mutex_t * counter_mutex;
	struct bandlim * bandlims[MAXBANDLIMS];
	struct bandlim * bandlimsout[MAXBANDLIMS];
	unsigned char * nonprintable;
	int usentlm;
	int pwtype;
	int threadid;
	time_t time_start;
	unsigned maxtraf;
	unsigned msec_start;
	unsigned short reqport;
	unsigned char replace;
};

struct filemon {
	char * path;
	struct stat sb;
	struct filemon *next;
};


struct extparam {
	int timeouts[10];
	struct ace * acls[256];
	char * conffile;
	struct bandlim * bandlimiter;
	struct bandlim * bandlimiterout;
	struct trafcount * trafcounter;
	struct clientparam *services;
	int threadinit;
	int counterd;
	int haveerror;
	int rotate;
	int paused;
	unsigned char *logname, **archiver;
	ROTATION logtype;
	ROTATION countertype;
	char * counterfile;
	int archiverc;
	char* demanddialprog;
	int demon;
	int maxchild;
	unsigned long intip;
	unsigned long extip;
	unsigned short intport;
	unsigned short extport;
	int singlepacket;
	int aclnum;
	struct passwords *pwl;
	AUTHFUNC authfunc;
	LOGFUNC logfunc;
	BANDLIMFUNC bandlimfunc;
	unsigned char * logformat;
	unsigned bufsize;
	struct filemon * fmon;
};

typedef enum {
	UNKNOWN,
	INTTABLE,
	INTPAIRTABLE,
	STRINGTABLE,
	STRINGPAIRTABLE,
	STRINGREPLACETABLE,
	IPTABLE,
	IPRANGETABLE,
	ACLTABLE
}TABLETYPE;

struct datatable {
	TABLETYPE type;
	int	n;
	void * data;
};


extern struct extparam conf;


int sockmap(struct clientparam * param, int timeo);
int socksend(SOCKET sock, unsigned char * buf, int bufsize, int to);
int socksendto(SOCKET sock, struct sockaddr_in * sin, unsigned char * buf, int bufsize, int to);
int sockrecvfrom(SOCKET sock, struct sockaddr_in * sin, unsigned char * buf, int bufsize, int to);


int sockgetcharcli(struct clientparam * param, int timeosec, int timeousec);
int sockgetcharsrv(struct clientparam * param, int timeosec, int timeousec);
int sockgetlinebuf(struct clientparam * param, DIRECTION which, unsigned char * buf, int bufsize, int delim, int to);




int dobuf(struct clientparam * param, unsigned char * buf, const unsigned char *s, const unsigned char * doublec);
extern FILE * stdlog;
void logstdout(struct clientparam * param, const unsigned char *s);
void logsyslog(struct clientparam * param, const unsigned char *s);
void lognone(struct clientparam * param, const unsigned char *s);
#ifndef NOSQL
void logsql(struct clientparam * param, const unsigned char *s);
int init_sql(char * s);
void close_sql();
#endif
int doconnect(struct clientparam * param);
int nbnameauth(struct clientparam * param);
int alwaysauth(struct clientparam * param);
int ipauth(struct clientparam * param);
int strongauth(struct clientparam * param);

int scanaddr(const unsigned char *s, unsigned long * ip, unsigned long * mask);
int myinet_ntoa(struct in_addr in, char * buf);
extern unsigned long nservers[MAXNSERVERS];
unsigned long getip(unsigned char *name);
unsigned long myresolver(unsigned char *);
unsigned long fakeresolver (unsigned char *name);
int inithashtable(unsigned nhashsize);
void hashadd(const unsigned char* name, unsigned long value, time_t expires);
void freeparam(struct clientparam * param);
void clearstat(struct clientparam * param);

typedef void * (* PROXYFUNC)(void * data);

struct proxydef {
	PROXYFUNC pf;
	unsigned short port;
	int isudp;
	int service;
	char * helpmessage;
};

extern struct proxydef childdef;

extern char* demanddialprog;
int reload (void);
extern int paused;
extern int demon;

typedef enum {
	SINGLEBYTE_S,
	SINGLEBYTE_L,
	STRING_S,
	STRING_L,
	CONNECTION_S,
	CONNECTION_L,
	DNS_TO,
	CHAIN_TO
}TIMEOUT;


unsigned char * mycrypt(const unsigned char *key, const unsigned char *salt, unsigned char *buf);
unsigned char * ntpwdhash (unsigned char *szHash, const unsigned char *szPassword, int tohex);
int de64 (const unsigned char *in, unsigned char *out, int maxlen);
unsigned char* en64 (const unsigned char *in, unsigned char *out, int inlen);
void tohex(unsigned char *in, unsigned char *out, int len);
void fromhex(unsigned char *in, unsigned char *out, int len);



extern unsigned char **stringtable;

int ftplogin(struct clientparam *param, char *buf, int *inbuf);
int ftpcd(struct clientparam *param, unsigned char* path, char *buf, int *inbuf);
int ftpsyst(struct clientparam *param, unsigned char *buf, unsigned len);
int ftppwd(struct clientparam *param, unsigned char *buf, unsigned len);
int ftptype(struct clientparam *param, unsigned char* f_type);
int ftpres(struct clientparam *param, unsigned char * buf, int len);
SOCKET ftpcommand(struct clientparam *param, unsigned char * command, unsigned char  *arg);


int text2unicode(const char * text, char * buf, int buflen);
void unicode2text(const char *unicode, char * buf, int len);
void genchallenge(struct clientparam *param, char * challenge, char *buf);
void mschap(const unsigned char *win_password,
		 const unsigned char *challenge, unsigned char *response);


int parsehostname(char *hostname, struct clientparam *param, unsigned short port);
int parseusername(char *username, struct clientparam *param, int extpasswd);
int parseconnusername(char *username, struct clientparam *param, int extpasswd, unsigned short port);
int ACLmatches(struct ace* acentry, struct clientparam * param);

int myrand(void * entropy, int len);

#ifdef WITH_STD_MALLOC

#define myalloc malloc
#define myfree free
#define myrealloc realloc
#define mystrdup strdup

#else

void *myalloc(size_t size);
void myfree(void *ptr);
void *myrealloc(void *ptr, size_t size);
char * mystrdup(const char *str);

#endif

extern char *copyright;


#define SERVICES 5

void * dnsprchild(void * data);
void * pop3pchild(void * data);
void * proxychild(void * data);
void * sockschild(void * data);
void * tcppmchild(void * data);
void * udppmchild(void * data);
void * adminchild(void * data);
void * ftpprchild(void * data);


struct datatype;
struct dictionary;
struct node;
struct property;

typedef void * (*EXTENDFUNC) (struct node *node);
typedef void (*CBFUNC)(void *cb, char * buf, int inbuf);
typedef void (*PRINTFUNC) (struct node *node, CBFUNC cbf, void*cb);

extern pthread_mutex_t bandlim_mutex;
extern pthread_mutex_t hash_mutex;
extern pthread_mutex_t acl_mutex;
extern pthread_mutex_t tc_mutex;
#ifndef NOODBC
extern pthread_mutex_t odbc_mutex;
#endif


struct property {
	struct property * next;
	char * name;
	EXTENDFUNC e_f;
	int type;
};

struct datatype {
	char * type;
	EXTENDFUNC i_f;
	PRINTFUNC p_f;
	struct property * properties;
};

struct node {
	void * value;
	void * iteration;
	struct node * parent;
	int type;
};

struct dictionary {
	char * name;
	struct node * node;
	EXTENDFUNC array_f;
	int arraysize;
};


extern struct datatype datatypes[64];

typedef enum {
	TYPE_INTEGER,
	TYPE_SHORT,
	TYPE_CHAR,
	TYPE_UNSIGNED,
	TYPE_TRAFFIC,
	TYPE_PORT,
	TYPE_IP,
	TYPE_CIDR,
	TYPE_STRING,
	TYPE_DATETIME,
	TYPE_OPERATIONS,
	TYPE_ROTATION,
	TYPE_PORTLIST,
	TYPE_IPLIST,
	TYPE_USERLIST,
	TYPE_PWLIST,
	TYPE_CHAIN,
	TYPE_ACE,
	TYPE_BANDLIMIT,
	TYPE_TRAFCOUNTER,
	TYPE_CLIENT,
	TYPE_WEEKDAYS,
	TYPE_TIME,
	TYPE_PERIOD
}DATA_TYPE;


#define COPYRIGHT "(c)2000-2006 3APA3A, Vladimir Dubrovin & Security.Nnov\n"\
		 "Documentation and sources: http://www.security.nnov.ru/soft/3proxy/\n"\
		 "Please read license agreement in \'copying\' file.\n"\
		 "You may not use this program without accepting license agreement"


#define WEBBANNERS 30

#endif

