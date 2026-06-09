/*
 * lexer.h — Interfaz del analizador léxico.
 */
#ifndef LEXER_H
#define LEXER_H

#include "../include/common.h"

typedef enum {
    TOK_KEYWORD,
    TOK_ID,
    TOK_NUMBER,
    TOK_FLOAT,
    TOK_STRING,
    TOK_BOOL,
    TOK_LPAREN,
    TOK_RPAREN,
    TOK_OP,
    TOK_COMMA,
    TOK_SEMICOLON,
    TOK_EOF,
    TOK_ERROR
} TokenType;

typedef enum {
    LERR_NONE = 0,
    LERR_01, /* carácter inválido */
    LERR_02, /* string no cerrada */
    LERR_03, /* número mal formado */
    LERR_04, /* identificador mal formado */
    LERR_05, /* token demasiado largo */
    LERR_06, /* secuencia ilegal */
    LERR_07  /* EOF inesperado */
} LexErrCode;

typedef struct Token {
    TokenType   tipo;
    char        valor[MAX_LEXEME];
    int         linea;
    int         columna;
    int         cod_err;
} Token;

typedef struct TokenList {
    Token  *items;
    size_t  count;
    size_t  cap;
} TokenList;

TokenList lexer_tokenize(const char *source, const char *filename, Diag *diag);
void      lexer_dump_tokens(const TokenList *tl);
void      token_list_free(TokenList *tl);

#endif /* LEXER_H */
