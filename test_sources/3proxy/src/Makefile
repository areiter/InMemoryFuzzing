#$Id: Makefile.unix,v 1.5 2005/04/30 18:29:00 vlad Exp $
#
# 3 proxy Makefile for GCC/Unix/Linux/Cygwin
#
# You can try to add -DWITH_STD_MALLOC to CFLAGS to use standard malloc
# libraries


CC = gcc

# you may need -L/usr/pkg/lib for older NetBSD versions
CFLAGS = -Wall -g -O0 -c -pthread -D_THREAD_SAFE -D_REENTRANT -DNOODBC -DWITH_STD_MALLOC -DFD_SETSIZE=4096 -DWITH_POLL
COUT = -o 
LN = gcc
LDFLAGS = -Wall -O2 -pthread
# -lpthreads may be reuqired on some platforms instead of -pthreads
LIBS =
LNOUT = -o 
EXESUFFICS =
OBJSUFFICS = .o
DEFINEOPTION = -D
COMPFILES = *~
REMOVECOMMAND = rm -f
TYPECOMMAND = cat
COMPATLIBS =
MAKEFILE = Makefile.unix

include Makefile.inc
