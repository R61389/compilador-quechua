/*
 * ast.c — Construcción y volcado del AST.
 */
#include "ast.h"
#include <stdio.h>

ASTNode *ast_new(Arena *a, NodeKind k, int line, int col) {
    ASTNode *n = (ASTNode *)arena_alloc(a, sizeof(ASTNode));
    memset(n, 0, sizeof(*n));
    n->kind = k;
    n->dtype = DTYPE_UNKNOWN;
    n->loc.line = line;
    n->loc.col = col;
    return n;
}

void ast_list_push(Arena *a, ASTList *list, ASTNode *n) {
    (void)a;
    if (list->count >= list->cap) {
        size_t nc = list->cap ? list->cap * 2 : 8;
        ASTNode **ni = (ASTNode **)realloc(list->items, nc * sizeof(ASTNode *));
        if (!ni) exit(1);
        list->items = ni;
        list->cap = nc;
    }
    list->items[list->count++] = n;
}

static const char *kind_name(NodeKind k) {
    static const char *names[] = {
        "PROGRAM","ASSIGN","IF","WHILE","PRINT","RETURN","FUNCDEF","CALL",
        "BINOP","UNOP","IDENT","INT","FLOAT","STRING","BOOL"
    };
    if (k >= 0 && k <= NODE_BOOL) return names[k];
    return "?";
}

static void dump_node(const ASTNode *n, int ind) {
    if (!n) return;
    for (int i = 0; i < ind; i++) printf("  ");
    printf("%s", kind_name(n->kind));
    switch (n->kind) {
        case NODE_ASSIGN:   printf(" name=%s", n->u.assign.name); break;
        case NODE_IDENT:    printf(" %s", n->u.ident.name); break;
        case NODE_INT:      printf(" %ld", n->u.intval.val); break;
        case NODE_FLOAT:    printf(" %g", n->u.floatval.val); break;
        case NODE_STRING:   printf(" \"%s\"", n->u.strval.val); break;
        case NODE_BOOL:     printf(" %s", n->u.boolval.val ? "true" : "false"); break;
        case NODE_BINOP:    printf(" op=%s", n->u.binop.op); break;
        case NODE_FUNCDEF:  printf(" %s", n->u.funcdef.name); break;
        case NODE_CALL:     printf(" %s", n->u.call.name); break;
        default: break;
    }
    printf(" (L%d:%d)\n", n->loc.line, n->loc.col);

    switch (n->kind) {
        case NODE_PROGRAM:
            for (size_t i = 0; i < n->u.program.stmts.count; i++)
                dump_node(n->u.program.stmts.items[i], ind + 1);
            break;
        case NODE_ASSIGN:
            dump_node(n->u.assign.value, ind + 1);
            break;
        case NODE_IF:
            dump_node(n->u.if_stmt.cond, ind + 1);
            for (size_t i = 0; i < n->u.if_stmt.then_b.count; i++)
                dump_node(n->u.if_stmt.then_b.items[i], ind + 1);
            for (size_t i = 0; i < n->u.if_stmt.else_b.count; i++)
                dump_node(n->u.if_stmt.else_b.items[i], ind + 1);
            break;
        case NODE_WHILE:
            dump_node(n->u.while_stmt.cond, ind + 1);
            for (size_t i = 0; i < n->u.while_stmt.body.count; i++)
                dump_node(n->u.while_stmt.body.items[i], ind + 1);
            break;
        case NODE_PRINT:
        case NODE_RETURN:
            dump_node(n->kind == NODE_PRINT ? n->u.print_stmt.expr : n->u.ret_stmt.expr, ind + 1);
            break;
        case NODE_FUNCDEF:
            for (size_t i = 0; i < n->u.funcdef.body.count; i++)
                dump_node(n->u.funcdef.body.items[i], ind + 1);
            break;
        case NODE_BINOP:
            dump_node(n->u.binop.left, ind + 1);
            dump_node(n->u.binop.right, ind + 1);
            break;
        case NODE_UNOP:
            dump_node(n->u.unop.operand, ind + 1);
            break;
        case NODE_CALL:
            for (size_t i = 0; i < n->u.call.args.count; i++)
                dump_node(n->u.call.args.items[i], ind + 1);
            break;
        default: break;
    }
}

void ast_dump(const ASTNode *root, int indent) {
    dump_node(root, indent);
}

void ast_free_tree(ASTNode *root) {
    (void)root; /* memoria en arena */
}
