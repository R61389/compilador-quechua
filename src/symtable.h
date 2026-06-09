#ifndef SYMTABLE_H
#define SYMTABLE_H

#include "ast.h"

typedef enum { SYM_VAR, SYM_FUNC, SYM_PARAM } SymKind;

typedef struct Symbol {
    char     nombre[MAX_NAME];
    SymKind  tipo;
    DataType tipo_dato;
    int      scope;
    int      linea;
} Symbol;

typedef struct SymTable {
    Symbol *items;
    size_t  count;
    size_t  cap;
    int     scope_depth;
    int     verbose;
} SymTable;

int symtable_analyze(ASTNode *root, SymTable *st, Diag *diag);
void symtable_dump(const SymTable *st);

#endif /* SYMTABLE_H */
