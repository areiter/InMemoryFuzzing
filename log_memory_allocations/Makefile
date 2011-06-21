# See LICENSE file for license and copyright information

include config.mk

LOG_MEM_SOURCE   = log_memory_allocations.c
LOG_MEM_OBJECTS  = ${LOG_MEM_SOURCE:.c=.o}

LOG_MEM_CLIENT_SOURCE = log_memory_allocations_client.c
LOG_MEM_CLIENT_OBJECTS = ${LOG_MEM_CLIENT_SOURCE:.c=.o}

all: options liblog_memory_allocations log_memory_allocations_client

options:
	@echo build options:
	@echo "CFLAGS  = ${CFLAGS}"
	@echo "LDFLAGS = ${LDFLAGS}"
	@echo "CC      = ${CC}"

OUTPUT_BUILD_LIB:
	@echo -e "\n\nBuilding liblog_memory_allocations"

OUTPUT_BUILD_CLIENT:
	@echo -e "\n\nBuilding log_memory_allocations_client"

${LOG_MEM_OBJECTS}: ${LOG_MEM_SOURCE}
	${CC} -c ${CFLAGS} $(LIBLDFLAGS)  -o $@ $<

${LOG_MEM_CLIENT_OBJECTS}: ${LOG_MEM_CLIENT_SOURCE}
	${CC} -c ${CFLAGS} $(LDFLAGS) -o $@ $<


liblog_memory_allocations: OUTPUT_BUILD_LIB ${LOG_MEM_OBJECTS}
	${CC} ${LIBLDFLAGS} -o liblog_memory_allocations.so $(LOG_MEM_OBJECTS)

log_memory_allocations_client: OUTPUT_BUILD_CLIENT ${LOG_MEM_CLIENT_OBJECTS}
	${CC} ${LDFLAGS} -o log_memory_allocations_client $(LOG_MEM_CLIENT_OBJECTS)

clean:
	rm -rf  ${LOG_MEM_OBJECTS} liblog_memory_allocations.so
	rm -rf  ${LOG_MEM_CLIENT_OBJECTS} log_memory_allocations_client
