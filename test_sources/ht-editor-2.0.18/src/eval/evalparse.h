/* A Bison parser, made by GNU Bison 2.3.  */

/* Skeleton interface for Bison's Yacc-like parsers in C

   Copyright (C) 1984, 1989, 1990, 2000, 2001, 2002, 2003, 2004, 2005, 2006
   Free Software Foundation, Inc.

   This program is free software; you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation; either version 2, or (at your option)
   any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program; if not, write to the Free Software
   Foundation, Inc., 51 Franklin Street, Fifth Floor,
   Boston, MA 02110-1301, USA.  */

/* As a special exception, you may create a larger work that contains
   part or all of the Bison parser skeleton and distribute that work
   under terms of your choice, so long as that work isn't itself a
   parser generator using the skeleton or a modified version thereof
   as a parser skeleton.  Alternatively, if you modify or redistribute
   the parser skeleton itself, you may (at your option) remove this
   special exception, which will cause the skeleton and the resulting
   Bison output files to be licensed under the GNU General Public
   License without this special exception.

   This special exception was added by the Free Software Foundation in
   version 2.2 of Bison.  */

/* Tokens.  */
#ifndef YYTOKENTYPE
# define YYTOKENTYPE
   /* Put the tokens into the symbol table, so that GDB and other debuggers
      know about them.  */
   enum yytokentype {
     EVAL_INT = 258,
     EVAL_STR = 259,
     EVAL_FLOAT = 260,
     EVAL_IDENT = 261,
     EVAL_LAND = 262,
     EVAL_LXOR = 263,
     EVAL_LOR = 264,
     EVAL_EQ = 265,
     EVAL_NE = 266,
     EVAL_STR_EQ = 267,
     EVAL_STR_NE = 268,
     EVAL_LT = 269,
     EVAL_LE = 270,
     EVAL_GT = 271,
     EVAL_GE = 272,
     EVAL_STR_LT = 273,
     EVAL_STR_LE = 274,
     EVAL_STR_GT = 275,
     EVAL_STR_GE = 276,
     EVAL_SHL = 277,
     EVAL_SHR = 278,
     NEG = 279,
     EVAL_POW = 280
   };
#endif
/* Tokens.  */
#define EVAL_INT 258
#define EVAL_STR 259
#define EVAL_FLOAT 260
#define EVAL_IDENT 261
#define EVAL_LAND 262
#define EVAL_LXOR 263
#define EVAL_LOR 264
#define EVAL_EQ 265
#define EVAL_NE 266
#define EVAL_STR_EQ 267
#define EVAL_STR_NE 268
#define EVAL_LT 269
#define EVAL_LE 270
#define EVAL_GT 271
#define EVAL_GE 272
#define EVAL_STR_LT 273
#define EVAL_STR_LE 274
#define EVAL_STR_GT 275
#define EVAL_STR_GE 276
#define EVAL_SHL 277
#define EVAL_SHR 278
#define NEG 279
#define EVAL_POW 280




#if ! defined YYSTYPE && ! defined YYSTYPE_IS_DECLARED
typedef union YYSTYPE
#line 18 "evalparse.y"
{
	eval_scalar scalar;
	char *ident;
	eval_scalarlist scalars;
}
/* Line 1489 of yacc.c.  */
#line 105 "evalparse.h"
	YYSTYPE;
# define yystype YYSTYPE /* obsolescent; will be withdrawn */
# define YYSTYPE_IS_DECLARED 1
# define YYSTYPE_IS_TRIVIAL 1
#endif



