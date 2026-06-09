/*
 * irgen.c — Genera Three-Address Code (TAC) desde el AST.
 */
#include "irgen.h"
#include <stdio.h>
#include <string.h>

static void ir_push(IRList *ir, IROp op) {
    if (ir->count >= ir->cap) {
        size_t nc = ir->cap ? ir->cap * 2 : 64;
        IROp *ni = (IROp *)realloc(ir->items, nc * sizeof(IROp));
        if (!ni) exit(1);
        ir->items = ni;
        ir->cap = nc;
    }
    ir->items[ir->count++] = op;
}

static char *new_temp(IRList *ir, char *buf) {
    sprintf(buf, "t%d", ir->temp_n++);
    return buf;
}

static char *new_label(IRList *ir, char *buf) {
    sprintf(buf, "L%d", ir->label_n++);
    return buf;
}

static IROpcode binop_to_ir(const char *op) {
    if (strcmp(op, "+") == 0) return IR_ADD;
    if (strcmp(op, "-") == 0) return IR_SUB;
    if (strcmp(op, "*") == 0) return IR_MUL;
    if (strcmp(op, "/") == 0) return IR_DIV;
    if (strcmp(op, "==") == 0) return IR_EQ;
    if (strcmp(op, "!=") == 0) return IR_NEQ;
    if (strcmp(op, "<") == 0) return IR_LT;
    if (strcmp(op, ">") == 0) return IR_GT;
    if (strcmp(op, "<=") == 0) return IR_LEQ;
    if (strcmp(op, ">=") == 0) return IR_GEQ;
    return IR_ADD;
}

static char *gen_expr(ASTNode *n, IRList *ir, char *out);

static char *gen_expr(ASTNode *n, IRList *ir, char *out) {
    if (!n) { out[0] = 0; return out; }
    switch (n->kind) {
        case NODE_INT:
            sprintf(out, "%ld", n->u.intval.val);
            return out;
        case NODE_FLOAT:
            sprintf(out, "%g", n->u.floatval.val);
            return out;
        case NODE_STRING:
            strncpy(out, n->u.strval.val, MAX_TAC - 1);
            return out;
        case NODE_BOOL:
            strcpy(out, n->u.boolval.val ? "1" : "0");
            return out;
        case NODE_IDENT:
            strncpy(out, n->u.ident.name, MAX_TAC - 1);
            return out;
        case NODE_BINOP: {
            char a[MAX_TAC], b[MAX_TAC], t[MAX_TAC];
            gen_expr(n->u.binop.left, ir, a);
            gen_expr(n->u.binop.right, ir, b);
            new_temp(ir, t);
            IROp op;
            memset(&op, 0, sizeof(op));
            op.op = binop_to_ir(n->u.binop.op);
            op.line = n->loc.line;
            strncpy(op.dest, t, MAX_TAC - 1);
            strncpy(op.src1, a, MAX_TAC - 1);
            strncpy(op.src2, b, MAX_TAC - 1);
            ir_push(ir, op);
            strncpy(out, t, MAX_TAC - 1);
            return out;
        }
        default:
            strcpy(out, "0");
            return out;
    }
}

static void gen_stmt(ASTNode *n, IRList *ir);

static void gen_stmt(ASTNode *n, IRList *ir) {
    if (!n) return;
    char buf[MAX_TAC], l0[MAX_TAC], l1[MAX_TAC], cond[MAX_TAC];

    switch (n->kind) {
        case NODE_ASSIGN: {
            gen_expr(n->u.assign.value, ir, buf);
            IROp op = {IR_ASSIGN, "", "", "", n->loc.line};
            strncpy(op.dest, n->u.assign.name, MAX_TAC - 1);
            strncpy(op.src1, buf, MAX_TAC - 1);
            ir_push(ir, op);
            break;
        }
        case NODE_PRINT: {
            gen_expr(n->u.print_stmt.expr, ir, buf);
            IROp op = {IR_PRINT, "", "", "", n->loc.line};
            strncpy(op.src1, buf, MAX_TAC - 1);
            ir_push(ir, op);
            break;
        }
        case NODE_IF: {
            new_label(ir, l0);
            new_label(ir, l1);
            gen_expr(n->u.if_stmt.cond, ir, cond);
            IROp jf = {IR_JUMPIF, "", "", "", n->loc.line};
            strncpy(jf.src1, cond, MAX_TAC - 1);
            strncpy(jf.src2, l0, MAX_TAC - 1);
            ir_push(ir, jf);
            IROp j = {IR_JUMP, "", "", "", n->loc.line};
            strncpy(j.src1, l1, MAX_TAC - 1);
            ir_push(ir, j);
            IROp lab0 = {IR_LABEL, "", "", "", n->loc.line};
            strncpy(lab0.dest, l0, MAX_TAC - 1);
            ir_push(ir, lab0);
            for (size_t i = 0; i < n->u.if_stmt.then_b.count; i++)
                gen_stmt(n->u.if_stmt.then_b.items[i], ir);
            IROp j2 = {IR_JUMP, "", "", "", n->loc.line};
            strncpy(j2.src1, l1, MAX_TAC - 1);
            ir_push(ir, j2);
            IROp lab1 = {IR_LABEL, "", "", "", n->loc.line};
            strncpy(lab1.dest, l1, MAX_TAC - 1);
            ir_push(ir, lab1);
            for (size_t i = 0; i < n->u.if_stmt.else_b.count; i++)
                gen_stmt(n->u.if_stmt.else_b.items[i], ir);
            break;
        }
        case NODE_WHILE: {
            new_label(ir, l0);
            new_label(ir, l1);
            IROp start = {IR_LABEL, "", "", "", n->loc.line};
            strncpy(start.dest, l0, MAX_TAC - 1);
            ir_push(ir, start);
            gen_expr(n->u.while_stmt.cond, ir, cond);
            IROp jf = {IR_JUMPIF, "", "", "", n->loc.line};
            strncpy(jf.src1, cond, MAX_TAC - 1);
            strncpy(jf.src2, l1, MAX_TAC - 1);
            ir_push(ir, jf);
            for (size_t i = 0; i < n->u.while_stmt.body.count; i++)
                gen_stmt(n->u.while_stmt.body.items[i], ir);
            IROp jb = {IR_JUMP, "", "", "", n->loc.line};
            strncpy(jb.src1, l0, MAX_TAC - 1);
            ir_push(ir, jb);
            IROp end = {IR_LABEL, "", "", "", n->loc.line};
            strncpy(end.dest, l1, MAX_TAC - 1);
            ir_push(ir, end);
            break;
        }
        default: break;
    }
}

IRList irgen_generate(ASTNode *root, Arena *a) {
    (void)a;
    IRList ir = {0};
    if (!root || root->kind != NODE_PROGRAM) return ir;
    for (size_t i = 0; i < root->u.program.stmts.count; i++)
        gen_stmt(root->u.program.stmts.items[i], &ir);
    return ir;
}

static const char *ir_op_name(IROpcode op) {
    static const char *n[] = {
        "ASSIGN","ADD","SUB","MUL","DIV","EQ","NEQ","LT","GT","LEQ","GEQ",
        "JUMP","JUMPIF","LABEL","CALL","PARAM","RETURN","PRINT"
    };
    if (op >= 0 && op <= IR_PRINT) return n[op];
    return "?";
}

IRList ir_clone(const IRList *src) {
    IRList c = {0};
    if (!src || src->count == 0) return c;
    c.items = (IROp *)malloc(src->count * sizeof(IROp));
    if (!c.items) return c;
    memcpy(c.items, src->items, src->count * sizeof(IROp));
    c.count = src->count;
    c.cap = src->count;
    c.temp_n = src->temp_n;
    c.label_n = src->label_n;
    return c;
}

void ir_dump(const IRList *ir) {
    for (size_t i = 0; i < ir->count; i++) {
        const IROp *o = &ir->items[i];
        printf("%4lu: %-8s ", (unsigned long)i, ir_op_name(o->op));
        if (o->op == IR_LABEL)
            printf("%s:", o->dest);
        else if (o->op == IR_JUMP || o->op == IR_JUMPIF)
            printf("%s %s %s", o->dest, o->src1, o->src2);
        else if (o->src2[0])
            printf("%s = %s %s %s", o->dest, o->src1, ir_op_name(o->op), o->src2);
        else if (o->op == IR_ASSIGN)
            printf("%s = %s", o->dest, o->src1);
        else if (o->op == IR_PRINT)
            printf("print %s", o->src1);
        else
            printf("%s = %s", o->dest, o->src1);
        printf("\n");
    }
}

void ir_list_free(IRList *ir) {
    free(ir->items);
    ir->items = NULL;
    ir->count = ir->cap = 0;
}
