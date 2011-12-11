/*
 * memory.c: memory storage for gif conversion
 * Copyright (C) 1995 Alexander Lehmann
 * For conditions of distribution and use, see the file COPYING.
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <assert.h>

#include "gif2png.h"

void *xalloc(unsigned long s)
{
  void *p=malloc((size_t)s);

  if(p==NULL) {
    fprintf(stderr, "gif2png: fatal error, out of memory\n");
    fprintf(stderr, "gif2png: exiting ungracefully\n");
    exit(1);
  }

  return p;
}

void *xrealloc(void *p, unsigned long s)
{
  p=realloc(p,(size_t)s);

  if(p==NULL) {
    fprintf(stderr, "gif2png: fatal error, out of memory\n");
    fprintf(stderr, "gif2png: exiting ungracefully\n");
    exit(1);
  }

  return p;
}

/* allocate a new GIFelement, advance current pointer and initialize
   size to 0 */

#define ALLOCSIZE 8192

void allocate_element(void)
{
  struct GIFelement *new=xalloc(sizeof(*new));

  memset(new, 0, sizeof(*new));

  new->next=NULL; /* just in case NULL is not represented by a binary 0 */

  current->next=new;
  current=new;
}

/* set size of current element to at least size bytes */
void set_size(long size)
{
  long nalloc;

  if(current->allocated_size==0) {
    nalloc=size;
    if(nalloc<ALLOCSIZE) nalloc=ALLOCSIZE;
    current->data=xalloc(nalloc);
    current->allocated_size=nalloc;
  } else
  if(current->allocated_size<size) {
    nalloc=size-current->allocated_size;
    if(nalloc<ALLOCSIZE) nalloc=ALLOCSIZE;
    current->data=xrealloc(current->data, current->allocated_size+nalloc);
    current->allocated_size+=nalloc;
  }
}

void store_block(char *data, int size)
{
  set_size(current->size+size);
  memcpy(current->data+current->size, data, size);
  current->size+=size;
}

void allocate_image(void)
{
  allocate_element();
  current->GIFtype=GIFimage;
  current->imagestruct=xalloc(sizeof(*current->imagestruct));
  memset(current->imagestruct, 0, sizeof(*current->imagestruct));
}

void fix_current(void)
{
  if(current->allocated_size!=current->size) {
    current->data=xrealloc(current->data, current->size);
    current->allocated_size=current->size;
  }
}

void free_mem(void)
{
  struct GIFelement *p,*t;

  p=first.next;
  first.next=NULL;

  while(p) {
    t=p;
    p=p->next;
    if(t->data) free(t->data);
    if(t->imagestruct) free(t->imagestruct);
    free(t);
  }
}

