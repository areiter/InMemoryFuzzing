/*
   3APA3A simpliest proxy server
   (c) 2002-2006 by ZARAZA <3APA3A@security.nnov.ru>

   please read License Agreement

   $Id: auth.c,v 1.46 2006/03/10 19:25:45 vlad Exp $
*/

#include "proxy.h"

#define HEADERSIZE 57
#define RECORDSIZE  18

unsigned char request[] = {	
		0xa2, 0x48, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x20, 0x43, 0x4b, 0x41, 
		0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 
		0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 
		0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 
		0x41, 0x41, 0x41, 0x41, 0x41, 0x00, 0x00, 0x21, 
		0x00, 0x01};

unsigned char * getNetBIOSnamebyip(unsigned long ip){
 unsigned char buf[1024];
 struct sockaddr_in sins;
 int res;
 SOCKET sock;
 unsigned char * username = NULL;
 int i;
 int j;
 int nnames;
 int type;

 if ( (sock=socket(AF_INET,SOCK_DGRAM,IPPROTO_UDP)) == INVALID_SOCKET) return NULL;
 sins.sin_family = AF_INET;
 sins.sin_port = htons(0);
 sins.sin_addr.s_addr = INADDR_ANY;
 if(bind(sock,(struct sockaddr *)&sins,sizeof(sins))) {
	closesocket(sock);
	return NULL;
 }
 sins.sin_family = AF_INET;
 sins.sin_addr.s_addr = ip;
 sins.sin_port = htons(137);
 res=socksendto(sock, &sins, request, sizeof(request), conf.timeouts[SINGLEBYTE_L]*1000);
 if(res <= 0) {
	closesocket(sock);
	return NULL;
 }
 res = sockrecvfrom(sock, &sins, buf, sizeof(buf), conf.timeouts[SINGLEBYTE_L]*1000);
 closesocket(sock);
 if(res < (HEADERSIZE + RECORDSIZE)) {
	return NULL;
 }
 nnames = buf[HEADERSIZE-1];
 if (res < (HEADERSIZE + (nnames * RECORDSIZE))) return NULL;
 for (i = 0; i < nnames; i++){
	type = buf[HEADERSIZE + (i*RECORDSIZE) + 15];
	if( type == 3) {
		for(j = 14; j && buf[HEADERSIZE + (i*RECORDSIZE) + j] == ' '; j--)
			buf[HEADERSIZE + (i*RECORDSIZE) + j] = 0;
		if(username)myfree(username);
		username = (unsigned char *)mystrdup((char *)buf + HEADERSIZE + i*RECORDSIZE);
	}
	buf[HEADERSIZE + (i*RECORDSIZE) + 15] = 0;
 }
 return username;
} 

int clientnegotiate(struct chain * redir, struct clientparam * param, unsigned long ip, unsigned short port){
	unsigned char buf[1024];
	struct in_addr ina;
	int res;
	int len=0;
	unsigned char * user, *pass;

	ina.s_addr = ip;

	
	user = redir->extuser;
	pass = redir->extpass;
	if(user) {
		if (*user == '*') {
			if(!param->username) return 4;
			user = param->username;
			pass = param->password;
		}
	}
	switch(redir->type){
		case R_TCP:
		case R_HTTP:
			return 0;
		case R_CONNECT:
		case R_CONNECTP:
		{
			sprintf((char *)buf, "CONNECT ");
			if(redir->type == R_CONNECTP && param->hostname) {
				len = 8 + sprintf((char *)buf + 8, "%.256s", param->hostname);
			}
			else {
				len = 8 + myinet_ntoa(ina, (char *)buf+8);
			}
			len += sprintf((char *)buf + len,
				":%hu HTTP/1.0\r\nProxy-Connection: keep-alive\r\n", ntohs(port));
			if(user){
				unsigned char username[256];
				len += sprintf((char *)buf + len, "Proxy-authorization: basic ");
				sprintf((char *)username, "%.128s:%.64s", user, pass?pass:(unsigned char *)"");
				en64(username, buf+len, strlen((char *)username));
				len = strlen((char *)buf);
				len += sprintf((char *)buf + len, "\r\n");
			}
			len += sprintf((char *)buf + len, "\r\n");
			if(socksend(param->remsock, buf, len, conf.timeouts[CHAIN_TO]) != (int)strlen((char *)buf))
				return 31;
			if((res = sockgetlinebuf(param, SERVER,buf,13,'\n',conf.timeouts[CHAIN_TO])) < 13)
				return 32;
			if(buf[9] != '2') return 33;
			while((res = sockgetlinebuf(param, SERVER,buf,1023,'\n', conf.timeouts[CHAIN_TO])) > 2);
			if(res <= 0) return 34;
			return 0;
		}
		case R_SOCKS4:
		case R_SOCKS4P:
		case R_SOCKS4B:
		{

			buf[0] = 4;
			buf[1] = 1;
			memcpy(buf+2, &port, 2);
			if(redir->type == R_SOCKS4P && param->hostname) {
				buf[4] = buf[5] = buf[6] = 0;
				buf[7] = 3;
			}
			else memcpy(buf+4, &ip, 4);
			if(!user)user = (unsigned char *)"anonymous";
			len = strlen((char *)user) + 1;
			memcpy(buf+8, user, len);
			len += 8;
			if(redir->type == R_SOCKS4P && param->hostname) {
				int hostnamelen;

				hostnamelen = strlen((char *)param->hostname) + 1;
				if(hostnamelen > 255) hostnamelen = 255;
				memcpy(buf+len, param->hostname, hostnamelen);
				len += hostnamelen;
			}
			if(socksend(param->remsock, buf, len, conf.timeouts[CHAIN_TO]) < len){
				return 41;
			}
			if(sockgetlinebuf(param, SERVER, buf, (redir->type == R_SOCKS4B)? 3:8, EOF, conf.timeouts[CHAIN_TO]) != ((redir->type == R_SOCKS4B)? 3:8)){
				return 42;
			}
			if(buf[1] != 90) {
				return 43;
			}

		}
		return 0;

		case R_SOCKS5:
		case R_SOCKS5P:
		case R_SOCKS5B:
		{
		 int inbuf = 0;
			buf[0] = 5;
			buf[1] = 1;
			buf[2] = user? 2 : 0;
			if(socksend(param->remsock, buf, 3, conf.timeouts[CHAIN_TO]) != 3){
				return 51;
			}
			if(sockgetlinebuf(param, SERVER, buf, 2, EOF, conf.timeouts[CHAIN_TO]) != 2){
				return 52;
			}
			if(buf[0] != 5) {
				return 53;
			}
			if(buf[1] != 0 && !(buf[1] == 2 && user)){
				return 54;
			}
			if(buf[1] == 2){
				buf[inbuf++] = 1;
				buf[inbuf] = (unsigned char)strlen((char *)user);
				memcpy(buf+inbuf+1, user, buf[inbuf]);
				inbuf += buf[inbuf] + 1;
				buf[inbuf] = pass?(unsigned char)strlen((char *)pass):0;
				if(pass)memcpy(buf+inbuf+1, pass, buf[inbuf]);
				inbuf += buf[inbuf] + 1;
				if(socksend(param->remsock, buf, inbuf, conf.timeouts[CHAIN_TO]) != inbuf){
					return 51;
				}
				if(sockgetlinebuf(param, SERVER, buf, 2, EOF, 60) != 2){
					return 55;
				}
				if(buf[0] != 1 || buf[1] != 0) {
					return 56;
				}
			}
			buf[0] = 5;
			buf[1] = 1;
			buf[2] = 0;
			if(redir->type == R_SOCKS5P && param->hostname) {
				buf[3] = 3;
				len = strlen((char *)param->hostname);
				if(len > 255) len = 255;
				buf[4] = len;
				memcpy(buf + 5, param->hostname, len);
				len += 5;
			}
			else {
				buf[3] = 1;
				memcpy(buf+4, &ip, 4);
				len = 8;
			}
			memcpy(buf+len, &port, 2);
			len += 2;
			if(socksend(param->remsock, buf, len, conf.timeouts[CHAIN_TO]) != len){
				return 51;
			}
			if(sockgetlinebuf(param, SERVER, buf, 4, EOF, conf.timeouts[CHAIN_TO]) != 4){
				return 57;
			}
			if(buf[0] != 5) {
				return 53;
			}
			if(buf[1] != 0) {
				return 60 + (buf[1] % 10);
			}
			if(buf[3] != 1) {
				return 58;
			}
			if (redir->type != R_SOCKS5B && sockgetlinebuf(param, SERVER, buf, 6, EOF, conf.timeouts[CHAIN_TO]) != 6){
				return 59;
			}
			return 0;
		}

		default:

			return 30;
	}
}


int handleredirect(struct clientparam * param, struct ace * acentry){
	int connected = 0;
	int weight = 1000;
	int res;
	int done = 0;
	struct chain * cur;
	struct chain * redir = NULL;
	unsigned long targetip;
	unsigned short targetport;
	int r1, r2;

	if(!param->sins.sin_addr.s_addr && param->hostname) param->sins.sin_addr.s_addr = getip(param->hostname);
	targetip = param->sins.sin_addr.s_addr;
	targetport = param->sins.sin_port;
	if(!targetip || !targetport) return 100;
	if(param->remsock != INVALID_SOCKET) {
		return 0;
	}

	myrand(param, sizeof(struct clientparam));

	for(cur = acentry->chains; cur; cur=cur->next){
		if(done || (cur->weight < weight)) {
			r1 = (cur->weight * 12347)/weight;
			r2 = (myrand(cur, sizeof(struct chain)))%12347;
			if( done ||  (r1 < r2)) {
				weight -= cur->weight;
				if(weight <= 0) {
					weight = 1000;
					done = 0;
				}
				continue;
			}
		}
		param->redirected++;
		done = 1;
		weight -= cur->weight;
		if(weight <= 0) {
			weight = 1000;
			done = 0;
		}
		if(!connected){
			if(cur->redirip) param->sins.sin_addr.s_addr = cur->redirip;
			if(cur->redirport) param->sins.sin_port = cur->redirport;
			if(!cur->redirip && !cur->redirport){
				if(cur->extuser){
					if(param->extusername)
						myfree(param->extusername);
					param->extusername = (unsigned char *)mystrdup((char *)((*cur->extuser == '*' && param->username)? param->username : cur->extuser));
					if(cur->extpass){
						if(param->extpassword)
							myfree(param->extpassword);
						param->extpassword = (unsigned char *)mystrdup((char *)((*cur->extuser == '*' && param->password)?param->password : cur->extpass));
					}
					if(*cur->extuser == '*' && !param->username) return 4;
				}
				switch(cur->type){
					case R_POP3:
						param->redirectfunc = pop3pchild;
						break;
					case R_FTP:
						param->redirectfunc = ftpprchild;
						break;
					default:
						param->redirectfunc = proxychild;
				}
				return 0;
			}
			if((res = alwaysauth(param))){
				return (res == 10)? res : 60+res;
			}
/*
			param->sins.sin_addr.s_addr = targetip;
			param->sins.sin_port = targetport;
*/
		}
		else {
			res = redir?clientnegotiate(redir, param, cur->redirip, cur->redirport):0;
			if(res) return res;
		}
		redir = cur;
		param->redirtype = redir->type;
		if(redir->type == R_TCP || redir->type ==R_HTTP) {
			if(cur->extuser){
				if(*cur -> extuser == '*' && !param->username) return 4;
				if(param->extusername)
					myfree(param->extusername);
				param->extusername = (unsigned char *)mystrdup((char *)((*cur->extuser == '*' && param->username)? param->username : cur->extuser));
				if(cur->extpass){
					if(param->extpassword)
						myfree(param->extpassword);
					param->extpassword = (unsigned char *)mystrdup((char *)((*cur->extuser == '*' && param->password)?param->password : cur->extpass));
				}
			}
			return 0;
		}
		connected = 1;
	}

	if(!connected) return 9;
	return redir?clientnegotiate(redir, param, targetip, targetport):0;
}


int ACLmatches(struct ace* acentry, struct clientparam * param){
	struct userlist * userentry;
	struct iplist *ipentry;
	struct portlist *portentry;
	struct period *periodentry;
	unsigned char * username;
	
	username = param->username?param->username:(unsigned char *)"*";

	if(acentry->src) {
	 for(ipentry = acentry->src; ipentry; ipentry = ipentry->next)
		if(ipentry->ip == (param->sinc.sin_addr.s_addr & ipentry->mask)) {
			break;
		}
		if(!ipentry) return 0;
	}
	if(acentry->dst && param->sins.sin_addr.s_addr) {
	 for(ipentry = acentry->dst; ipentry; ipentry = ipentry->next)
		if(ipentry->ip == (param->sins.sin_addr.s_addr & ipentry->mask)) {
			break;
		}
		if(!ipentry) return 0;
	}
	if(acentry->ports && param->sins.sin_port) {
	 for (portentry = acentry->ports; portentry; portentry = portentry->next)
		if(ntohs(param->sins.sin_port) >= portentry->startport &&
			   ntohs(param->sins.sin_port) <= portentry->endport) {
			break;
		}
		if(!portentry) return 0;
	}
	if(acentry->wdays){
		if(!(acentry -> wdays & wday)) return 0;
	}
	if(acentry->periods){
	 int start_time = param->time_start - basetime;
	 for(periodentry = acentry->periods; periodentry; periodentry = periodentry -> next)
		if(start_time >= periodentry->fromtime && start_time < periodentry->totime){
			break;
		}
		if(!periodentry) return 0;
	}
	if(acentry->users){
	 for(userentry = acentry->users; userentry; userentry = userentry->next)
		if(!strcmp((char *)username, (char *)userentry->user)){
			break;
		}
	 if(!userentry) return 0;
	}
	if(acentry->operation) {
		if((acentry->operation & param->operation) != param->operation){
				 return 0;
		}
	}
	return 1;
}


unsigned bandlimitfunc(struct clientparam *param, unsigned nbytesin, unsigned nbytesout){
	unsigned sleeptime = 0, nsleeptime;
	unsigned long sec;
	unsigned msec;
	unsigned now;
	int i;

#ifdef _WIN32
	struct timeb tb;

	ftime(&tb);
	sec = (unsigned)tb.time;
	msec = (unsigned)tb.millitm*1000;

#else
	struct timeval tv;
	gettimeofday(&tv, NULL);

	sec = tv.tv_sec;
	msec = tv.tv_usec;
#endif
	if(!nbytesin && !nbytesout) return 0;
	pthread_mutex_lock(&bandlim_mutex);
	if(param->version != paused){
		struct bandlim * be;
		for(i = 0, be = conf.bandlimiter; be && i<MAXBANDLIMS; be = be->next) {
			if(ACLmatches(be->ace, param)){
				if(be->ace->action == NOBANDLIM) {
					break;
				}
				param->bandlims[i++] = be;
			}
		}
		if(i < MAXBANDLIMS) param->bandlims[i++] = 0;
		for(i = 0, be = conf.bandlimiterout; be && i<MAXBANDLIMS; be = be->next) {
			if(ACLmatches(be->ace, param)){
				if(be->ace->action == NOBANDLIM) {
					break;
				}
				param->bandlimsout[i++] = be;
			}
		}
		if(i < MAXBANDLIMS) param->bandlimsout[i++] = 0;
	}
	for(i=0; nbytesin&& i<MAXBANDLIMS && param->bandlims[i]; i++){
		if( !param->bandlims[i]->basetime || 
			param->bandlims[i]->basetime > sec ||
			param->bandlims[i]->basetime < (sec - 120)
		  )
		{
			param->bandlims[i]->basetime = sec;
			param->bandlims[i]->nexttime = 0;
			continue;
		}
		now = ((sec - param->bandlims[i]->basetime) * 1000000) + msec;
		nsleeptime = (param->bandlims[i]->nexttime > now)?
			param->bandlims[i]->nexttime - now : 0;
		sleeptime = (nsleeptime > sleeptime)? nsleeptime : sleeptime;
		param->bandlims[i]->basetime = sec;
		param->bandlims[i]->nexttime = msec + nsleeptime + ((param->bandlims[i]->rate > 1000000)? ((nbytesin/32)*(256000000/param->bandlims[i]->rate)) : (nbytesin * (8000000/param->bandlims[i]->rate)));
	}
	for(i=0; nbytesout && i<MAXBANDLIMS && param->bandlimsout[i]; i++){
		if( !param->bandlimsout[i]->basetime || 
			param->bandlimsout[i]->basetime > sec ||
			param->bandlimsout[i]->basetime < (sec - 120)
		  )
		{
			param->bandlimsout[i]->basetime = sec;
			param->bandlimsout[i]->nexttime = 0;
			continue;
		}
		now = ((sec - param->bandlimsout[i]->basetime) * 1000000) + msec;
		nsleeptime = (param->bandlimsout[i]->nexttime > now)?
			param->bandlimsout[i]->nexttime - now : 0;
		sleeptime = (nsleeptime > sleeptime)? nsleeptime : sleeptime;
		param->bandlimsout[i]->basetime = sec;
		param->bandlimsout[i]->nexttime = msec + nsleeptime + ((param->bandlimsout[i]->rate > 1000000)? ((nbytesout/32)*(256000000/param->bandlimsout[i]->rate)) : (nbytesout * (8000000/param->bandlimsout[i]->rate)));
	}
	pthread_mutex_unlock(&bandlim_mutex);
	return sleeptime/1000;
}

void trafcountfunc(struct clientparam *param){
	struct trafcount * tc;
	unsigned long val;

	pthread_mutex_lock(&tc_mutex);
	for(tc = conf.trafcounter; tc; tc = tc->next) {
		if(ACLmatches(tc->ace, param)){
			time_t t;
			if(tc->ace->action == NOCOUNT) break;
			val = tc->traf + param->statssrv;
			if(val < tc->traf) tc->trafgb++;
			tc->traf = val;
			time(&t);
			tc->updated = t;
		}
	}
	pthread_mutex_unlock(&tc_mutex);
}

int alwaysauth(struct clientparam * param){
	int res;
	int i = 0;
	struct bandlim * be;
	struct trafcount * tc;

	res = doconnect(param);
	if(!res){
		if(param->version != paused) return 333;
		for(be = conf.bandlimiter; be && i<MAXBANDLIMS; be = be->next) {
			if(ACLmatches(be->ace, param)){
				if(be->ace->action == NOBANDLIM) {
					break;
				}
				param->bandlims[i++] = be;
				param->bandlimfunc = bandlimitfunc;
			}
		}
		if(i<MAXBANDLIMS)param->bandlims[i] = NULL;
		for(be = conf.bandlimiterout; be && i<MAXBANDLIMS; be = be->next) {
			if(ACLmatches(be->ace, param)){
				if(be->ace->action == NOBANDLIM) {
					break;
				}
				param->bandlimsout[i++] = be;
				param->bandlimfunc = bandlimitfunc;
			}
		}
		if(i<MAXBANDLIMS)param->bandlimsout[i] = NULL;
		if(conf.trafcounter) {
			for(tc = conf.trafcounter; tc; tc = tc->next) {
				if(tc->disabled) continue;
				if(ACLmatches(tc->ace, param)){
					if(tc->ace->action == NOCOUNT) break;
				
					if((tc->traflimgb < tc->trafgb) ||
						((tc->traflimgb == tc->trafgb) && (tc->traflim < tc->traf))
					) return 10;
					param->trafcountfunc = trafcountfunc;
					if(tc->traflimgb - tc->trafgb < 2){
						unsigned maxtraf = tc->traflim - tc->traf + ((tc->traflimgb - tc->trafgb) * 0x40000000);
						if(!param->maxtraf || param->maxtraf > maxtraf) param->maxtraf = maxtraf;
					}
				}
			}
		}
	}
	return res;
}

int checkACL(struct clientparam * param){
	struct ace* acentry;

	if(param->remsock == INVALID_SOCKET && param->hostname && !param->sins.sin_addr.s_addr) param->sins.sin_addr.s_addr = getip(param->hostname);
	pthread_mutex_lock(&acl_mutex);
	if(!conf.acls[param->aclnum]) {
		pthread_mutex_unlock(&acl_mutex);
		return alwaysauth(param);
	}
	for(acentry = conf.acls[param->aclnum]; acentry; acentry = acentry->next) {
		if(ACLmatches(acentry, param)) {
			if(acentry->action == 2) {
				struct ace dup;

				if(param->operation < 256 && !(param->operation & CONNECT)){
					continue;
				}
				if(param->redirected && acentry->chains && !acentry->chains->redirip && !acentry->chains->redirport) {
					continue;
				}
				memcpy(&dup, acentry, sizeof(struct ace));
				pthread_mutex_unlock(&acl_mutex);
				return handleredirect(param, &dup);
			}
			pthread_mutex_unlock(&acl_mutex);
			return (acentry->action)?acentry->action:alwaysauth(param);
		}
	}
	pthread_mutex_unlock(&acl_mutex);
	return 3;
}

int ipauth(struct clientparam * param){
	return checkACL(param);
}

int nbnameauth(struct clientparam * param){
	unsigned char * name = getNetBIOSnamebyip(param->sinc.sin_addr.s_addr);

	if (param->username) myfree (param->username);
	param->username = name;
	return checkACL(param);
}

int strongauth(struct clientparam * param){
	struct passwords * pwl;
	unsigned char buf[256];

	if(!param->username) return 4;
	for(pwl = conf.pwl; pwl; pwl=pwl->next){
		if(!strcmp((char *)pwl->user, (char *)param->username)) switch(pwl->pwtype) {
			case CL:
				if(!pwl->password || !*pwl->password){
					break;
				}
				else if (!param->pwtype && param->password && !strcmp((char *)param->password, (char *)pwl->password)){
					break;
				}
				else if (param->pwtype == 2 && param->password) {
					ntpwdhash(buf, pwl->password, 0);
					mschap(buf, param->password, buf + 16);
					if(!memcmp(buf+16, param->password+8, 24)) {
						break;
					}
				}
				return 6;
			case CR:
				if(param->password && !param->pwtype && !strcmp((char *)pwl->password, (char *)mycrypt(param->password, pwl->password,buf))) {
					break;
				}
				return 7;
			case NT:
				if(param->password && !param->pwtype && !memcmp(pwl->password, ntpwdhash(buf,param->password, 0), 16)) {
					break;
				}
				else if (param->pwtype == 2){
					mschap(pwl->password, param->password, buf);
					if(!memcmp(buf, param->password+8, 24)) {
						break;
					}
				}
				return 8;
				
			default:
				return 999;
		}
		else continue;
		return checkACL(param);
	}
	return 5;
}


struct hashentry {
	unsigned char hash[sizeof(unsigned)*2];
	unsigned long value;
	time_t expires;
	struct hashentry *next;
};

struct hashentry ** hashtable = NULL;
struct hashentry * hashvalues = NULL;
struct hashentry * hashempty = NULL;

unsigned hashsize = 0;

void nametohash(const unsigned char * name, unsigned char *hash){
	unsigned i, j;
	memset(hash, 0, sizeof(unsigned)*2);
	for(i=0, j=0; name[j]; j++){
		hash[i] += toupper(name[j]) - 32;
		if(++i == sizeof(unsigned)*2) i = 0;
	}
}

unsigned hashindex(const unsigned char* hash){
	unsigned t1, t2;
	t1 = *(unsigned *)hash;
	t2 = *(unsigned *)(hash + sizeof(unsigned));
	return (t1 * 54321 + t2) % (hashsize >> 2);
}


void destroyhashtable(void){
	pthread_mutex_lock(&hash_mutex);
	if(hashtable){
		myfree(hashtable);
		hashtable = NULL;
	}
	if(hashvalues){
		myfree(hashvalues);
		hashvalues = NULL;
	}
	pthread_mutex_unlock(&hash_mutex);
}

int inithashtable(unsigned nhashsize){
	unsigned i;

	if(nhashsize<4) return 1;
	if(hashtable){
		myfree(hashtable);
		hashtable = NULL;
	}
	if(hashvalues){
		myfree(hashvalues);
		hashvalues = NULL;
	}
	hashsize = 0;
	if(!(hashtable = myalloc((nhashsize>>2) * sizeof(struct hashentry *)))){
		return 2;
	}
	if(!(hashvalues = myalloc(nhashsize * sizeof(struct hashentry)))){
		myfree(hashtable);
		hashtable = NULL;
		return 3;
	}
	hashsize = nhashsize;
	memset(hashtable, 0, (hashsize>>2) * sizeof(struct hashentry *));
	memset(hashvalues, 0, hashsize * sizeof(struct hashentry));
	for(i = 0; i< (hashsize - 1); i++) {
		(hashvalues + i)->next = hashvalues + i + 1;
	}
	hashempty = hashvalues;
	return 0;
}

void hashadd(const unsigned char* name, unsigned long value, time_t expires){
        struct hashentry * he;
	unsigned index;
	
	if(!value||!name||!hashtable||!hashempty) return;
	pthread_mutex_lock(&hash_mutex);
	he = hashempty;
	hashempty = hashempty->next;
	nametohash(name, he->hash);
	he->value = value;
	he->expires = expires;
	he->next = NULL;
	index = hashindex(he->hash);
	if(!hashtable[index] || !memcmp(he->hash, hashtable[index]->hash, sizeof(he->hash))){
		he->next = hashtable[index];
		hashtable[index] = he;
	}
	else {
		memset(he, 0, sizeof(struct hashentry));
		he->next = hashempty;
		hashempty = he;
	}
	pthread_mutex_unlock(&hash_mutex);
}

unsigned long hashresolv(const unsigned char* name, unsigned *ttl){
	unsigned char hash[sizeof(unsigned)*2];
        struct hashentry ** hep;
	struct hashentry *he;
	unsigned index;
	time_t t;

	if(!hashtable || !name) return 0;
	time(&t);
	nametohash(name, hash);
	index = hashindex(hash);
	pthread_mutex_lock(&hash_mutex);
	for(hep = hashtable + index; (he = *hep)!=NULL; ){
		if((unsigned long)he->expires < (unsigned long)t) {
			(*hep) = he->next;
			he->expires = 0;
			he->next = hashempty;
			hashempty = he;
		}
		else if(!memcmp(hash, he->hash, sizeof(unsigned)*2)){
			pthread_mutex_unlock(&hash_mutex);
			if(ttl) *ttl = (he->expires - t);
			return he->value;
		}
		else hep=&(he->next);
	}
	pthread_mutex_unlock(&hash_mutex);
	return 0;
}

unsigned long nservers[MAXNSERVERS] = {0, 0, 0, 0, 0};


unsigned long udpresolve(unsigned char * name, unsigned *retttl, struct clientparam* param){

	int i;
	unsigned long retval;

	if((retval = hashresolv(name, retttl))) {
		return retval;
	}
	
	for(i=0; i<MAXNSERVERS && nservers[i]; i++){
		unsigned short nquery, nq, na;
		unsigned char buf[4096], *s1, *s2;
		int j, k, len, flen;
		SOCKET sock;
		unsigned ttl;
		time_t t;
		struct sockaddr_in sin, *sinsp;

		sinsp = param? &param->sins : &sin;
		


		if((sock=socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == INVALID_SOCKET) break;
		sinsp->sin_family = AF_INET;
		sinsp->sin_port = htons(0);
		sinsp->sin_addr.s_addr = htonl(0);
		if(bind(sock,(struct sockaddr *)sinsp,sizeof(struct sockaddr_in))) {
			shutdown(sock, SHUT_RDWR);
			closesocket(sock);
			break;
		}
		sinsp->sin_addr.s_addr = nservers[i];
		sinsp->sin_port = htons(53);

		len = strlen((char *)name);
		nquery = myrand(name, len);
		*(unsigned short*)buf = nquery; /* query id */
		buf[2] = 1; 			/* recursive */
		buf[3] = 0;
		buf[4] = 0;
		buf[5] = 1;			/* 1 request */
		buf[6] = buf[7] = 0;		/* no replies */
		buf[8] = buf[9] = 0;		/* no ns count */
		buf[10] = buf[11] = 0;		/* no additional */
		if(len > 255) {
			len = 255;
		}
		memcpy(buf + 13, name, len);
		len += 13;
		buf[len] = 0;
		for(s2 = buf + 12; (s1 = (unsigned char *)strchr((char *)s2 + 1, '.')); s2 = s1)*s2 = (unsigned char)((s1 - s2) - 1);
		*s2 = (len - (s2 - buf)) - 1;
		len++;
		buf[len++] = 0;
		buf[len++] = 1;			/* host address */
		buf[len++] = 0;
		buf[len++] = 1;			/* INET */
		if(socksendto(sock, sinsp, buf, len, conf.timeouts[SINGLEBYTE_L]*1000) != len){
			shutdown(sock, SHUT_RDWR);
			closesocket(sock);
			continue;
		}
		if(param) param->statscli += len;
		len = sockrecvfrom(sock, sinsp, buf, 4096, 15000);
		shutdown(sock, SHUT_RDWR);
		closesocket(sock);
		if(len <= 13) continue;
		if(param) param->statssrv += len;
		if(*(unsigned short *)buf != nquery)continue;
		if((na = buf[7] + (((unsigned short)buf[6])<<8)) < 1) {
			return 0;
		}
		nq = buf[5] + (((unsigned short)buf[4])<<8);
		if (nq != 1) {
			continue;			/* we did only 1 request */
		}
		for(k = 13; k<len && buf[k]; k++) {
		}
		k++;
		if( (k+4) >= len) {
			continue;
		}
		k += 4;
		if(na > 255) na = 255;			/* somebody is very evil */
		for (j = 0; j < na; j++) {		/* now there should be answers */
			if((k+16) > len) {
				break;
			}
			flen = buf[k+11] + (((unsigned short)buf[k+10])<<8);
			if((k+12+flen) > len) break;
			if(buf[k+2] != 0 || buf[k+3] != 1 || flen != 4) {
				k+= (12 + flen);
				continue; 		/* we need A IPv4 */
			}
			retval = *(unsigned long *)(buf + k + 12);
			ttl = ntohl(*(unsigned long *)(buf + k + 6));
			t = time(0);
			if(ttl < 60 || ((unsigned)t)+ttl < ttl) ttl = 300;
			if(ttl)hashadd(name, retval, ((unsigned)t)+ttl);
			if(retttl) *retttl = ttl;
			return retval;
		}
	}
	return 0;
}

unsigned long myresolver(unsigned char * name){
 return udpresolve(name, NULL, NULL);
}

unsigned long fakeresolver (unsigned char *name){
 return htonl(0x7F000002);
}

#ifndef NOODBC

SQLHENV  henv = NULL;
SQLHSTMT hstmt = NULL;
SQLHDBC hdbc = NULL;
char * sqlstring = NULL;


void close_sql(){
	if(hstmt) {
		SQLFreeHandle(SQL_HANDLE_STMT, hstmt);
		hstmt = NULL;
	}
	if(hdbc){
		SQLDisconnect(hdbc);
		SQLFreeHandle(SQL_HANDLE_DBC, hdbc);
		hdbc = NULL;
	}
	if(henv) {
		SQLFreeHandle(SQL_HANDLE_ENV, henv);
		henv = NULL;
	}
}

int init_sql(char * s){
	SQLRETURN  retcode;
	char * datasource;
	char * username;
	char * password;
	char * string;

	if(!s) return 0;
	if(!sqlstring || strcmp(sqlstring, s)){
		string = sqlstring;
		sqlstring=mystrdup(s);
		if(string)myfree(string);
	}

	if(hstmt || hdbc || henv) close_sql();
	if(!henv){
		retcode = SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &henv);
		if (!henv || (retcode != SQL_SUCCESS && retcode != SQL_SUCCESS_WITH_INFO)){
			henv = NULL;
			return 0;
		}
		retcode = SQLSetEnvAttr(henv, SQL_ATTR_ODBC_VERSION, (void*)SQL_OV_ODBC3, 0); 

		if (retcode != SQL_SUCCESS && retcode != SQL_SUCCESS_WITH_INFO) {
			return 0;
		}
	}
	if(!hdbc){
		retcode = SQLAllocHandle(SQL_HANDLE_DBC, henv, &hdbc); 
		if (!hdbc || (retcode != SQL_SUCCESS && retcode != SQL_SUCCESS_WITH_INFO)) {
			hdbc = NULL;
			SQLFreeHandle(SQL_HANDLE_ENV, henv);
			henv = NULL;
			return 0;
		}
	       	SQLSetConnectAttr(hdbc, SQL_LOGIN_TIMEOUT, (void*)15, 0);
	}
	string = mystrdup(sqlstring);
	if(!string) return 0;
	datasource = strtok(string, ",");
	username = strtok(NULL, ",");
	password = strtok(NULL, ",");
	

         /* Connect to data source */
        retcode = SQLConnect(hdbc, (SQLCHAR*) datasource, (SQLSMALLINT)strlen(datasource),
                (SQLCHAR*) username, (SQLSMALLINT)((username)?strlen(username):0),
                (SQLCHAR*) password, (SQLSMALLINT)((password)?strlen(password):0));

	myfree(string);
	if (retcode != SQL_SUCCESS && retcode != SQL_SUCCESS_WITH_INFO){
		SQLFreeHandle(SQL_HANDLE_DBC, hdbc);
		hdbc = NULL;
		SQLFreeHandle(SQL_HANDLE_ENV, henv);
		henv = NULL;
		return 0;
	}
        retcode = SQLAllocHandle(SQL_HANDLE_STMT, hdbc, &hstmt); 
        if (retcode != SQL_SUCCESS && retcode != SQL_SUCCESS_WITH_INFO){
		close_sql();
		return 0;
	}
	return 1;
}

void logsql(struct clientparam * param, const unsigned char *s) {
	unsigned char buf[4096];
	SQLRETURN ret;
	int len;

	len = dobuf(param, buf, s, "\'");
#ifdef SAFESQL
	pthread_mutex_lock(&odbc_mutex);
#endif
	if(!hstmt){
#ifndef SAFESQL
		pthread_mutex_lock(&odbc_mutex);
#endif
		if(!init_sql(sqlstring)) {
			pthread_mutex_unlock(&odbc_mutex);
			return;
		}
#ifndef SAFESQL
		pthread_mutex_unlock(&odbc_mutex);
#endif
	}
	if(hstmt){
		ret = SQLExecDirect(hstmt, (SQLCHAR *)buf, (SQLINTEGER)len);
		if(ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO){
#ifndef SAFESQL
			pthread_mutex_lock(&odbc_mutex);
#endif
			close_sql();
			if(!init_sql(sqlstring)){
				pthread_mutex_unlock(&odbc_mutex);
				return;
			}
#ifndef SAFESQL
		pthread_mutex_unlock(&odbc_mutex);
#endif
			if(hstmt) SQLExecDirect(hstmt, (SQLCHAR *)buf, (SQLINTEGER)len);
		}
		
	}
#ifdef SAFESQL
	pthread_mutex_unlock(&odbc_mutex);
#endif
}

#endif
 
#ifdef WITHMAIN
int main(int argc, unsigned char * argv[]) {
	unsigned ip = 0;
 WSADATA wd;
 WSAStartup(MAKEWORD( 1, 1 ), &wd);
	if(argc == 2)ip=getip(argv[1]);
	if(!hp) {
		printf("Not found");
		return 0;
	}
	printf("Name: '%s'\n", getnamebyip(ip);
	return 0;
}
#endif
