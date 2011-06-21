# flags
CFLAGS  += -g
LIBLDFLAGS +=  -nostartfiles --shared -fPIC -ldl
LDFLAGS += 
# compiler
CC ?= gcc
