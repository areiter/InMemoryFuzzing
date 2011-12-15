#include <stdio.h>

unsigned char * strings[] = {
/* 00 */	(unsigned char *)"3proxy tiny proxy server v0.5.2 stringtable file",
/* 01 */	(unsigned char *)"3proxy",
/* 02 */	(unsigned char *)"3proxy tiny proxy server",
/* 03 */	(unsigned char *)"0.5.2",
/* 04 */	(unsigned char *)"3proxy allows to share and control Internet connection and count traffic",
/* 05 */	(unsigned char *)"SERVR",
/* 06 */	(unsigned char *)"PROXY",
/* 07 */	(unsigned char *)"TCPPM",
/* 08 */	(unsigned char *)"POP3P",
/* 09 */	(unsigned char *)"SOCK4",
/* 10 */	(unsigned char *)"SOCK5",
/* 11 */	(unsigned char *)"UDPPM",
/* 12 */	(unsigned char *)"SOCKS",
/* 13 */	(unsigned char *)"SOC45",
/* 14 */	(unsigned char *)"ADMIN",
/* 15 */	(unsigned char *)"DNSPR",
/* 16 */	(unsigned char *)"FTPPR",
/* 17 */	(unsigned char *)"ZOMBIE",
/* 18 */	NULL,
/* 19 */	NULL,
#ifndef TPROXY_CONF
#ifndef _WIN32
/* 20 */	(unsigned char *)"/usr/local/etc/3proxy.cfg",
#else
/* 20 */	(unsigned char *)"3proxy.cfg",
#endif
#else
/* 20 */       (unsigned char *)TPROXY_CONF,
#endif
/* 21 */	NULL,
/* 22 */	NULL,
/* 23 */	NULL,
/* 24 */	NULL,
/* 25 */	NULL,
/* 26 */	NULL,
/* 27 */	NULL,
/* 28 */	NULL,
/* 29 */	NULL,
/* 30 */	(unsigned char *)
	"<table align=\"center\" width=\"75%\"><tr><td>\n"
	"<h3>Welcome to 3proxy Web Interface</h3>\n"
	"Probably you've noticed interface is very ugly currently.\n"
	"It's because you have development version of 3proxy and interface\n"
	"is coded right now. What you see is a part of work that is done\n"
	"already.\n"
	"<p>It's planned 0.6 release of 3proxy to have extandable template based\n"
	"web interface, to show all required information about current\n"
	"configuration and connected clients and allow some of administrative\n"
	"tasks.\n"
	"<p>Please send all your comments to\n"
	"<A HREF=\"mailto:3proxy@security.nnov.ru\">3proxy@security.nnov.ru</A>\n"
	"<p>Documentation:\n"
	"<A HREF=\"http://www.security.nnov.ru/soft/3proxy\">http://www.security.nnov.ru/soft/3proxy</A>\n"
	"</tr></td></table>",
/* 31 */	NULL,
/* 32 */	NULL,
/* 33 */	NULL,
/* 34 */	NULL,
/* 35 */	NULL,
/* 36 */	NULL,
/* 37 */	NULL,
/* 38 */	NULL,
/* 39 */	NULL,
};

int constants[] = {0,0};
