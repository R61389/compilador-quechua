/*
 * ast.h — Árbol de sintaxis abstracta (tagged union de nodos).
 */
#ifndef AST_H
#define AST_H

#include "../include/common.h"

typedef enum {
    NODE_PROGRAM,
    NODE_ASSIGN,
    NODE_IF,
    NODE_WHILE,
    NODE_PRINT,
    NODE_RETURN,
    NODE_FUNCDEF,
    NODE_CALL,
    NODE_BINOP,
    NODE_UNOP,
    NODE_IDENT,
    NODE_INT,
    NODE_FLOAT,
    NODE_STRING,
    NODE_BOOL
} NodeKind;

typedef enum {
    DTYPE_UNKNOWN,
    DTYPE_INT,
    DTYPE_FLOAT,
    DTYPE_STRING,
    DTYPE_BOOL,
    DTYPE_VOID
} DataType;

typedef struct ASTNode ASTNode;

typedef struct ASTList {
    ASTNode **items;
    size_t    count;
    size_t    cap;
} ASTList;

struct ASTNode {
    NodeKind  kind;
    DataType  dtype;      /* rellenado por symtable */
    SrcLoc    loc;
    union {
        struct { ASTList stmts; } program;
        struct { char *name; ASTNode *value; } assign;
        struct { ASTNode *cond; ASTList then_b; ASTList else_b; } if_stmt;
        struct { ASTNode *cond; ASTList body; } while_stmt;
        struct { ASTNode *expr; } print_stmt;
        struct { ASTNode *expr; } ret_stmt;
        struct { char *name; ASTList params; ASTList body; DataType ret_type; } funcdef;
        struct { char *name; ASTList args; } call;
        struct { char op[8]; ASTNode *left; ASTNode *right; } binop;
        struct { char op[8]; ASTNode *operand; } unop;
        struct { char *name; } ident;
        struct { long val; } intval;
        struct { double val; } floatval;
        struct { char *val; } strval;
        struct { int val; } boolval;
    } u;
};

ASTNode *ast_new(Arena *a, NodeKind k, int line, int col);
void     ast_list_push(Arena *a, ASTList *list, ASTNode *n);
void     ast_dump(const ASTNode *root, int indent);
void     ast_free_tree(ASTNode *root);

#endif /* AST_H */
