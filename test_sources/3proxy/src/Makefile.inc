#$Id: Makefile.inc,v 1.6 2005/05/06 09:35:05 vlad Exp $
#
# 3 proxy common Makefile
#


all:	pre 3proxy$(EXESUFFICS) mycrypt$(EXESUFFICS) dighosts$(EXESUFFICS) pop3p$(EXESUFFICS) ftppr$(EXESUFFICS) tcppm$(EXESUFFICS) udppm$(EXESUFFICS) socks$(EXESUFFICS) proxy$(EXESUFFICS)


pre:
	-cd .. && $(TYPECOMMAND) License
	$(PREMAKE)


sockmap$(OBJSUFFICS): sockmap.c proxy.h
	$(CC) $(CFLAGS) sockmap.c

common$(OBJSUFFICS): common.c proxy.h
	$(CC) $(CFLAGS) common.c

myalloc$(OBJSUFFICS): myalloc.c proxy.h
	$(CC) $(CFLAGS) myalloc.c

base64$(OBJSUFFICS): base64.c
	$(CC) $(CFLAGS) base64.c

ftp$(OBJSUFFICS): ftp.c proxy.h
	$(CC) $(CFLAGS) ftp.c

#$(COMPATLIBS): 
#	$(CC) $(CFLAGS) strncasecmp.c

sockgetchar$(OBJSUFFICS): sockgetchar.c proxy.h
	$(CC) $(CFLAGS) sockgetchar.c

proxy$(OBJSUFFICS): proxy.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)NOPORTMAP $(DEFINEOPTION)ANONYMOUS proxy.c

pop3p$(OBJSUFFICS): pop3p.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)NOPORTMAP pop3p.c

ftppr$(OBJSUFFICS): ftppr.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)NOPORTMAP ftppr.c

tcppm$(OBJSUFFICS): tcppm.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)PORTMAP tcppm.c

socks$(OBJSUFFICS): socks.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)NOPORTMAP socks.c

udppm$(OBJSUFFICS): udppm.c proxy.h proxymain.c
	$(CC) $(CFLAGS) $(DEFINEOPTION)WITHMAIN $(DEFINEOPTION)PORTMAP $(DEFINEOPTION)UDP udppm.c

3proxy$(OBJSUFFICS): 3proxy.c proxy.h
	$(CC) $(CFLAGS) 3proxy.c


proxy$(EXESUFFICS): sockmap$(OBJSUFFICS) proxy$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) base64$(OBJSUFFICS) ftp$(OBJSUFFICS) $(COMPATLIBS)
	$(LN) $(LNOUT)proxy$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) proxy$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) base64$(OBJSUFFICS) ftp$(OBJSUFFICS) $(COMPATLIBS) $(LIBS)

pop3p$(EXESUFFICS): sockmap$(OBJSUFFICS) pop3p$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(COMPATLIBS)
	$(LN) $(LNOUT)pop3p$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) pop3p$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(COMPATLIBS) $(LIBS)

ftppr$(EXESUFFICS): sockmap$(OBJSUFFICS) ftppr$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(COMPATLIBS)
	$(LN) $(LNOUT)ftppr$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) ftppr$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) ftp$(OBJSUFFICS) $(COMPATLIBS) $(LIBS)

socks$(EXESUFFICS): sockmap$(OBJSUFFICS) socks$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS)
	$(LN) $(LNOUT)socks$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) socks$(OBJSUFFICS) sockgetchar$(OBJSUFFICS)  myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(LIBS)

tcppm$(EXESUFFICS): sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) tcppm$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS)
	$(LN) $(LNOUT)tcppm$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) tcppm$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(LIBS)

udppm$(EXESUFFICS): sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) udppm$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS)
	$(LN) $(LNOUT)udppm$(EXESUFFICS) $(LDFLAGS) $(VERFILE) sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) udppm$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(LIBS)

tcpmainfunc$(OBJSUFFICS): proxy.h proxymain.c
	$(CC) $(COUT)tcpmainfunc$(OBJSUFFICS) $(CFLAGS) $(DEFINEOPTION)MODULEMAINFUNC=tcpmainfunc proxymain.c

udpmainfunc$(OBJSUFFICS): proxy.h proxymain.c
	$(CC) $(COUT)udpmainfunc$(OBJSUFFICS) $(CFLAGS) $(DEFINEOPTION)MODULEMAINFUNC=udpmainfunc $(DEFINEOPTION)UDP proxymain.c


srvproxy$(OBJSUFFICS): proxy.c proxy.h
	$(CC) $(COUT)srvproxy$(OBJSUFFICS) $(CFLAGS) proxy.c

srvpop3p$(OBJSUFFICS): pop3p.c proxy.h
	$(CC) $(COUT)srvpop3p$(OBJSUFFICS) $(CFLAGS) pop3p.c

srvftppr$(OBJSUFFICS): ftppr.c proxy.h
	$(CC) $(COUT)srvftppr$(OBJSUFFICS) $(CFLAGS) ftppr.c

srvtcppm$(OBJSUFFICS): tcppm.c proxy.h
	$(CC) $(COUT)srvtcppm$(OBJSUFFICS) $(CFLAGS) tcppm.c

srvsocks$(OBJSUFFICS): socks.c proxy.h
	$(CC) $(COUT)srvsocks$(OBJSUFFICS) $(CFLAGS) socks.c

srvwebadmin$(OBJSUFFICS): webadmin.c proxy.h
	$(CC) $(COUT)srvwebadmin$(OBJSUFFICS) $(CFLAGS) webadmin.c

srvudppm$(OBJSUFFICS): udppm.c proxy.h
	$(CC) $(COUT)srvudppm$(OBJSUFFICS) $(CFLAGS) udppm.c

srvdnspr$(OBJSUFFICS): dnspr.c proxy.h
	$(CC) $(COUT)srvdnspr$(OBJSUFFICS) $(CFLAGS) dnspr.c

auth$(OBJSUFFICS): auth.c proxy.h
	$(CC) $(COUT)auth$(OBJSUFFICS) $(CFLAGS) auth.c

datatypes$(OBJSUFFICS): datatypes.c proxy.h
	$(CC) $(COUT)datatypes$(OBJSUFFICS) $(CFLAGS) datatypes.c

mycrypt$(OBJSUFFICS): mycrypt.c
	$(CC) $(COUT)mycrypt$(OBJSUFFICS) $(CFLAGS) mycrypt.c

dighosts$(OBJSUFFICS): dighosts.c
	$(CC) $(COUT)dighosts$(OBJSUFFICS) $(CFLAGS) dighosts.c

dighosts$(EXESUFFICS): dighosts$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS)  $(COMPATLIBS)
	$(LN) $(LNOUT)dighosts$(EXESUFFICS) $(LDFLAGS) $(VERFILE) dighosts$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) $(COMPATLIBS) $(LIBS)

mycryptmain$(OBJSUFFICS): mycrypt.c
	$(CC) $(COUT)mycryptmain$(OBJSUFFICS) $(CFLAGS) $(DEFINEOPTION)WITHMAIN mycrypt.c

mycrypt$(EXESUFFICS): md4$(OBJSUFFICS) md5$(OBJSUFFICS) mycryptmain$(OBJSUFFICS) base64$(OBJSUFFICS)
	$(LN) $(LNOUT)mycrypt$(EXESUFFICS) $(VERFILE) $(LDFLAGS) md4$(OBJSUFFICS) md5$(OBJSUFFICS) base64$(OBJSUFFICS) mycryptmain$(OBJSUFFICS)


md4$(OBJSUFFICS):  libs/md4.h libs/md4.c
	$(CC) $(COUT)md4$(OBJSUFFICS) $(CFLAGS) libs/md4.c

smbdes$(OBJSUFFICS):  libs/smbdes.c
	$(CC) $(COUT)smbdes$(OBJSUFFICS) $(CFLAGS) libs/smbdes.c

md5$(OBJSUFFICS):  libs/md5.h libs/md5.c
	$(CC) $(COUT)md5$(OBJSUFFICS) $(CFLAGS) libs/md5.c

ntlm$(OBJSUFFICS):  ntlm.c
	$(CC) $(COUT)ntlm$(OBJSUFFICS) $(CFLAGS) ntlm.c

stringtable$(OBJSUFFICS):  stringtable.c
	$(CC) $(COUT)stringtable$(OBJSUFFICS) $(CFLAGS) stringtable.c

3proxy$(EXESUFFICS): 3proxy$(OBJSUFFICS) tcpmainfunc$(OBJSUFFICS) udpmainfunc$(OBJSUFFICS) srvproxy$(OBJSUFFICS) srvpop3p$(OBJSUFFICS) srvftppr$(OBJSUFFICS) srvsocks$(OBJSUFFICS) srvtcppm$(OBJSUFFICS) srvudppm$(OBJSUFFICS) sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) auth$(OBJSUFFICS) datatypes$(OBJSUFFICS) md4$(OBJSUFFICS) md5$(OBJSUFFICS) mycrypt$(OBJSUFFICS) base64$(OBJSUFFICS) ftp$(OBJSUFFICS) smbdes$(OBJSUFFICS) ntlm$(OBJSUFFICS) stringtable$(OBJSUFFICS) srvwebadmin$(OBJSUFFICS) srvdnspr$(OBJSUFFICS) $(COMPATLIBS)
	$(LN) $(LNOUT)3proxy$(EXESUFFICS) $(LDFLAGS) $(VERFILE)  3proxy$(OBJSUFFICS) tcpmainfunc$(OBJSUFFICS) udpmainfunc$(OBJSUFFICS) auth$(OBJSUFFICS) datatypes$(OBJSUFFICS) srvproxy$(OBJSUFFICS) srvpop3p$(OBJSUFFICS) srvftppr$(OBJSUFFICS) srvsocks$(OBJSUFFICS) srvtcppm$(OBJSUFFICS) srvudppm$(OBJSUFFICS) sockmap$(OBJSUFFICS) sockgetchar$(OBJSUFFICS) myalloc$(OBJSUFFICS) common$(OBJSUFFICS) mycrypt$(OBJSUFFICS) md5$(OBJSUFFICS) md4$(OBJSUFFICS) base64$(OBJSUFFICS) ftp$(OBJSUFFICS) smbdes$(OBJSUFFICS) ntlm$(OBJSUFFICS) stringtable$(OBJSUFFICS) srvwebadmin$(OBJSUFFICS) srvdnspr$(OBJSUFFICS) $(COMPATLIBS) $(LIBS)

clean:
	@$(REMOVECOMMAND) *$(OBJSUFFICS) $(COMPFILES)
