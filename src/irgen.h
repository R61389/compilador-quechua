#ifndef IRGEN_H
#define IRGEN_H

#include "ast.h"

typedef enum {
    IR_ASSIGN, IR_ADD, IR_SUB, IR_MUL, IR_DIV,
    IR_EQ, IR_NEQ, IR_LT, IR_GT, IR_LEQ, IR_GEQ,
    IR_JUMP, IR_JUMPIF, IR_LABEL, IR_CALL,
    IR_PARAM, IR_RETURN, IR_PRINT
} IROpcode;

typedef struct IROp {
    IROpcode op;
    char     dest[MAX_TAC];
    char     src1[MAX_TAC];
    char     src2[MAX_TAC];
    int      line;
} IROp;

typedef struct IRList {
    IROp   *items;
    size_t  count;
    size_t  cap;
    int     temp_n;
    int     label_n;
} IRList;

IRList irgen_generate(ASTNode *root, Arena *a);
IRList ir_clone(const IRList *src);
void   ir_dump(const IRList *ir);
void   ir_list_free(IRList *ir);

#endif /* IRGEN_H */
