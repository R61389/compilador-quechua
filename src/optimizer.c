/*
 * optimizer.c — Propagación, folding, dead code elimination y CSE básico.
 */
#include "optimizer.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

static int is_temp(const char *s) {
    return s[0] == 't' && isdigit((unsigned char)s[1]);
}

static int is_number(const char *s) {
    if (!s || !*s) return 0;
    char *end;
    strtol(s, &end, 10);
    return *end == '\0';
}

static long eval_bin(long a, long b, IROpcode op) {
    switch (op) {
        case IR_ADD: return a + b;
        case IR_SUB: return a - b;
        case IR_MUL: return a * b;
        case IR_DIV: return b ? a / b : 0;
        case IR_EQ:  return a == b;
        case IR_NEQ: return a != b;
        case IR_LT:  return a < b;
        case IR_GT:  return a > b;
        case IR_LEQ: return a <= b;
        case IR_GEQ: return a >= b;
        default: return 0;
    }
}

/* Mapa simple temp -> valor constante conocido */
#define MAP_SZ 256
static char map_name[MAP_SZ][MAX_TAC];
static char map_val[MAP_SZ][MAX_TAC];
static int  map_n;

static const char *map_get(const char *name) {
    for (int i = 0; i < map_n; i++)
        if (strcmp(map_name[i], name) == 0) return map_val[i];
    return NULL;
}

static void map_set(const char *name, const char *val) {
    for (int i = 0; i < map_n; i++) {
        if (strcmp(map_name[i], name) == 0) {
            strncpy(map_val[i], val, MAX_TAC - 1);
            return;
        }
    }
    if (map_n < MAP_SZ) {
        strncpy(map_name[map_n], name, MAX_TAC - 1);
        strncpy(map_val[map_n], val, MAX_TAC - 1);
        map_n++;
    }
}

static void map_clear(void) { map_n = 0; }

static int dest_used(const IRList *ir, size_t skip, const char *dest) {
    if (!dest[0] || !is_temp(dest)) return 1;
    for (size_t i = skip + 1; i < ir->count; i++) {
        const IROp *o = &ir->items[i];
        if (strcmp(o->dest, dest) == 0) continue;
        if (strcmp(o->src1, dest) == 0 || strcmp(o->src2, dest) == 0) return 1;
        if (o->op == IR_JUMPIF && strcmp(o->src1, dest) == 0) return 1;
    }
    return 0;
}

void optimize_ir(IRList *ir) {
    if (!ir || ir->count == 0) return;

    /* Paso 1: propagación + folding */
    map_clear();
    for (size_t i = 0; i < ir->count; i++) {
        IROp *o = &ir->items[i];
        if (o->op == IR_ASSIGN && is_temp(o->dest) && is_number(o->src1))
            map_set(o->dest, o->src1);

        const char *v1 = map_get(o->src1);
        const char *v2 = map_get(o->src2);
        if (v1) strncpy(o->src1, v1, MAX_TAC - 1);
        if (v2) strncpy(o->src2, v2, MAX_TAC - 1);

        if (o->op >= IR_ADD && o->op <= IR_GEQ &&
            is_number(o->src1) && is_number(o->src2) && is_temp(o->dest)) {
            long a = strtol(o->src1, NULL, 10);
            long b = strtol(o->src2, NULL, 10);
            long r = eval_bin(a, b, o->op);
            char buf[32];
            sprintf(buf, "%ld", r);
            o->op = IR_ASSIGN;
            strncpy(o->src1, buf, MAX_TAC - 1);
            o->src2[0] = 0;
            map_set(o->dest, buf);
        }
    }

    /* Paso 2: CSE básico — reutilizar temporales idénticos */
    for (size_t i = 0; i < ir->count; i++) {
        IROp *a = &ir->items[i];
        if (a->op < IR_ADD || a->op > IR_GEQ || !is_temp(a->dest)) continue;
        for (size_t j = 0; j < i; j++) {
            IROp *b = &ir->items[j];
            if (b->op == a->op && strcmp(b->src1, a->src1) == 0 && strcmp(b->src2, a->src2) == 0) {
                strncpy(a->dest, b->dest, MAX_TAC - 1);
                a->op = IR_ASSIGN;
                strncpy(a->src1, b->dest, MAX_TAC - 1);
                a->src2[0] = 0;
                break;
            }
        }
    }

    /* Paso 3: dead code — marcar no usados (solo ASSIGN a temp) */
    for (size_t i = 0; i < ir->count; i++) {
        IROp *o = &ir->items[i];
        if (o->op == IR_ASSIGN && is_temp(o->dest) && !dest_used(ir, i, o->dest))
            o->op = IR_LABEL; /* marca muerta: dest vacío, sin efecto si dest=="" */
    }

    /* Compactar instrucciones muertas marcadas */
    size_t w = 0;
    for (size_t i = 0; i < ir->count; i++) {
        IROp *o = &ir->items[i];
        if (o->op == IR_LABEL && o->dest[0] == 0 && o->src1[0] == 0 && o->src2[0] == 0)
            continue;
        if (o->op == IR_ASSIGN && is_temp(o->dest) && !dest_used(ir, i, o->dest))
            continue;
        ir->items[w++] = *o;
    }
    ir->count = w;
}
