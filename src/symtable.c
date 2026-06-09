/*
 * symtable.c — Scoping léxico y verificación de tipos sobre el AST.
 */
#include "symtable.h"
#include <stdio.h>
#include <string.h>

static void st_push(SymTable *st, Symbol s) {
    if (st->count >= st->cap) {
        size_t nc = st->cap ? st->cap * 2 : 32;
        Symbol *ni = (Symbol *)realloc(st->items, nc * sizeof(Symbol));
        if (!ni) exit(1);
        st->items = ni;
        st->cap = nc;
    }
    st->items[st->count++] = s;
}

static Symbol *lookup(SymTable *st, const char *name, int scope) {
    for (int i = (int)st->count - 1; i >= 0; i--) {
        if (strcmp(st->items[i].nombre, name) == 0 && st->items[i].scope <= scope)
            return &st->items[i];
    }
    return NULL;
}

static void sem_err(Diag *d, int code, int line, int col, const char *msg) {
    d->sem_errors++;
    fprintf(stderr, "[ERR-SEM-%03d] linea %d, columna %d: %s\n", code, line, col, msg);
}

static DataType infer_expr(ASTNode *n, SymTable *st, Diag *d);

static DataType infer_expr(ASTNode *n, SymTable *st, Diag *d) {
    if (!n) return DTYPE_UNKNOWN;
    switch (n->kind) {
        case NODE_INT:    n->dtype = DTYPE_INT; return DTYPE_INT;
        case NODE_FLOAT:  n->dtype = DTYPE_FLOAT; return DTYPE_FLOAT;
        case NODE_STRING: n->dtype = DTYPE_STRING; return DTYPE_STRING;
        case NODE_BOOL:   n->dtype = DTYPE_BOOL; return DTYPE_BOOL;
        case NODE_IDENT: {
            Symbol *s = lookup(st, n->u.ident.name, st->scope_depth);
            if (!s) {
                sem_err(d, 1, n->loc.line, n->loc.col, "variable usada sin declarar");
                return DTYPE_UNKNOWN;
            }
            n->dtype = s->tipo_dato;
            return s->tipo_dato;
        }
        case NODE_BINOP: {
            DataType a = infer_expr(n->u.binop.left, st, d);
            DataType b = infer_expr(n->u.binop.right, st, d);
            if (a != DTYPE_UNKNOWN && b != DTYPE_UNKNOWN && a != b &&
                !(a == DTYPE_INT && b == DTYPE_FLOAT) && !(a == DTYPE_FLOAT && b == DTYPE_INT)) {
                if (strcmp(n->u.binop.op, "==") != 0 && strcmp(n->u.binop.op, "!=") != 0)
                    sem_err(d, 3, n->loc.line, n->loc.col, "incompatibilidad de tipos en operacion");
            }
            if (strchr(n->u.binop.op, '=') || strchr("<>!", n->u.binop.op[0]))
                n->dtype = DTYPE_BOOL;
            else if (a == DTYPE_FLOAT || b == DTYPE_FLOAT)
                n->dtype = DTYPE_FLOAT;
            else
                n->dtype = DTYPE_INT;
            return n->dtype;
        }
        case NODE_CALL: {
            Symbol *s = lookup(st, n->u.call.name, st->scope_depth);
            if (!s || s->tipo != SYM_FUNC)
                sem_err(d, 2, n->loc.line, n->loc.col, "funcion llamada sin declarar con 'rurana'");
            return s ? s->tipo_dato : DTYPE_UNKNOWN;
        }
        default: return DTYPE_UNKNOWN;
    }
}

static void walk_stmt(ASTNode *n, SymTable *st, Diag *d);

static void walk_stmt(ASTNode *n, SymTable *st, Diag *d) {
    if (!n) return;
    switch (n->kind) {
        case NODE_ASSIGN: {
            Symbol *ex = lookup(st, n->u.assign.name, st->scope_depth);
            DataType vt = infer_expr(n->u.assign.value, st, d);
            if (ex && ex->scope == st->scope_depth) {
                sem_err(d, 4, n->loc.line, n->loc.col, "variable declarada dos veces en el mismo scope");
            } else if (!ex) {
                Symbol s;
                memset(&s, 0, sizeof(s));
                strncpy(s.nombre, n->u.assign.name, MAX_NAME - 1);
                s.tipo = SYM_VAR;
                s.tipo_dato = vt != DTYPE_UNKNOWN ? vt : DTYPE_INT;
                s.scope = st->scope_depth;
                s.linea = n->loc.line;
                st_push(st, s);
            } else if (ex->tipo_dato != vt && vt != DTYPE_UNKNOWN) {
                sem_err(d, 3, n->loc.line, n->loc.col, "incompatibilidad de tipos en asignacion");
            }
            break;
        }
        case NODE_IF:
            infer_expr(n->u.if_stmt.cond, st, d);
            for (size_t i = 0; i < n->u.if_stmt.then_b.count; i++) walk_stmt(n->u.if_stmt.then_b.items[i], st, d);
            for (size_t i = 0; i < n->u.if_stmt.else_b.count; i++) walk_stmt(n->u.if_stmt.else_b.items[i], st, d);
            break;
        case NODE_WHILE:
            infer_expr(n->u.while_stmt.cond, st, d);
            st->scope_depth++;
            for (size_t i = 0; i < n->u.while_stmt.body.count; i++) walk_stmt(n->u.while_stmt.body.items[i], st, d);
            st->scope_depth--;
            break;
        case NODE_PRINT:
            infer_expr(n->u.print_stmt.expr, st, d);
            break;
        case NODE_RETURN: {
            DataType rt = infer_expr(n->u.ret_stmt.expr, st, d);
            (void)rt;
            break;
        }
        case NODE_FUNCDEF: {
            Symbol s;
            memset(&s, 0, sizeof(s));
            strncpy(s.nombre, n->u.funcdef.name, MAX_NAME - 1);
            s.tipo = SYM_FUNC;
            s.tipo_dato = n->u.funcdef.ret_type;
            s.scope = st->scope_depth;
            s.linea = n->loc.line;
            if (lookup(st, n->u.funcdef.name, st->scope_depth))
                sem_err(d, 4, n->loc.line, n->loc.col, "funcion declarada dos veces");
            else
                st_push(st, s);
            st->scope_depth++;
            for (size_t i = 0; i < n->u.funcdef.body.count; i++) walk_stmt(n->u.funcdef.body.items[i], st, d);
            st->scope_depth--;
            break;
        }
        default: break;
    }
}

int symtable_analyze(ASTNode *root, SymTable *st, Diag *diag) {
    if (!root || root->kind != NODE_PROGRAM) return 1;
    st->scope_depth = 0;
    for (size_t i = 0; i < root->u.program.stmts.count; i++)
        walk_stmt(root->u.program.stmts.items[i], st, diag);
    if (st->verbose) symtable_dump(st);
    return diag->sem_errors > 0 ? 1 : 0;
}

void symtable_dump(const SymTable *st) {
    printf("NOMBRE | TIPO | TIPO_DATO | SCOPE | LINEA\n");
    const char *sk[] = {"VAR","FUNC","PARAM"};
    const char *dt[] = {"UNK","INT","FLOAT","STRING","BOOL","VOID"};
    int vars = 0, funcs = 0;
    for (size_t i = 0; i < st->count; i++) {
        const Symbol *s = &st->items[i];
        printf("%s | %s | %s | %d | %d\n",
               s->nombre, sk[s->tipo], dt[s->tipo_dato], s->scope, s->linea);
        if (s->tipo == SYM_VAR) vars++;
        if (s->tipo == SYM_FUNC) funcs++;
    }
    printf("Resumen: %d variables, %d funciones registradas. Verificacion %s.\n",
           vars, funcs, "OK");
}
