/*
	corehttp - single process nonblocking http server
	by frank yaul (frank723@gmail.com) 5 Aug 2005
	licensed under the academic free license version 1.2
	file: corehttp.h
*/

#ifndef COREHTTP_H
#define COREHTTP_H

#include <stdio.h>
#include <time.h>
#include <signal.h>
#include "common.h"
#include "sprock.h"
#include "watch.h"
#include "http.h"
#include "handler.h"

int main(int argc, char **argv);

#endif
