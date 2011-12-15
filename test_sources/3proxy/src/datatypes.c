/*
 * Copyright (c) 2000-2006 3APA3A
 *
 * please read License Agreement
 *
 * $Id: datatypes.c,v 1.15 2006/03/10 19:25:47 vlad Exp $
 */

#include "proxy.h"

static void pr_integer(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	if(node->value)(*cbf)(cb, buf, sprintf(buf, "%d", *(int *)node->value));
}

static void pr_short(struct node *node, CBFUNC cbf, void*cb){
	char buf[8];
	if(node->value)(*cbf)(cb, buf, sprintf(buf, "%hu", *(unsigned short*)node->value));
}

static void pr_char(struct node *node, CBFUNC cbf, void*cb){
	if(node->value)(*cbf)(cb, (char *)node->value, 1);
}


static void pr_unsigned(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	if(node->value)(*cbf)(cb, buf, sprintf(buf, "%u", *(unsigned *)node->value));
}

static void pr_traffic(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	unsigned long u1, u2;
	if(node->value){
		u1 = ((unsigned long *)node->value)[0];
		u2 = ((unsigned long *)node->value)[0];
		(*cbf)(cb, buf, sprintf(buf, "%lu", (u1>>20) + (u2<<10)));
	}
}

static void pr_port(struct node *node, CBFUNC cbf, void*cb){
	char buf[8];
	if(node->value)(*cbf)(cb, buf, sprintf(buf, "%hu", ntohs(*(unsigned short*)node->value)));
}

static void pr_datetime(struct node *node, CBFUNC cbf, void*cb){
	char *s;
	if(node->value){
		s = ctime((time_t *)node->value);
		(*cbf)(cb, s, strlen(s)-1);
	}
}

int ipprint(char *buf, unsigned uu){
	unsigned u = ntohl(uu);

	return sprintf(buf, "%u.%u.%u.%u", 
		((u&0xFF000000)>>24), 
		((u&0x00FF0000)>>16),
		((u&0x0000FF00)>>8),
		((u&0x000000FF)));
}

static void pr_ip(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	if(node->value)(*cbf)(cb, buf, ipprint(buf, *(unsigned *)node -> value));
}

static void pr_wdays(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	int i, found = 0;
	if(node -> value)for(i = 0; i<8; i++){
		if( (1<<i) & *(int *)node -> value ) {
			sprintf(buf, "%s%d", found?",":"", i);
			(*cbf)(cb, buf, found? 2:1);
			found = 1;
		}
	}
}

static void pr_time(struct node *node, CBFUNC cbf, void*cb){
	char buf[16];
	int t = *(int *)node;
	
	(*cbf)(cb, buf, sprintf(buf, "%02d:%02d:%02d", (t/3600)%24, (t/60)%60, t%60));
}

int cidrprint(char *buf, unsigned long u){
	unsigned long u1 = 0xffffffff;
	int i;

	u = ntohl(u);
	for(i = 32; i && (u1!=u); i--){
		u1 = (u1 << 1);
	}
	if (i == 32) {
		return 0;
	}
	return sprintf(buf, "/%d", i);
}

static void pr_cidr(struct node *node, CBFUNC cbf, void*cb){
	char buf[4];
	int i;

	if(node->value){
		if ((i = cidrprint(buf, *(unsigned *)node -> value)))
		 (*cbf)(cb, buf, i);
		else (*cbf)(cb, "/32", 3);
	}
}

static void pr_string(struct node *node, CBFUNC cbf, void*cb){
	if(node->value){
		(*cbf)(cb, (char*)node->value, strlen((char*)node->value));
	}
	else (*cbf)(cb, "(NULL)", 6);
}

static void pr_rotation(struct node *node, CBFUNC cbf, void*cb){
	char * strings[] = {
		"N", "C", "H", "D", "W", "M", "Y", "N"
	};
	int i;

	if(node->value && (i = *(int*)node->value) > 1 && i < 6){
	 (*cbf)(cb, strings[i], 1);
	}
}

static void pr_operations(struct node *node, CBFUNC cbf, void*cb){
	char buf[64];
	int operation;
	int delim = 0;

	*buf = 0;
	if(!node->value || !(operation = *(int*)node->value)){
		(*cbf)(cb, "*", 1);
		return;
	}
	if(operation & HTTP){
		if((operation & HTTP) == HTTP)
		 (*cbf)(cb, buf, sprintf(buf, "HTTP"));
		else 
		 (*cbf)(cb, buf, sprintf(buf, "%s%s%s%s%s%s%s%s%s",
			(operation & HTTP_GET)? "HTTP_GET" : "",
			((operation & HTTP_GET) && (operation & (HTTP_PUT|HTTP_POST|HTTP_HEAD|HTTP_OTHER)))? "," : "",
			(operation & HTTP_PUT)? "HTTP_PUT" : "",
			((operation & HTTP_PUT) && (operation & (HTTP_POST|HTTP_HEAD|HTTP_OTHER)))? "," : "",
			(operation & HTTP_POST)? "HTTP_POST" : "",
			((operation & HTTP_POST) && (operation & (HTTP_HEAD|HTTP_OTHER)))? "," : "",
			(operation & HTTP_HEAD)? "HTTP_HEAD" : "",
			((operation & HTTP_HEAD) && (operation & HTTP_OTHER))? "," : "",
			(operation & HTTP_OTHER)? "HTTP_OTHER" : ""));
		delim = 1;
	}
	if(operation & HTTP_CONNECT){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "HTTP_CONNECT"));
		delim = 1;
	}
	if(operation & FTP) {
		if((operation & FTP) == FTP)
		 (*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "FTP"));
		else
		 (*cbf)(cb, buf, sprintf(buf, "%s%s%s%s%s%s",
			delim? ",":"",
			(operation & FTP_GET)? "FTP_GET" : "",
			((operation & FTP_GET) && (operation & (FTP_PUT|FTP_LIST)))? ",":"",
			(operation & FTP_PUT)? "FTP_PUT" : "",
			((operation & FTP_PUT) && (operation & FTP_LIST))? ",":"",
			(operation & FTP_LIST)? "FTP_LIST" : ""));
		delim = 1;
	}
	if(operation & CONNECT){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "CONNECT"));
		delim = 1;
	}
	if(operation & BIND){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "BIND"));
		delim = 1;
	}
	if(operation & UDPASSOC){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "UDPASSOC"));
		delim = 1;
	}
	if(operation & ICMPASSOC){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "ICMPASSOC"));
		delim = 1;
	}
	if(operation & DNSRESOLVE){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "DNSRESOLVE"));
		delim = 1;
	}
	if(operation & ADMIN){
		(*cbf)(cb, buf, sprintf(buf, "%s%s", delim?",":"", "ADMIN"));
	}
}

static void pr_portlist(struct node *node, CBFUNC cbf, void*cb){
	struct portlist *pl= (struct portlist *)node->value;
	char buf[16];
	if(!pl) {
		(*cbf)(cb, "*", 1);
		return;
	}
	for(; pl; pl = pl->next) {
		if(pl->startport == pl->endport)
		 (*cbf)(cb, buf, sprintf(buf, "%hu", pl->startport));
		else
		 (*cbf)(cb, buf, sprintf(buf, "%hu-%hu", pl->startport, pl->endport));
		if(pl->next)(*cbf)(cb, ",", 1);
	}
}

static void pr_userlist(struct node *node, CBFUNC cbf, void*cb){
	struct userlist *ul= (struct userlist *)node->value;
	if(!ul) {
		(*cbf)(cb, "*", 1);
		return;
	}
	for(; ul; ul = ul->next){
	 (*cbf)(cb, (char *)ul->user, strlen((char *)ul->user));
	 if(ul->next)(*cbf)(cb, ",", 1);
	}
}

static void pr_iplist(struct node *node, CBFUNC cbf, void*cb){
	char buf[20];
	int i;
	struct iplist *il = (struct iplist *)node->value;

	if(!il) {
		(*cbf)(cb, "*", 1);
		return;
	}
	for(; il; il = il->next){
	 i = ipprint(buf, il->ip);
	 i += cidrprint(buf+i, il->mask);
	 if(il->next)buf[i++] = ',';
	 (*cbf)(cb, buf, i);
	}
}

static void * ef_portlist_next(struct node *node){
	return (((struct portlist *)node->value) -> next);
}


static void * ef_portlist_start(struct node *node){
	return &(((struct portlist *)node->value) -> startport);
}

static void * ef_portlist_end(struct node *node){
	return &(((struct portlist *)node->value) -> endport);
}

static void * ef_iplist_next(struct node *node){
	return (((struct iplist *)node->value) -> next);
}

static void * ef_iplist_ip(struct node *node){
	return &(((struct iplist *)node->value) -> ip);
}

static void * ef_iplist_cidr(struct node *node){
	return &(((struct iplist *)node->value) -> mask);
}

static void * ef_iplist_mask(struct node *node){
	return &(((struct iplist *)node->value) -> mask);
}

static void * ef_userlist_next(struct node * node){
	return (((struct userlist *)node->value) -> next);
}

static void * ef_userlist_user(struct node * node){
	return (((struct userlist *)node->value) -> user);
}

static void * ef_pwlist_next(struct node * node){
	return (((struct passwords *)node->value) -> next);
}

static void * ef_pwlist_user(struct node * node){
	return (((struct passwords *)node->value) -> user);
}

static void * ef_pwlist_password(struct node * node){
	return (((struct passwords *)node->value) -> password);
}

static void * ef_pwlist_type(struct node * node){
	switch (((struct passwords *)node->value) -> pwtype) {
		case SYS:
			return "SYS";
		case CL:
			return "CL";
		case CR:
			return "CR";
		case NT:
			return "NT";
		case LM:
			return "LM";
		default:
			return "";
	}
}

static void * ef_chain_next(struct node * node){
	return ((struct chain *)node->value) -> next;
}

static void * ef_chain_type(struct node * node){
	switch (((struct chain *)node->value) -> type) {
		case R_TCP:
			return "tcp";
		case R_CONNECT:
			return "connect";
		case R_SOCKS4:
			return "socks4";
		case R_SOCKS5:
			return "socks5";
		case R_HTTP:
			return "http";
		case R_FTP:
			return "ftp";
		case R_POP3:
			return "pop3";
		default:
			return "";
	}
}

static void * ef_chain_ip(struct node * node){
	return &((struct chain *)node->value) -> redirip;
}

static void * ef_chain_port(struct node * node){
	return &((struct chain *)node->value) -> redirport;
}

static void * ef_chain_weight(struct node * node){
	return &((struct chain *)node->value) -> weight;
}

static void * ef_chain_user(struct node * node){
	return ((struct chain *)node->value) -> extuser;
}

static void * ef_chain_password(struct node * node){
	return ((struct chain *)node->value) -> extpass;
}

static void * ef_ace_next(struct node * node){
	return ((struct ace *)node->value) -> next;
}

static void * ef_ace_type(struct node * node){
	switch (((struct ace *)node->value) -> action) {
		case ALLOW:
		case REDIRECT:
			return "allow";
		case DENY:
			return "deny";
		case BANDLIM:
			return "bandlim";
		case NOBANDLIM:
			return "nobandlim";
		case COUNT:
			return "count";
		case NOCOUNT:
			return "nocount";
		default:
			return "unknown";
	}
}


static void * ef_ace_operations(struct node * node){
	if(!((struct ace *)node->value) -> operation) return NULL;
	return &((struct ace *)node->value) -> operation;
}

static void * ef_ace_users(struct node * node){
	return ((struct ace *)node->value) -> users;
}

static void * ef_ace_src(struct node * node){
	return ((struct ace *)node->value) -> src;
}


static void * ef_ace_dst(struct node * node){
	return ((struct ace *)node->value) -> dst;
}


static void * ef_ace_ports(struct node * node){
	return ((struct ace *)node->value) -> ports;
}

static void * ef_ace_chain(struct node * node){
	return ((struct ace *)node->value) -> chains;
}

static void * ef_ace_weekdays(struct node * node){
	return (((struct ace *)node->value) -> wdays) ? &((struct ace *)node->value) -> wdays : NULL;
}

static void * ef_ace_period(struct node * node){
	return ((struct ace *)node->value) -> periods;
}


static void * ef_bandlimit_next(struct node * node){
	return ((struct bandlim *)node->value) -> next;
}

static void * ef_bandlimit_ace(struct node * node){
	return ((struct bandlim *)node->value) -> ace;
}

static void * ef_bandlimit_rate(struct node * node){
	return &((struct bandlim *)node->value) -> rate;
}

static void * ef_trafcounter_next(struct node * node){
	return ((struct trafcount *)node->value) -> next;
}

static void * ef_trafcounter_ace(struct node * node){
	return ((struct trafcount *)node->value) -> ace;
}

static void * ef_trafcounter_number(struct node * node){
	return &((struct trafcount *)node->value) -> number;
}

static void * ef_trafcounter_type(struct node * node){
	return &((struct trafcount *)node->value) -> type;
}

static void * ef_trafcounter_traffic(struct node * node){
	return &((struct trafcount *)node->value) -> traf;
}

static void * ef_trafcounter_limit(struct node * node){
	return &((struct trafcount *)node->value) -> traflim;
}

static void * ef_trafcounter_cleared(struct node * node){
	return &((struct trafcount *)node->value) -> cleared;
}

static void * ef_trafcounter_updated(struct node * node){
	return &((struct trafcount *)node->value) -> updated;
}

static void * ef_trafcounter_comment(struct node * node){
	return ((struct trafcount *)node->value) -> comment;
}

static void * ef_trafcounter_disabled(struct node * node){
	return &((struct trafcount *)node->value) -> disabled;
}

static void * ef_client_next(struct node * node){
	return ((struct clientparam *)node->value) -> next;
}

static void * ef_client_child(struct node * node){
	return ((struct clientparam *)node->value) -> child;
}

static void * ef_client_maxtraf(struct node * node){
	return &((struct clientparam *)node->value) -> maxtraf;
}

static void * ef_client_type(struct node * node){
	int service = ((struct clientparam *)node->value) -> service;
	return (service>=0 && service < 15)? (void *)stringtable[SERVICES + service] : (void *)"unknown";
}

static void * ef_client_auth(struct node * node){
	AUTHFUNC af = ((struct clientparam *)node->value) -> authfunc;

	if(af == alwaysauth) return "none";
	if(af == nbnameauth) return "nbname";
	if(af == ipauth) return "iponly";
	if(af == strongauth) return "strong";
	return "uknown";
}

static void * ef_client_childcount(struct node * node){
	return ((struct clientparam *)node->value) -> childcount;
}

static void * ef_client_log(struct node * node){
	if(((struct clientparam *)node->value) -> logfunc == logstdout)	return "";
#ifndef _WIN32
	if(((struct clientparam *)node->value) -> logfunc == logsyslog)	return "@";
#endif
#ifndef NOODBC
	if(((struct clientparam *)node->value) -> logfunc == logsql)	return "&";
#endif
	return NULL;
}


static void * ef_client_logformat(struct node * node){
	return ((struct clientparam *)node->value) -> logformat;
}

static void * ef_client_nonprintable(struct node * node){
	return ((struct clientparam *)node->value) -> nonprintable;
}

static void * ef_client_replacement(struct node * node){
	if(((struct clientparam *)node->value) -> nonprintable)return &((struct clientparam *)node->value) -> replace;
	return NULL;
}

static void * ef_client_logtarget(struct node * node){
	return ((struct clientparam *)node->value) -> logtarget;
}

static void * ef_client_operation(struct node * node){
	if(!((struct clientparam *)node->value) -> operation) return NULL;
	return &((struct clientparam *)node->value) -> operation;
	
}

static void * ef_client_redirected(struct node * node){
	return &((struct clientparam *)node->value) -> redirected;
	
}

static void * ef_client_target(struct node * node){
	return ((struct clientparam *)node->value) -> target;
}

static void * ef_client_targetport(struct node * node){
	return &((struct clientparam *)node->value) -> targetport;
}

static void * ef_client_intip(struct node * node){
	return &((struct clientparam *)node->value) -> intip;
}

static void * ef_client_extip(struct node * node){
	return &((struct clientparam *)node->value) -> extip;
}

static void * ef_client_intport(struct node * node){
	return &((struct clientparam *)node->value) -> intport;
}

static void * ef_client_extport(struct node * node){
	return &((struct clientparam *)node->value) -> extport;
}

static void * ef_client_acl(struct node * node){
	return conf.acls[((struct clientparam *)node->value) -> aclnum];
}

static void * ef_client_aclnum(struct node * node){
	return &((struct clientparam *)node->value) -> aclnum;
}

static void * ef_client_hostname(struct node * node){
	return ((struct clientparam *)node->value) -> hostname;
}

static void * ef_client_username(struct node * node){
	return ((struct clientparam *)node->value) -> username;
}

static void * ef_client_password(struct node * node){
	return ((struct clientparam *)node->value) -> password;
}

static void * ef_client_extusername(struct node * node){
	return ((struct clientparam *)node->value) -> extusername;
}

static void * ef_client_extpassword(struct node * node){
	return ((struct clientparam *)node->value) -> extpassword;
}

static void * ef_client_cliip(struct node * node){
	return &((struct clientparam *)node->value) -> sinc.sin_addr.s_addr;
}

static void * ef_client_srvip(struct node * node){
	return &((struct clientparam *)node->value) -> sins.sin_addr.s_addr;
}

static void * ef_client_cliport(struct node * node){
	return &((struct clientparam *)node->value) -> sinc.sin_port;
}

static void * ef_client_srvport(struct node * node){
	return &((struct clientparam *)node->value) -> sins.sin_port;
}

static void * ef_client_bytesin(struct node * node){
	return &((struct clientparam *)node->value) -> statssrv;
}

static void * ef_client_bytesout(struct node * node){
	return &((struct clientparam *)node->value) -> statscli;
}

static void * ef_client_singlepacket(struct node * node){
	return &((struct clientparam *)node->value) -> singlepacket;
}

static void * ef_client_usentlm(struct node * node){
	return &((struct clientparam *)node->value) -> usentlm;
}

static void * ef_client_pwtype(struct node * node){
	return &((struct clientparam *)node->value) -> pwtype;
}

static void * ef_client_threadid(struct node * node){
	return &((struct clientparam *)node->value) -> threadid;
}

static void * ef_client_reqport(struct node * node){
	return &((struct clientparam *)node->value) -> reqport;
}

static void * ef_client_starttime(struct node * node){
	return &((struct clientparam *)node->value) -> time_start;
}

static void * ef_client_starttime_msec(struct node * node){
	return &((struct clientparam *)node->value) -> msec_start;
}

static void * ef_period_fromtime(struct node * node){
	return &((struct period *)node->value) -> fromtime;
}

static void * ef_period_totime(struct node * node){
	return &((struct period *)node->value) -> totime;
}

static void * ef_period_next(struct node * node){
	return ((struct period *)node->value) -> next;
}

static struct property prop_portlist[] = {
	{prop_portlist + 1, "start", ef_portlist_start, TYPE_PORT},
	{prop_portlist + 2, "end", ef_portlist_end, TYPE_PORT},
	{NULL, "next", ef_portlist_next, TYPE_PORTLIST}
};

static struct property prop_userlist[] = {
	{prop_userlist+1, "user", ef_userlist_user, TYPE_STRING},
	{NULL, "next", ef_userlist_next, TYPE_USERLIST}
};

static struct property prop_pwlist[] = {
	{prop_pwlist + 1, "user", ef_pwlist_user, TYPE_STRING},
	{prop_pwlist + 2, "password", ef_pwlist_password, TYPE_STRING},
	{prop_pwlist + 3, "type", ef_pwlist_type, TYPE_STRING},
	{NULL, "next", ef_pwlist_next, TYPE_PWLIST}
};

static struct property prop_iplist[] = {
	{prop_iplist + 1, "ip", ef_iplist_ip, TYPE_IP},
	{prop_iplist + 2, "cidr", ef_iplist_cidr, TYPE_CIDR},
	{prop_iplist + 3, "mask", ef_iplist_mask, TYPE_IP},
	{NULL, "next", ef_iplist_next, TYPE_IPLIST}
};

static struct property prop_chain[] = {
	{prop_chain + 1, "ip", ef_chain_ip, TYPE_IP},
	{prop_chain + 2, "port", ef_chain_port, TYPE_PORT},
	{prop_chain + 3, "type", ef_chain_type, TYPE_STRING},
	{prop_chain + 4, "weight", ef_chain_weight, TYPE_SHORT},
	{prop_chain + 5, "user", ef_chain_user, TYPE_STRING},
	{prop_chain + 6, "password", ef_chain_password, TYPE_STRING},
	{NULL, "next", ef_chain_next, TYPE_CHAIN}
};

static struct property prop_period[] = {
	{prop_period + 1, "fromtime", ef_period_fromtime, TYPE_TIME },
	{prop_period + 2, "totime", ef_period_totime, TYPE_TIME },
	{NULL, "next", ef_period_next, TYPE_PERIOD}
};

static struct property prop_ace[] = {
	{prop_ace + 1, "type", ef_ace_type, TYPE_STRING},
	{prop_ace + 2, "operations", ef_ace_operations, TYPE_OPERATIONS},
	{prop_ace + 3, "users", ef_ace_users, TYPE_USERLIST},
	{prop_ace + 4, "src", ef_ace_src, TYPE_IPLIST},
	{prop_ace + 5, "dst", ef_ace_dst, TYPE_IPLIST},
	{prop_ace + 6, "ports", ef_ace_ports, TYPE_PORTLIST},
	{prop_ace + 7, "chain", ef_ace_chain, TYPE_CHAIN},
	{prop_ace + 8, "wdays", ef_ace_weekdays, TYPE_WEEKDAYS},
	{prop_ace + 9, "periods", ef_ace_period, TYPE_PERIOD},
	{NULL, "next", ef_ace_next, TYPE_ACE}
};

static struct property prop_bandlimit[] = {
	{prop_bandlimit + 1, "ace", ef_bandlimit_ace, TYPE_ACE},
	{prop_bandlimit + 2, "rate", ef_bandlimit_rate, TYPE_UNSIGNED},
	{NULL, "next", ef_bandlimit_next, TYPE_BANDLIMIT}
};

static struct property prop_trafcounter[] = {
	{prop_trafcounter + 1, "disabled", ef_trafcounter_disabled, TYPE_INTEGER},
	{prop_trafcounter + 2, "ace", ef_trafcounter_ace, TYPE_ACE},
	{prop_trafcounter + 3, "number", ef_trafcounter_number, TYPE_UNSIGNED},
	{prop_trafcounter + 4, "type", ef_trafcounter_type, TYPE_ROTATION},
	{prop_trafcounter + 5, "traffic", ef_trafcounter_traffic, TYPE_TRAFFIC},
	{prop_trafcounter + 6, "limit", ef_trafcounter_limit, TYPE_TRAFFIC},
	{prop_trafcounter + 7, "cleared", ef_trafcounter_cleared, TYPE_DATETIME},
	{prop_trafcounter + 8, "updated", ef_trafcounter_updated, TYPE_DATETIME},
	{prop_trafcounter + 9, "comment", ef_trafcounter_comment, TYPE_STRING},
	{NULL, "next", ef_trafcounter_next, TYPE_TRAFCOUNTER}
};

static struct property prop_client[] = {
	{prop_client + 1, "servicetype", ef_client_type, TYPE_STRING},
	{prop_client + 2, "threadid", ef_client_threadid, TYPE_INTEGER},
	{prop_client + 3, "starttime", ef_client_starttime, TYPE_DATETIME},
	{prop_client + 4, "starttime_msec", ef_client_starttime_msec, TYPE_UNSIGNED},
	{prop_client + 5, "auth", ef_client_auth, TYPE_STRING},
	{prop_client + 6, "childcount", ef_client_childcount, TYPE_INTEGER},
	{prop_client + 7, "log", ef_client_log, TYPE_STRING},
	{prop_client + 8, "logformat", ef_client_logformat, TYPE_STRING},
	{prop_client + 9, "nonprintable", ef_client_nonprintable, TYPE_STRING},
	{prop_client + 10, "replacement", ef_client_replacement, TYPE_CHAR},
	{prop_client + 11, "logtarget", ef_client_logtarget, TYPE_STRING},
	{prop_client + 12, "operation", ef_client_logtarget, TYPE_OPERATIONS},
	{prop_client + 13, "redirected", ef_client_redirected, TYPE_INTEGER},
	{prop_client + 14, "target", ef_client_target, TYPE_STRING},
	{prop_client + 15, "targetport", ef_client_targetport, TYPE_PORT},
	{prop_client + 16, "operation", ef_client_operation, TYPE_OPERATIONS},
	{prop_client + 17, "intip", ef_client_intip, TYPE_IP},
	{prop_client + 18, "extip", ef_client_extip, TYPE_IP},
	{prop_client + 19, "intport", ef_client_intport, TYPE_PORT},
	{prop_client + 20, "extport", ef_client_extport, TYPE_PORT},
	{prop_client + 21, "acl", ef_client_acl, TYPE_ACE},
	{prop_client + 22, "aclnum", ef_client_aclnum, TYPE_INTEGER},
	{prop_client + 23, "hostname", ef_client_hostname, TYPE_STRING},
	{prop_client + 24, "username", ef_client_username, TYPE_STRING},
	{prop_client + 25, "password", ef_client_password, TYPE_STRING},
	{prop_client + 26, "extusername", ef_client_extusername, TYPE_STRING},
	{prop_client + 27, "extpassword", ef_client_extpassword, TYPE_STRING},
	{prop_client + 28, "cliip", ef_client_cliip, TYPE_IP},
	{prop_client + 29, "cliport", ef_client_cliport, TYPE_PORT},
	{prop_client + 30, "srvip", ef_client_srvip, TYPE_IP},
	{prop_client + 31, "srvport", ef_client_srvport, TYPE_PORT},
	{prop_client + 32, "bytesin", ef_client_bytesin, TYPE_UNSIGNED},
	{prop_client + 33, "bytesout", ef_client_bytesout, TYPE_UNSIGNED},
	{prop_client + 34, "singlepacket", ef_client_singlepacket, TYPE_INTEGER},
	{prop_client + 35, "usentlm", ef_client_usentlm, TYPE_INTEGER},
	{prop_client + 36, "pwtype", ef_client_pwtype, TYPE_INTEGER},
	{prop_client + 37, "reqport", ef_client_reqport, TYPE_PORT},
	{prop_client + 38, "child", ef_client_child, TYPE_CLIENT},
	{prop_client + 39, "maxtraf", ef_client_maxtraf, TYPE_UNSIGNED},
	{NULL, "next", ef_client_next, TYPE_CLIENT}

	
};

struct datatype datatypes[64] = {
	{"integer", NULL, pr_integer, NULL},
	{"short", NULL, pr_short, NULL},
	{"char", NULL, pr_char, NULL},
	{"unsigned", NULL, pr_unsigned, NULL},
	{"traffic", NULL, pr_traffic, NULL},
	{"port", NULL, pr_port, NULL},
	{"ip", NULL, pr_ip, NULL},
	{"cidr", NULL, pr_cidr, NULL},
	{"string", NULL, pr_string, NULL},
	{"datetime", NULL, pr_datetime, NULL},
	{"operations", NULL, pr_operations, NULL},
	{"rotation", NULL, pr_rotation, NULL},
	{"portlist", ef_portlist_next, pr_portlist, prop_portlist},
	{"iplist", ef_iplist_next, pr_iplist, prop_iplist},
	{"userlist", ef_userlist_next, pr_userlist, prop_userlist},
	{"pwlist", ef_pwlist_next, NULL, prop_pwlist},
	{"chain", ef_chain_next, NULL, prop_chain},
	{"ace", ef_ace_next, NULL, prop_ace},
	{"bandlimit", ef_bandlimit_next, NULL, prop_bandlimit},
	{"trafcounter", ef_trafcounter_next, NULL, prop_trafcounter},
	{"client", ef_trafcounter_next, NULL, prop_client},
	{"weekdays", NULL, pr_wdays, NULL},
	{"time", NULL, pr_time, NULL},
	{"period", ef_period_next, NULL, prop_period}
};
