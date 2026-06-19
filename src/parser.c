/*
 * parser.c — Parser de descenso recursivo (gramatica quechua boliviana).
 */
#include "parser.h"
#include <stdio.h>
#include <string.h>

typedef struct Parser {
    Arena     *arena;
    TokenList *tok;
    size_t     pos;
    Diag      *diag;
    int        in_if;
} Parser;

static Token *cur(Parser *p) {
    return &p->tok->items[p->pos];
}
static int at_end(Parser *p) { return cur(p)->tipo == TOK_EOF; }

static Token *peek_at(Parser *p, size_t off) {
    size_t i = p->pos + off;
    if (i >= p->tok->count) return &p->tok->items[p->tok->count - 1];
    return &p->tok->items[i];
}

static int keyword_is(Parser *p, const char *kw) {
    Token *t = cur(p);
    return t->tipo == TOK_KEYWORD && strcmp(t->valor, kw) == 0;
}

static int keyword_at(Parser *p, size_t off, const char *kw) {
    Token *t = peek_at(p, off);
    return t->tipo == TOK_KEYWORD && strcmp(t->valor, kw) == 0;
}

static int expect_kw(Parser *p, const char *kw) {
    if (keyword_is(p, kw)) { p->pos++; return 1; }
    return 0;
}

/* Frases de dos palabras reservadas (quechua boliviano). */
static int match_kw2(Parser *p, const char *a, const char *b) {
    if (!keyword_is(p, a)) return 0;
    if (!keyword_at(p, 1, b)) return 0;
    p->pos += 2;
    return 1;
}

static int peek_kw2(Parser *p, const char *a, const char *b) {
    return keyword_is(p, a) && keyword_at(p, 1, b);
}

static int is_else_kw(Parser *p) {
    return peek_kw2(p, "mana", "chayqa");
}

static int is_end_kw(Parser *p) {
    return keyword_is(p, "tukukun");
}

static void syn_err(Parser *p, int code, const char *msg) {
    Token *t = cur(p);
    p->diag->syn_errors++;
    diag_mark_err_line(p->diag, t->linea);
    if (p->diag->syn_msg_n < MAX_DIAG_MSGS) {
        DiagMsg *m = &p->diag->syn_msgs[p->diag->syn_msg_n++];
        m->code = code;
        m->line = t->linea;
        m->col = t->columna;
        strncpy(m->text, msg, ERR_BUF - 1);
        m->text[ERR_BUF - 1] = '\0';
    }
}

static int accept(Parser *p, TokenType ty) {
    if (cur(p)->tipo == ty) { p->pos++; return 1; }
    return 0;
}

static ASTNode *parse_expr(Parser *p);
static ASTNode *parse_stmt(Parser *p);
static ASTNode *parse_add(Parser *p);
static ASTNode *parse_unary(Parser *p);
static ASTNode *parse_primary(Parser *p);
static void parse_block(Parser *p, ASTList *out);

static ASTNode *parse_primary(Parser *p) {
    Token *t = cur(p);
    if (match_kw2(p, "mana", "chiqap")) {
        ASTNode *n = ast_new(p->arena, NODE_BOOL, t->linea, t->columna);
        n->u.boolval.val = 0;
        n->dtype = DTYPE_BOOL;
        return n;
    }
    if (t->tipo == TOK_NUMBER) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_INT, t->linea, t->columna);
        n->u.intval.val = atol(t->valor);
        n->dtype = DTYPE_INT;
        return n;
    }
    if (t->tipo == TOK_FLOAT) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_FLOAT, t->linea, t->columna);
        n->u.floatval.val = atof(t->valor);
        n->dtype = DTYPE_FLOAT;
        return n;
    }
    if (t->tipo == TOK_STRING) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_STRING, t->linea, t->columna);
        n->u.strval.val = arena_strdup(p->arena, t->valor);
        n->dtype = DTYPE_STRING;
        return n;
    }
    if (t->tipo == TOK_BOOL) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_BOOL, t->linea, t->columna);
        n->u.boolval.val = (strcmp(t->valor, "true") == 0);
        n->dtype = DTYPE_BOOL;
        return n;
    }
    if (t->tipo == TOK_ID) {
        char *name = arena_strdup(p->arena, t->valor);
        p->pos++;
        if (accept(p, TOK_LPAREN)) {
            ASTNode *call = ast_new(p->arena, NODE_CALL, t->linea, t->columna);
            call->u.call.name = name;
            if (!accept(p, TOK_RPAREN)) {
                do {
                    ast_list_push(p->arena, &call->u.call.args, parse_expr(p));
                } while (accept(p, TOK_COMMA));
                if (!accept(p, TOK_RPAREN))
                    syn_err(p, 6, "llamada a rurana con argumentos incorrectos");
            }
            return call;
        }
        ASTNode *id = ast_new(p->arena, NODE_IDENT, t->linea, t->columna);
        id->u.ident.name = name;
        return id;
    }
    if (accept(p, TOK_LPAREN)) {
        ASTNode *e = parse_expr(p);
        if (!accept(p, TOK_RPAREN)) syn_err(p, 1, "parentesis desbalanceados");
        return e;
    }
    syn_err(p, 5, "expresion mal formada");
    return ast_new(p->arena, NODE_INT, t->linea, t->columna);
}

/* Comparaciones prefijas: tupachiy x y | aswan hatun x y | aswan pisi x y */
static ASTNode *parse_quechua_prefix_cmp(Parser *p) {
    const char *op = NULL;
    Token *t = cur(p);

    if (keyword_is(p, "tupachiy")) {
        op = "==";
        p->pos++;
    } else if (peek_kw2(p, "aswan", "hatun")) {
        op = ">";
        p->pos += 2;
        t = cur(p);
    } else if (peek_kw2(p, "aswan", "pisi")) {
        op = "<";
        p->pos += 2;
        t = cur(p);
    } else {
        return NULL;
    }

    ASTNode *left = parse_primary(p);
    ASTNode *right = parse_primary(p);
    ASTNode *n = ast_new(p->arena, NODE_BINOP, t->linea, t->columna);
    strncpy(n->u.binop.op, op, 7);
    n->u.binop.left = left;
    n->u.binop.right = right;
    return n;
}

static ASTNode *parse_unary(Parser *p) {
    ASTNode *qc = parse_quechua_prefix_cmp(p);
    if (qc) return qc;
    if (cur(p)->tipo == TOK_OP && (strcmp(cur(p)->valor, "-") == 0 || strcmp(cur(p)->valor, "!") == 0)) {
        Token *t = cur(p);
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_UNOP, t->linea, t->columna);
        strncpy(n->u.unop.op, t->valor, 7);
        n->u.unop.operand = parse_unary(p);
        return n;
    }
    return parse_primary(p);
}

static ASTNode *parse_mul(Parser *p) {
    ASTNode *l = parse_unary(p);
    while (cur(p)->tipo == TOK_OP && strchr("*/", cur(p)->valor[0])) {
        Token *t = cur(p); p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_BINOP, t->linea, t->columna);
        strncpy(n->u.binop.op, t->valor, 7);
        n->u.binop.left = l;
        n->u.binop.right = parse_unary(p);
        l = n;
    }
    return l;
}

static ASTNode *parse_add(Parser *p) {
    ASTNode *l = parse_mul(p);
    while (cur(p)->tipo == TOK_OP && strchr("+-", cur(p)->valor[0])) {
        Token *t = cur(p); p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_BINOP, t->linea, t->columna);
        strncpy(n->u.binop.op, t->valor, 7);
        n->u.binop.left = l;
        n->u.binop.right = parse_mul(p);
        l = n;
    }
    return l;
}

static ASTNode *parse_cmp(Parser *p) {
    ASTNode *l = parse_add(p);
    for (;;) {
        Token *t = cur(p);
        const char *op = NULL;
        if (t->tipo == TOK_OP) {
            if (strcmp(t->valor, "==") == 0 || strcmp(t->valor, "!=") == 0 ||
                strcmp(t->valor, "<") == 0 || strcmp(t->valor, ">") == 0 ||
                strcmp(t->valor, "<=") == 0 || strcmp(t->valor, ">=") == 0)
                op = t->valor;
            else break;
        } else break;
        if (!op) break;
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_BINOP, t->linea, t->columna);
        strncpy(n->u.binop.op, op, 7);
        n->u.binop.left = l;
        n->u.binop.right = parse_add(p);
        l = n;
    }
    return l;
}

static ASTNode *parse_and(Parser *p) {
    ASTNode *l = parse_cmp(p);
    while (cur(p)->tipo == TOK_OP && strcmp(cur(p)->valor, "&&") == 0) {
        Token *t = cur(p); p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_BINOP, t->linea, t->columna);
        strncpy(n->u.binop.op, "&&", 7);
        n->u.binop.left = l;
        n->u.binop.right = parse_cmp(p);
        l = n;
    }
    return l;
}

static ASTNode *parse_expr(Parser *p) {
    if (at_end(p)) { syn_err(p, 8, "EOF inesperado dentro de una expresion"); return NULL; }
    return parse_and(p);
}

static void parse_block(Parser *p, ASTList *out) {
    while (!at_end(p)) {
        if (is_end_kw(p)) break;
        if (is_else_kw(p) && p->in_if) break;
        ASTNode *s = parse_stmt(p);
        if (s) ast_list_push(p->arena, out, s);
    }
}

static int is_decl_kw(Parser *p) {
    return keyword_is(p, "tikraq") || keyword_is(p, "yupay") || keyword_is(p, "sayaq");
}

static ASTNode *parse_stmt(Parser *p) {
    Token *t = cur(p);
    if (t->tipo == TOK_EOF) { syn_err(p, 8, "EOF inesperado"); return NULL; }

    if (is_decl_kw(p)) {
        int typed = keyword_is(p, "yupay");
        int is_const = keyword_is(p, "sayaq");
        p->pos++;
        if (cur(p)->tipo != TOK_ID) { syn_err(p, 5, "se esperaba identificador"); return NULL; }
        /* yupay main() — inicio de programa (compatibilidad) */
        if (typed && strcmp(cur(p)->valor, "main") == 0) {
            p->pos++;
            if (accept(p, TOK_LPAREN)) accept(p, TOK_RPAREN);
            return NULL;
        }
        (void)is_const;
        char *name = arena_strdup(p->arena, cur(p)->valor);
        p->pos++;
        if (cur(p)->tipo == TOK_OP && strcmp(cur(p)->valor, "=") == 0)
            p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_ASSIGN, t->linea, t->columna);
        n->u.assign.name = name;
        n->u.assign.value = parse_expr(p);
        return n;
    }

    if (keyword_is(p, "qallariy")) {
        p->pos++;
        if (cur(p)->tipo == TOK_ID && strcmp(cur(p)->valor, "main") == 0) {
            p->pos++;
            if (accept(p, TOK_LPAREN)) accept(p, TOK_RPAREN);
        }
        return NULL;
    }

    if (keyword_is(p, "qillqay") || keyword_is(p, "rimay")) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_PRINT, t->linea, t->columna);
        n->u.print_stmt.expr = parse_expr(p);
        return n;
    }

    if (keyword_is(p, "kutiy")) {
        p->pos++;
        ASTNode *n = ast_new(p->arena, NODE_RETURN, t->linea, t->columna);
        n->u.ret_stmt.expr = parse_expr(p);
        return n;
    }

    if (keyword_is(p, "sichus")) {
        p->pos++;
        if (!accept(p, TOK_LPAREN)) syn_err(p, 1, "parentesis desbalanceados");
        ASTNode *n = ast_new(p->arena, NODE_IF, t->linea, t->columna);
        p->in_if = 1;
        n->u.if_stmt.cond = parse_expr(p);
        if (!accept(p, TOK_RPAREN)) syn_err(p, 1, "parentesis desbalanceados");
        parse_block(p, &n->u.if_stmt.then_b);
        if (is_else_kw(p)) {
            p->pos += 2;
            p->in_if = 1;
            parse_block(p, &n->u.if_stmt.else_b);
        }
        if (!expect_kw(p, "tukukun"))
            syn_err(p, 4, "falta 'tukukun' para cerrar bloque");
        p->in_if = 0;
        return n;
    }

    if (keyword_is(p, "chaykamataq")) {
        p->pos++;
        if (!accept(p, TOK_LPAREN)) syn_err(p, 1, "parentesis desbalanceados");
        ASTNode *n = ast_new(p->arena, NODE_WHILE, t->linea, t->columna);
        n->u.while_stmt.cond = parse_expr(p);
        if (!accept(p, TOK_RPAREN)) syn_err(p, 1, "parentesis desbalanceados");
        parse_block(p, &n->u.while_stmt.body);
        if (!expect_kw(p, "tukukun"))
            syn_err(p, 4, "falta 'tukukun' para cerrar bloque");
        return n;
    }

    if (keyword_is(p, "rurana") || keyword_is(p, "ruway")) {
        p->pos++;
        if (cur(p)->tipo != TOK_ID) {
            syn_err(p, 2, "se esperaba identificador tras 'rurana' o 'ruway");
            return NULL;
        }
        ASTNode *n = ast_new(p->arena, NODE_FUNCDEF, t->linea, t->columna);
        n->u.funcdef.name = arena_strdup(p->arena, cur(p)->valor);
        n->u.funcdef.ret_type = DTYPE_VOID;
        p->pos++;
        if (accept(p, TOK_LPAREN)) {
            while (cur(p)->tipo == TOK_ID || keyword_is(p, "yupay") || keyword_is(p, "simi") ||
                   peek_kw2(p, "chunka", "yupay") || peek_kw2(p, "sananpa", "qillqa")) {
                if (keyword_is(p, "yupay") || keyword_is(p, "simi")) p->pos++;
                else if (match_kw2(p, "chunka", "yupay")) { /* tipo float */ }
                else if (match_kw2(p, "sananpa", "qillqa")) { /* tipo char */ }
                else if (keyword_is(p, "qillqasqa") && keyword_at(p, 1, "simi")) p->pos += 2;
                if (cur(p)->tipo == TOK_ID) p->pos++;
                if (!accept(p, TOK_COMMA)) break;
            }
            accept(p, TOK_RPAREN);
        }
        parse_block(p, &n->u.funcdef.body);
        return n;
    }

    if (peek_kw2(p, "mana", "chayqa")) {
        if (!p->in_if) syn_err(p, 7, "'mana chayqa' sin 'sichus' previo");
        return NULL;
    }

    /* tukukun al nivel raiz cierra qallariy main() */
    if (keyword_is(p, "tukukun")) {
        p->pos++;
        return NULL;
    }

    syn_err(p, 5, "sentencia no reconocida");
    p->pos++;
    return NULL;
}

ASTNode *parse_program(Arena *a, TokenList *tokens, Diag *diag) {
    Parser p;
    p.arena = a;
    p.tok = tokens;
    p.pos = 0;
    p.diag = diag;
    p.in_if = 0;

    ASTNode *prog = ast_new(a, NODE_PROGRAM, 1, 1);
    while (!at_end(&p)) {
        if (cur(&p)->tipo == TOK_ERROR) { p.pos++; continue; }
        ASTNode *s = parse_stmt(&p);
        if (s) ast_list_push(a, &prog->u.program.stmts, s);
    }
    return prog;
}

void parser_dump_errors(Diag *diag) {
    (void)diag;
}
