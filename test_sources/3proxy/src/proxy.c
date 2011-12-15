/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: proxy.c,v 1.45 2006/09/05 14:42:11 vlad Exp $
*/


#include "proxy.h"

#define RETURN(xxx) { param->res = xxx; goto CLEANRET; }

char * proxy_stringtable[] = {
	"HTTP/1.0 400 Bad Request\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>400 Bad Request</title></head>\r\n"
	"<body><h2>400 Bad Request</h2></body></html>\r\n",

	"HTTP/1.0 502 Bad Gateway\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>502 Bad Gateway</title></head>\r\n"
	"<body><h2>502 Bad Gateway</h2><h3>Host Not Found or connection failed</h3></body></html>\r\n",

	"HTTP/1.0 503 Service Unavailable\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>503 Service Unavailable</title></head>\r\n"
	"<body><h2>503 Service Unavailable</h2><h3>You have exceeded your traffic limit</h3></body></html>\r\n",

	"HTTP/1.0 503 Service Unavailable\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>503 Service Unavailable</title></head>\r\n"
	"<body><h2>503 Service Unavailable</h2><h3>Recursion detected</h3></body></html>\r\n",

	"HTTP/1.0 501 Not Implemented\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>501 Not Implemented</title></head>\r\n"
	"<body><h2>501 Not Implemented</h2><h3>Required action is not supported by proxy server</h3></body></html>\r\n",

	"HTTP/1.0 502 Bad Gateway\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>502 Bad Gateway</title></head>\r\n"
	"<body><h2>502 Bad Gateway</h2><h3>Failed to connect parent proxy</h3></body></html>\r\n",

	"HTTP/1.0 500 Internal Error\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>500 Internal Error</title></head>\r\n"
	"<body><h2>500 Internal Error</h2><h3>Internal proxy error during processing your request</h3></body></html>\r\n",

	"HTTP/1.0 407 Proxy Authentication Required\r\n"
	"Proxy-Authenticate: Basic realm=\"proxy\"\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>407 Proxy Authentication Required</title></head>\r\n"
	"<body><h2>407 Proxy Authentication Required</h2><h3>Access to requested resource disallowed by administrator or you need valid username/password to use this resource</h3></body></html>\r\n",

	"HTTP/1.0 200 Connection established\r\n\r\n",

	"HTTP/1.0 200 Connection established\r\n"
	"Content-Type: text/html\r\n\r\n",

	"HTTP/1.0 404 Not Found\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>404 Not Found</title></head>\r\n"
	"<body><h2>404 Not Found</h2><h3>File not found</body></html>\r\n",
	
	"HTTP/1.0 403 Forbidden\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>403 Access Denied</title></head>\r\n"
	"<body><h2>403 Access Denied</h2><h3>Access control list denies you to access this resource</body></html>\r\n",

	"HTTP/1.0 407 Proxy Authentication Required\r\n"
	"Proxy-Authenticate: NTLM\r\n"
	"Proxy-Authenticate: basic realm=\"proxy\"\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<html><head><title>407 Proxy Authentication Required</title></head>\r\n"
	"<body><h2>407 Proxy Authentication Required</h2><h3>Access to requested resource disallowed by administrator or you need valid username/password to use this resource</h3></body></html>\r\n",

	"HTTP/1.0 407 Proxy Authentication Required\r\n"
	"Proxy-Connection: keep-alive\r\n"
	"Content-Length: 0\r\n"
	"Proxy-Authenticate: NTLM ",

	"HTTP/1.0 403 Forbidden\r\n"
	"Proxy-Connection: close\r\n"
	"Content-type: text/html; charset=us-ascii\r\n"
	"\r\n"
	"<pre>",

};

struct datatable proxy_table = {
	STRINGTABLE,
	sizeof(proxy_stringtable)/sizeof(unsigned char *),
	(void *)proxy_stringtable
};

#define BUFSIZE 4096
#define LINESIZE 2048

void logurl(struct clientparam * param, char * req, int ftp){
 char *sb;
 char *se;
 char buf[LINESIZE];

 if(req) {
	strcpy(buf,req);
	sb = strchr(buf, '\r');
	if(sb)*sb = 0;
	if(ftp && (se = strchr(buf + 10, ':')) && (sb = strchr(se, '@')) ) {
		strcpy(se, sb);
	}
 }
 if(param->res != 555)(*param->logfunc)(param, (unsigned char *)(req?buf:NULL));
}

void decodeurl(unsigned char *s, int allowcr){
 unsigned char *d = s;
 unsigned u;

 while(*s){
	if(*s == '%' && ishex(s[1]) && ishex(s[2])){
		sscanf((char *)s+1, "%2x", &u);
		if(allowcr && u != '\r')*d++ = u;
		else if (u != '\r' && u != '\n') {
			if (u == '\"' || u == '\\') *d++ = '\\';
			else if (u == 255) *d++ = 255;
			*d++ = u;
		}
		s+=3;
	}
	else if(!allowcr && *s == '?') {
		break;
	}
	else if(*s == '+') {
		*d++ = ' ';
		s++;
	}
	else {
		*d++ = *s++;
	}
 }
 *d = 0;
}

void file2url(unsigned char *sb, unsigned char *buf, unsigned bufsize, int * inbuf, int skip255){
 for(; *sb; sb++){
	if((bufsize - *inbuf)<16)break;
	if(*sb=='\r'||*sb=='\n')continue;
	if(isallowed(*sb))buf[(*inbuf)++]=*sb;
	else if(*sb == '\"'){
		memcpy(buf+*inbuf, "%5C%22", 6);
		(*inbuf)+=6;
	}
	else if(skip255 && *sb == 255 && *(sb+1) == 255) {
		memcpy(buf+*inbuf, "%ff", 3);
		(*inbuf)+=3;
		sb++;
        }
	else {
		sprintf((char *)buf+*inbuf, "%%%.2x", (unsigned)*sb);
		(*inbuf)+=3;
	}
 }
}


void * proxychild(void * data) {
#define param ((struct clientparam*)data)
 int res=0, i=0;
 unsigned char* buf = NULL, *newbuf;
 unsigned int inbuf;
 unsigned bufsize, reqlen = 0;
 unsigned char	*sb=NULL, *sg=NULL, *se=NULL, *sp=NULL,
		*req=NULL, *su=NULL, *ss = NULL;
 unsigned char *ftpbase=NULL;
 unsigned char username[1024];
 int keepalive = 0;
 int contentlength = -1;
 int isconnect = 0;
 int transparent = 0;
 int redirect = 0;
 int prefix = 0, ckeepalive=0;
 int ftp = 0;
 int anonymous;
 int sleeptime = 0;
 SOCKET ftps;
 SASIZETYPE sasize;



 
 if(!(buf = myalloc(BUFSIZE))) {RETURN(21);}
 bufsize = BUFSIZE;
 anonymous = param->singlepacket;
for(;;){
 memset(buf, 0, BUFSIZE);
 inbuf = 0;
 i = sockgetlinebuf(param, CLIENT, buf, LINESIZE - 1, '\n', (keepalive)?conf.timeouts[CONNECTION_S]:conf.timeouts[STRING_S]);
 if(i<=0) {
	RETURN(keepalive?555:509);
 }
 if (i==2 && buf[0]=='\r' && buf[1]=='\n') continue;
 buf[i] = 0;
 if(req) {
	if(i<=prefix || strncasecmp((char *)buf, (char *)req, prefix)){
		ckeepalive = 0;
		if(param->remsock != INVALID_SOCKET){
			shutdown(param->remsock, SHUT_RDWR);
			closesocket(param->remsock);
		}
		param->sins.sin_addr.s_addr = 0;
		param->sins.sin_port = 0;
		param->remsock = INVALID_SOCKET;
	}
 	else if(ckeepalive && param->remsock!= INVALID_SOCKET){
		struct pollfd fds;

		fds.fd = param->remsock;
		fds.events = POLLIN;
		res = poll(&fds, 1, 0);
		if(res) {
			ckeepalive = 0;
			if(param->remsock != INVALID_SOCKET){
				shutdown(param->remsock, SHUT_RDWR);
				closesocket(param->remsock);
			}
			param->sins.sin_addr.s_addr = 0;
			param->sins.sin_port = 0;
			param->remsock = INVALID_SOCKET;
		}
	}
	myfree(req);
 }
 req = (unsigned char *)mystrdup((char *)buf);
 if(!req){RETURN(510);}
 if(i<10) {
	RETURN(511);
 }
 if(!strncasecmp((char *)buf, "CONNECT", 7))isconnect = 1;
 if ((sb=(unsigned char *)(unsigned char *)strchr((char *)buf, ' ')) == NULL) {RETURN(512);}
 ss = ++sb;
 if(!isconnect) {
	if (!strncasecmp((char *)sb, "http://", 7)) {
		sb += 7;
	}
	else if (!strncasecmp((char *)sb, "ftp://", 6)) {
		ftp = 1;
		sb += 6;
	}
	else if(*sb == '/') {
		transparent = 1;
	}
	else {
		RETURN (513);
	}
 }
 else {
	 if ((se=(unsigned char *)(unsigned char *)strchr((char *)sb, ' ')) == NULL || sb==se) {RETURN (514);}
	 *se = 0;
 }
 if(!transparent) {
	if(!isconnect) {
		if ((se=(unsigned char *)(unsigned char *)strchr((char *)sb, '/')) == NULL 
			|| sb==se
			|| !(sg=(unsigned char *)strchr((char *)sb, ' '))) {RETURN (515);}
		if(se > sg) se=sg;
 		*se = 0;
	}
	prefix = (int)(se - buf);
	su = (unsigned char*)strchr((char *)sb, '@');
	if(su) parseconnusername((char *)sb, (struct clientparam *)param, (int)1, (unsigned short)((ftp)?21:80));
	else parsehostname((char *)sb, (struct clientparam *)param, (unsigned short)((ftp)? 21:80));
	if(!isconnect){
		if(se==sg)*se-- = ' ';
		*se = '/';
		memcpy(ss, se, i - (se - sb) + 1);
	}
 }
 reqlen = i = (int)strlen((char *)buf);
 if(!strncasecmp((char *)buf, "CONNECT", 7))param->operation = HTTP_CONNECT;
 else if(!strncasecmp((char *)buf, "GET", 3))param->operation = (ftp)?FTP_GET:HTTP_GET;
 else if(!strncasecmp((char *)buf, "PUT", 3))param->operation = (ftp)?FTP_PUT:HTTP_PUT;
 else if(!strncasecmp((char *)buf, "POST", 4))param->operation = HTTP_POST;
 else if(!strncasecmp((char *)buf, "HEAD", 4))param->operation = HTTP_HEAD;
 else param->operation = HTTP_OTHER;
 do {
	buf[inbuf+i]=0;
/*printf("Got: %s\n", buf+inbuf);*/
#ifndef WITHMAIN
	if(i > 25 && (!strncasecmp((char *)(buf+inbuf), "proxy-authorization", 19))){
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		if(!*sb) continue;
		if(!strncasecmp((char *)sb, "basic", 5)){
			sb+=5;
			while(isspace(*sb))sb++;
			i = de64(sb, username, 255);
			if(i<=0)continue;
			username[i] = 0;
			sb = (unsigned char *)strchr((char *)username, ':');
			if(sb){
				*sb = 0;
				if(param->password)myfree(param->password);
				param->password = (unsigned char *)mystrdup((char *)sb+1);
				param->pwtype = 0;
			}
			if(param->username)myfree(param->username);
			param->username = (unsigned char *)mystrdup((char *)username);
			continue;
		}
		if(!strncasecmp((char *)sb, "ntlm", 4)){
			sb+=4;
			while(isspace(*sb))sb++;
			i = de64(sb, username, 1023);
			if(i<=16)continue;
			username[i] = 0;
			if(strncasecmp((char *)username, "NTLMSSP", 8)) continue;
			if(username[8] == 1) {
				while( (i = sockgetlinebuf(param, CLIENT, buf, BUFSIZE - 1, '\n', conf.timeouts[STRING_S])) > 2){
					if(i> 15 && (!strncasecmp((char *)(buf), "content-length", 14))){
						buf[i]=0;
						contentlength = atoi(buf + 15);
					}
				}
				if(contentlength){
					while( contentlength > 0 && (i = sockgetlinebuf(param, CLIENT, buf, (BUFSIZE < contentlength)? BUFSIZE - 1:contentlength, '\n', conf.timeouts[STRING_S])) > 0){
						contentlength-=i;
					}
				}
				if(param->password)myfree(param->password);
				param->password = myalloc(32);
				param->pwtype = 2;
				i = (int)strlen(proxy_stringtable[13]);
				memcpy(buf, proxy_stringtable[13], i);
				genchallenge(param, (char *)param->password, (char *)buf + i);
				memcpy(buf + strlen((char *)buf), "\r\n\r\n", 5);
				socksend(param->clisock, buf, (int)strlen((char *)buf), conf.timeouts[STRING_S]);
				ckeepalive = keepalive = 1;
				goto REQUESTEND;
			}
			if(username[8] == 3 && param->pwtype == 2 && i>=80) {
				unsigned offset, len;

				len = username[20] + (((unsigned)username[21]) << 8);
				offset = username[24] + (((unsigned)username[25]) << 8);
				if(len != 24 || len + offset > (unsigned)i) continue;
				memcpy(param->password + 8, username + offset, 24);
				len = username[36] + (((unsigned)username[37]) << 8);
				offset = username[40] + (((unsigned)username[41]) << 8);
				if(len> 255 || len + offset > (unsigned)i) continue;
				if(param->username) myfree(param->username);
				unicode2text((char *)username+offset, (char *)username+offset, (len>>1));
				param->username = (unsigned char *)mystrdup((char *)username+offset);
			}
			continue;
		}
	}
#endif
	if(!isconnect && (
			(i> 25 && !transparent && !strncasecmp((char *)(buf+inbuf), "proxy-connection:", 17))
			||
			(i> 16 && transparent && (!strncasecmp((char *)(buf+inbuf), "connection:", 11)))
			)){
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		if(!strncasecmp((char *)sb,"keep-alive", 10))keepalive = 1;
		continue; 
	}
	if(transparent && i > 6 && !strncasecmp((char *)buf + inbuf, "Host:", 5)){
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		se = (unsigned char *)strchr((char *)sb, '\r');
		if(se) *se = 0;
		if(!ckeepalive){
			parsehostname((char *)sb, param, param->reqport?ntohs(param->reqport):80);
		}
		newbuf = myalloc(strlen((char *)req) + strlen((char *)(buf+inbuf)) + 8);
		if(newbuf){
			sp = (unsigned char *)strchr((char *)req+1, '/');
			memcpy(newbuf, req, (sp - req));
			sprintf((char*)newbuf + (sp - req), "http://%s%s",sb,sp);
			myfree(req);
			req = newbuf;
		}
		if(se)*se = '\r';
	}
	if(i> 15 && (!strncasecmp((char *)(buf+inbuf), "content-length", 14))){
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		contentlength = atoi((char *)sb);
		if(contentlength < 0)contentlength = 0;
	}
	inbuf += i;
	if((bufsize - inbuf) < LINESIZE){
		if (bufsize > 20000){
			RETURN (516);
		}
		if(!(newbuf = myrealloc(buf, bufsize + BUFSIZE))){RETURN (21);}
		buf = newbuf;
		bufsize += BUFSIZE;
	}
 } while( (i = sockgetlinebuf(param, CLIENT, buf + inbuf, LINESIZE - 2, '\n', conf.timeouts[STRING_S])) > 2);

 buf[inbuf] = 0;

 if((res = (*param->authfunc)(param))) {RETURN(res);}
 if (param->sins.sin_addr.s_addr == param->intip && param->sins.sin_port == param->intport) {
	RETURN(519);
 }
 sasize = sizeof(struct sockaddr_in);
 if(getpeername(param->remsock, (struct sockaddr *)&param->sins, &sasize)){
	RETURN(520);
 }
#define FTPBUFSIZE 2048
 if(ftp && param->redirtype != R_HTTP){
	SOCKET s;
	int mode = 0;
	int i=0;
	char ftpbuf[FTPBUFSIZE];
	int inftpbuf = 0;

	if(!ckeepalive){
		inftpbuf = FTPBUFSIZE - 20;
		res = ftplogin(param, ftpbuf, &inftpbuf);
		if(res){
			if (res == 700 || res == 701){
				socksend(param->clisock, (unsigned char *)proxy_stringtable[14], (int)strlen(proxy_stringtable[14]), conf.timeouts[STRING_S]);
				socksend(param->clisock, (unsigned char *)ftpbuf, inftpbuf, conf.timeouts[STRING_S]);
			}
			RETURN(res);
		}
	}
	ckeepalive = 1;
	if(ftpbase) myfree(ftpbase);
	ftpbase = NULL;
	if(!(sp = (unsigned char *)strchr((char *)ss, ' '))){RETURN(799);}
	*sp = 0;

	decodeurl(ss, 0);
	i = (int)strlen((char *)ss);
	if(!(ftpbase = myalloc(i+2))){RETURN(21);}
	memcpy(ftpbase, ss, i);
	if(ftpbase[i-1] != '/') ftpbase[i++] = '/';
	ftpbase[i] = 0;
	memcpy(buf, "<pre><hr>\n", 10);
	inbuf = 10;
	if(inftpbuf) {
		memcpy(buf+inbuf, ftpbuf, inftpbuf);
		inbuf += inftpbuf;
		memcpy(buf+inbuf, "<hr>", 4);
		inbuf += 4;
	}
	if(ftpbase[1] != 0){
		memcpy(buf+inbuf, "[<A HREF=\"..\">..</A>]\n", 22);
		inbuf += 22;
	}
	inftpbuf = FTPBUFSIZE - (20 + inftpbuf);
	res = ftpcd(param, ftpbase, ftpbuf, &inftpbuf);
	if(res){
		res = ftptype(param, (unsigned char *)"I");
		if(res)RETURN(res);
		ftpbase[--i] = 0;
		ftps = ftpcommand(param, (unsigned char *)"RETR", ftpbase);
	}
	else {
		if(inftpbuf){
			memcpy(buf+inbuf, ftpbuf, inftpbuf);
			inbuf += inftpbuf;
			memcpy(buf+inbuf, "<hr>", 4);
			inbuf += 4;
		}
		ftps = ftpcommand(param, (unsigned char *)"LIST", NULL);
		mode = 1;
	}
	if(ftps == INVALID_SOCKET){RETURN(780);}
	if(!mode){
		socksend(param->clisock, (unsigned char *)proxy_stringtable[8], (int)strlen(proxy_stringtable[8]), conf.timeouts[STRING_S]);
		s = param->remsock;
		param->remsock = ftps;
		res = sockmap(param, conf.timeouts[CONNECTION_L]);
		closesocket(ftps);
		ftps = INVALID_SOCKET;
		param->remsock = s;
	}
	else {
		int headsent = 0;
		s = param->remsock;
		param->remsock = ftps;
		for(; (res = sockgetlinebuf(param, SERVER, (unsigned char *)ftpbuf, FTPBUFSIZE - 20, '\n', conf.timeouts[STRING_S])) > 0; i++){
			int isdir = 0;
			int islink = 0;
			int filetoken =-1;
			int sizetoken =-1;
			int modetoken =-1;
			int datetoken =-1;
			int spaces = 1;
			unsigned char * tokens[10];
			unsigned wordlen [10];
			unsigned char j=0;
			int space = 1;

			param->statssrv += res;
			ftpbuf[res] = 0;
			if(!i && ftpbuf[0] == 't' && ftpbuf[1] == 'o' && ftpbuf[2] == 't'){
				mode = 2;
				continue;
			}
			if(!isnumber(*ftpbuf) && mode == 1) mode = 2;
			for(sb=(unsigned char *)ftpbuf; *sb; sb++){
				if(!space && isspace(*sb)){
					space = 1;
					wordlen[j]=(unsigned)(sb-tokens[j]);
					j++;
				}
				if(space && !isspace(*sb)){
					space = 0;
					tokens[j] = sb;
					if(j==8)break;
				}				
			}
			if(mode == 1){
				if(j < 4) continue;
				if(!(isdir = !memcmp(tokens[2], "<DIR>", wordlen[2])) && !isnumber(*tokens[2])){
					continue;
				}
				datetoken = 0;
				wordlen[datetoken] = ((unsigned)(tokens[1] - tokens[0])) + wordlen[1];
				sizetoken = 2;
				filetoken = 3;
				spaces = 10;
			}
			else {
				if(j < 8 || wordlen[0]!=10) continue;
				if(j < 8 || !isnumber(*tokens[4])) mode = 3;
				if(*tokens[0] == 'd') isdir = 1;
				if(*tokens[0] == 'l') islink = 1;
				modetoken = 0;
				sizetoken = (mode == 2)? 4:3;
				filetoken = (mode == 2)? 8:7;
				datetoken = (mode == 2)? 5:4;
				tokens[filetoken] = tokens[filetoken-1];
				while(*tokens[filetoken] && !isspace(*tokens[filetoken]))tokens[filetoken]++;
				if(*tokens[filetoken]){
					tokens[filetoken]++;
				}
				wordlen[datetoken] = (unsigned)(tokens[filetoken] - tokens[datetoken]);
				wordlen[filetoken] = (unsigned)strlen((char *)tokens[filetoken]);
			}

			if(modetoken >= 0) memcpy(buf+inbuf, tokens[modetoken], 11);
			else memcpy(buf+inbuf, "---------- ", 11);
			inbuf += 11;
			if(wordlen[datetoken]+256 > bufsize-inbuf) continue;
			memcpy(buf+inbuf, tokens[datetoken], wordlen[datetoken]);
			inbuf += wordlen[datetoken];
			if(isdir){
				memcpy(buf+inbuf, "       DIR", 10);
				inbuf+=10;
			}
			else if(islink){
				memcpy(buf+inbuf, "      LINK", 10);
				inbuf+=10;
			}
			else{
				unsigned k;
				if(wordlen[sizetoken]>10) wordlen[sizetoken] = 10;
				for(k=10; k > wordlen[sizetoken]; k--){
					buf[inbuf++] = ' ';
				}
				memcpy(buf+inbuf, tokens[sizetoken], wordlen[sizetoken]);
				inbuf+=wordlen[sizetoken];
			}
			memcpy(buf+inbuf, " <A HREF=\"", 10);
			inbuf+=10;
			sb = NULL;
			if(islink) sb = (unsigned char *)strstr((char *)tokens[filetoken], " -> ");
			if(sb) sb+=4;

			else sb=tokens[filetoken]; 
			if(*sb != '/' && ftpbase)file2url(ftpbase, buf, bufsize, (int *)&inbuf, 1);
			file2url(sb, buf, bufsize, (int *)&inbuf, 0);

			if(isdir)buf[inbuf++] = '/';
			memcpy(buf+inbuf, "\">", 2);
			inbuf+=2;
			for(sb=tokens[filetoken]; *sb; sb++){
				if((bufsize - inbuf)<16)break;
				if(*sb == '<'){
					memcpy(buf+inbuf, "&lt;", 4);
					inbuf+=4;
				}
				else if(*sb == '>'){
					memcpy(buf+inbuf, "&gt;", 4);
					inbuf+=4;
				}
				else if(*sb == '\r' || *sb=='\n'){
					continue;
				}
				else if(islink && sb[0] == ' ' && sb[1] == '-' 
				 && sb[2] == '>'){
					memcpy(buf+inbuf, "</A> ", 5);
					inbuf+=5;
				}
				else buf[inbuf++]=*sb;
			}
			if(islink!=2)memcpy(buf+inbuf, "</A>", 4);
			inbuf+=4;
			buf[inbuf++] = '\n';

			if((bufsize - inbuf) < LINESIZE){
				if (bufsize > 20000){
					if(!headsent++){
						socksend(param->clisock, (unsigned char *)proxy_stringtable[9], (int)strlen(proxy_stringtable[9]), conf.timeouts[STRING_S]);
					}
					if((unsigned)socksend(param->clisock, buf, inbuf, conf.timeouts[STRING_S]) != inbuf){
						RETURN(781);
					}
					inbuf = 0;
				}
				else {
					if(!(newbuf = myrealloc(buf, bufsize + BUFSIZE))){RETURN (21);}
					buf = newbuf;
					bufsize += BUFSIZE;
				}
			}
		}
		memcpy(buf+inbuf, "<hr>", 4);
		inbuf += 4;
		closesocket(ftps);
		ftps = INVALID_SOCKET;
		param->remsock = s;
		if(inbuf){
			buf[inbuf] = 0;
			res = ftpres(param, buf+inbuf, bufsize-inbuf);
			inbuf = (int)strlen(buf);
			if(!headsent){
				sprintf(ftpbuf, 
					"HTTP/1.0 200 OK\r\n"
					"Content-Type: text/html\r\n"
					"Proxy-Connection: keep-alive\r\n"
					"Content-Length: %d\r\n\r\n",
					inbuf);
				socksend(param->clisock, (unsigned char *)ftpbuf, (int)strlen(ftpbuf), conf.timeouts[STRING_S]);
			}
			socksend(param->clisock, buf, inbuf, conf.timeouts[STRING_S]);
			if(res){RETURN(res);}
			if(!headsent)goto REQUESTEND;
		}
		RETURN(0);
	}
	RETURN(res);
 }

 if(isconnect && param->redirtype != R_HTTP) {
	socksend(param->clisock, (unsigned char *)proxy_stringtable[8], (int)strlen(proxy_stringtable[8]), conf.timeouts[STRING_S]);
	RETURN(sockmap(param, conf.timeouts[CONNECTION_L]));
 }

 if(!req || param->redirtype != R_HTTP) {
	reqlen = 0;
 }

 else {
	 redirect = 1;
	 if(socksend(param->remsock, req , (res = (int)strlen((char *)req)), conf.timeouts[STRING_L]) != res) {
		RETURN(518);
	 }
	 param->statscli += res;
 }
 inbuf = 0;
#ifndef ANONYMOUS
 if(anonymous!=1){
		sprintf((char*)buf+strlen((char *)buf), "Via: 1.1 ");
		gethostname((char *)(buf+strlen((char *)buf)), 256);
		sprintf((char*)buf+strlen((char *)buf), ":%d (%s %s)\r\nX-Forwarded-For: ", (int)ntohs(param->intport), stringtable?stringtable[2]:(unsigned char *)"", stringtable?stringtable[3]:(unsigned char *)"");
		if(!anonymous)myinet_ntoa(param->sinc.sin_addr, (char *)buf + strlen((char *)buf));
		else {
			unsigned long tmp;

			tmp = param->sinc.sin_addr.s_addr;
			param->sinc.sin_addr.s_addr = ((unsigned long)myrand(param, sizeof(struct clientparam))<<16)^(unsigned long)rand();
			myinet_ntoa(param->sinc.sin_addr, (char *)buf + strlen((char *)buf));
			param->sinc.sin_addr.s_addr = tmp;
		}
		sprintf((char*)buf+strlen((char *)buf), "\r\n");
 }
#endif
 if(keepalive)sprintf((char*)buf+strlen((char *)buf), "%s: Keep-Alive\r\n", (redirect)?"Proxy-Connection":"Connection");
 if(param->extusername){
	sprintf((char*)buf + strlen((char *)buf), "%s: basic ", (redirect)?"Proxy-Authorization":"Authorization");
	sprintf((char*)username, "%.32s:%.64s", param->extusername, param->extpassword?param->extpassword:(unsigned char*)"");
	en64(username, buf+strlen((char *)buf), (int)strlen((char *)username));
	sprintf((char*)buf + strlen((char *)buf), "\r\n");
 }
 sprintf((char*)buf+strlen((char *)buf), "\r\n");
 if ((res = socksend(param->remsock, buf+reqlen, (int)strlen((char *)buf+reqlen), conf.timeouts[STRING_S])) != (int)strlen((char *)buf+reqlen)) {
	RETURN(518);
 }
 param->statscli += res;
 if(param->bandlimfunc) {
	sleeptime = param->bandlimfunc(param, 0, (int)strlen(buf));
 }
 if(isconnect){
	RETURN (sockmap(param, conf.timeouts[CONNECTION_S]));
 }
 if(contentlength > 0){
	 param->waitclient = contentlength;
	 res = sockmap(param, conf.timeouts[CONNECTION_S]);
	 param->waitclient = 0;
	 if(res != 99) {
		RETURN(res);
	}
 }
 contentlength = -1;
 inbuf = 0;
 ckeepalive = 1;
 res = 0; 
 while( (i = sockgetlinebuf(param, SERVER, buf + inbuf, LINESIZE - 1, '\n', conf.timeouts[(res)?STRING_S:STRING_L])) > 2) {
	if(!res && i>9)res = atoi((char *)buf + inbuf + 9);
	if(((i> 25 && redirect && !strncasecmp((char *)(buf+inbuf), "proxy-connection:", 17))
	   ||
	    (i> 16 && !redirect && !strncasecmp((char *)(buf+inbuf), "connection:", 11))
			)){
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		if(!strncasecmp((char *)sb,"keep-alive", 10))ckeepalive = 1;
		else ckeepalive = 0;
		continue; 
	}
	if(i> 6 && (!strncasecmp((char *)(buf+inbuf), "proxy-", 6))){
		continue; 
	}
	if(i > 15 && (!strncasecmp((char *)(buf+inbuf), "content-length", 14))){
		buf[inbuf+i]=0;
		sb = (unsigned char *)strchr((char *)(buf+inbuf), ':');
		if(!sb)continue;
		++sb;
		while(isspace(*sb))sb++;
		contentlength = atoi((char *)sb);
		if(contentlength < 0)contentlength = 0;
		if(param->maxtraf && (param->maxtraf < param->statssrv || (unsigned)contentlength > param->maxtraf - param->statssrv)){
			RETURN(10);
		}
	}
	inbuf += i;
	if((bufsize - inbuf) < LINESIZE){
		if (bufsize > 20000){
			RETURN (516);
		}
		if(!(newbuf = myrealloc(buf, bufsize + BUFSIZE))){RETURN (21);}
		buf = newbuf;
		bufsize += BUFSIZE;
	}
 }
 if(res < 200 || res > 499) {
	ckeepalive = 0;
 }
 param->statssrv += inbuf;
 if(param->bandlimfunc) {
	int st1;

	st1 = (*param->bandlimfunc)(param, inbuf, 0);
	if(st1 > sleeptime) sleeptime = st1;
	if(sleeptime > 0){
/*		if(sleeptime > 30) sleeptime = 30; */
		usleep(sleeptime * SLEEPTIME);
	}
 }
 buf[inbuf] = 0;
 if(inbuf < 9) {RETURN (520);}
 sprintf((char*)buf+strlen((char *)buf), "%s: %s\r\n", 
	(transparent)?"Connection":"Proxy-Connection",
	(contentlength >= 0 && ckeepalive)?"Keep-Alive":"Close"
 );
 sprintf((char*)buf + strlen((char *)buf), "\r\n");
 if((socksend(param->clisock, buf, (int)strlen((char *)buf), conf.timeouts[STRING_S])) != (int)strlen((char *)buf)) {
	RETURN(518);
 }
 if(contentlength > 0 && param->operation != HTTP_HEAD && res != 204 && res != 304) {
	 param->waitserver = contentlength;
	 res = sockmap(param, conf.timeouts[CONNECTION_S]);
	 param->waitserver = 0;
	 if(res != 98) {
		RETURN(res);
	 }
 }
 else if(contentlength < 0) {
	RETURN(sockmap(param, conf.timeouts[CONNECTION_S]));
 }
 contentlength = -1;

REQUESTEND:

 if((!ckeepalive || !keepalive) && param->remsock != INVALID_SOCKET){
	shutdown(param->remsock, SHUT_RDWR);
	closesocket(param->remsock);
	param->remsock = INVALID_SOCKET;
	RETURN(0);
 }
 logurl(param, (char *)req, ftp);
}

CLEANRET:

 if(param->res != 555 && param->res && param->clisock != INVALID_SOCKET && (param->res < 90 || param->res >=800 || param->res == 100 ||(param->res > 701 && param->res< 800))) {
	if((param->res>=510 && param->res <=517) || param->res > 900) while( (i = sockgetlinebuf(param, CLIENT, buf, BUFSIZE - 1, '\n', conf.timeouts[STRING_S])) > 2);
	if(param->res == 10) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[2], (int)strlen(proxy_stringtable[2]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 100 || (param->res >10 && param->res < 20) || (param->res >701 && param->res <= 705)) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[1], (int)strlen(proxy_stringtable[1]), conf.timeouts[STRING_S]);
	}
	else if(param->res >=20 && param->res < 30) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[6], (int)strlen(proxy_stringtable[6]), conf.timeouts[STRING_S]);
	}
	else if(param->res >=30 && param->res < 80) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[5], (int)strlen(proxy_stringtable[5]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 1) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[11], (int)strlen(proxy_stringtable[11]), conf.timeouts[STRING_S]);
	}
	else if(param->res < 10) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[param->usentlm?12:7], (int)strlen(proxy_stringtable[param->usentlm?12:7]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 999) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[4], (int)strlen(proxy_stringtable[4]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 519) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[3], (int)strlen(proxy_stringtable[3]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 780) {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[10], (int)strlen(proxy_stringtable[10]), conf.timeouts[STRING_S]);
	}
	else if(param->res == 750) {
	}
	else {
		socksend(param->clisock, (unsigned char *)proxy_stringtable[0], (int)strlen(proxy_stringtable[0]), conf.timeouts[STRING_S]);
	}
 } 
 logurl(param, (char *)req, ftp);
 if(req)myfree(req);
 if(buf)myfree(buf);
 if(ftpbase)myfree(ftpbase);
 freeparam(param);
 return (NULL);
}

#ifdef WITHMAIN
struct proxydef childdef = {
	proxychild,
	3128,
	0,
	S_PROXY,
	"-a - anonymous proxy\r\n"
	"-a1 - anonymous proxy with random client IP spoofing\r\n"
};
#include "proxymain.c"
#endif
