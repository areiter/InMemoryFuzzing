/* See the file COPYING for conditions of use */

/* get png type definitions */
#include "png.h"

#define GIFterminator ';'
#define GIFextension '!'
#define GIFimage ','

#define GIFcomment 0xfe
#define GIFapplication 0xff
#define GIFplaintext 0x01
#define GIFgraphicctl 0xf9

#define MAXCMSIZE 256

typedef unsigned char byte;

typedef png_color GifColor;

struct GIFimagestruct {
  GifColor colors[MAXCMSIZE];
  unsigned long color_count[MAXCMSIZE];
  int offset_x;
  int offset_y;
  int width;
  int height;
  int trans;
  int interlace;
};

struct GIFelement {
  struct GIFelement *next;
  char GIFtype;
  byte *data;
  long allocated_size;
  long size;
  /* only used if GIFtype==GIFimage */
  struct GIFimagestruct *imagestruct;
};

extern struct gif_scr{
  unsigned int  Width;
  unsigned int  Height;
  GifColor      ColorMap[MAXCMSIZE];
  unsigned int  ColorMap_present;
  unsigned int  BitPixel;
  unsigned int  ColorResolution;
  int           Background;
  unsigned int  AspectRatio;
} GifScreen;

int ReadGIF(FILE *fd);
int MatteGIF(GifColor matte);

void allocate_element(void);
void store_block(char *data, int size);
void allocate_image(void);
void set_size(long);

void *xalloc(unsigned long s);
void *xrealloc(void *p, unsigned long s);
void fix_current(void);
void free_mem(void);

int interlace_line(int height, int line);
int inv_interlace_line(int height, int line);

extern struct GIFelement first;
extern struct GIFelement *current;
extern int recover;

extern const char version[];
extern const char compile_info[];

extern int skip_pte;

