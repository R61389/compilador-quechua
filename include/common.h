/*
 * common.h — Tipos base, arena allocator y macros de error compartidos.
 * Todas las etapas del compilador usan el arena para evitar fugas en C99.
 */
#ifndef COMMON_H
#define COMMON_H

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>
#include <stdint.h>
#include <stdbool.h>

#define MAX_LEXEME  128
#define MAX_NAME    128
#define MAX_TAC     64
#define ARENA_CHUNK (64 * 1024)

/* Arena: asignación por bloques; reset al final del compile. */
typedef struct Arena {
    char  *base;
    size_t used;
    size_t cap;
    struct Arena *next;
} Arena;

static inline void arena_init(Arena *a) {
    a->base = NULL;
    a->used = 0;
    a->cap  = 0;
    a->next = NULL;
}

static inline char *arena_alloc(Arena *a, size_t n) {
    n = (n + 7u) & ~7u;
    if (!a->base || a->used + n > a->cap) {
        size_t new_cap = a->cap ? a->cap * 2 : ARENA_CHUNK;
        while (new_cap < a->used + n) new_cap *= 2;
        char *nb = (char *)realloc(a->base, new_cap);
        if (!nb) { fprintf(stderr, "arena: sin memoria\n"); exit(1); }
        a->base = nb;
        a->cap  = new_cap;
    }
    char *p = a->base + a->used;
    a->used += n;
    return p;
}

static inline char *arena_strdup(Arena *a, const char *s) {
    size_t n = strlen(s) + 1;
    char *d = arena_alloc(a, n);
    memcpy(d, s, n);
    return d;
}

static inline void arena_free(Arena *a) {
    free(a->base);
    a->base = NULL;
    a->used = a->cap = 0;
}

/* Ubicación en fuente para mensajes de error */
typedef struct SrcLoc {
    int line;
    int col;
} SrcLoc;

#define ERR_BUF       256
#define MAX_DIAG_MSGS 16
#define DIAG_MAX_LINE 2048

typedef struct DiagMsg {
    int  code;
    int  line;
    int  col;
    char text[ERR_BUF];
} DiagMsg;

/* Contador global de errores por etapa */
typedef struct Diag {
    int lex_errors;
    int syn_errors;
    int sem_errors;
    DiagMsg syn_msgs[MAX_DIAG_MSGS];
    int     syn_msg_n;
    DiagMsg lex_msgs[MAX_DIAG_MSGS];
    int     lex_msg_n;
    unsigned char err_line[DIAG_MAX_LINE]; /* 1 = esa linea tiene error */
} Diag;

static inline void diag_reset(Diag *d) {
    d->lex_errors = d->syn_errors = d->sem_errors = 0;
    d->syn_msg_n = 0;
    d->lex_msg_n = 0;
    memset(d->err_line, 0, sizeof(d->err_line));
}

static inline void diag_mark_err_line(Diag *d, int line) {
    if (line > 0 && line < DIAG_MAX_LINE)
        d->err_line[line] = 1;
}

#endif /* COMMON_H */
