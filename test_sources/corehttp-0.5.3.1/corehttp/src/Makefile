#
#	corehttp - single process nonblocking http server
#	by frank yaul (frank723@gmail.com) 5 Aug 2005
#	licensed under the academic free license version 1.2
#	file: Makefile
#

BINDIR =	/usr/local/bin
MANDIR = 	/usr/local/man/man7
CC =		gcc
CFLAGS = 	-pedantic -Wall

all: corehttp

install: all uninstall
	mkdir -p ${BINDIR} ${MANDIR}
	cp corehttp ${BINDIR}/corehttp
	cp ../doc/corehttp.7 ${MANDIR}/corehttp.7

clean:
	rm -f *.o corehttp

uninstall:
	rm -f ${BINDIR}/corehttp
	rm -f ${MANDIR}/corehttp.7

corehttp: common.o corehttp.o handler.o sprock.o watch.o http.o
	${CC} ${CFLAGS} common.o corehttp.o handler.o sprock.o watch.o http.o -o corehttp

corehttp.o: corehttp.c corehttp.h
	${CC} ${CFLAGS} -c corehttp.c

common.o: common.c common.h
	${CC} ${CFLAGS} -c common.c 

handler.o: handler.c handler.h
	${CC} ${CFLAGS} -c handler.c

sprock.o: sprock.c sprock.h
	${CC} ${CFLAGS} -c sprock.c

watch.o: watch.c watch.h
	${CC} ${CFLAGS} -c watch.c

http.o: http.c http.h
	${CC} ${CFLAGS} -c http.c

