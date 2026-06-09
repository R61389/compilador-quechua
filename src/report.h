#ifndef REPORT_H
#define REPORT_H

#include "../include/common.h"
#include "lexer.h"
#include "ast.h"
#include "symtable.h"
#include "irgen.h"

void report_phase(const char *title);
void report_lexer_phase(const TokenList *tl, const Diag *d);
void report_parser_phase(const Diag *d);
void report_semantic_phase(const SymTable *st, const Diag *d);
void report_ast_phase(ASTNode *ast);
void report_backend_phase(const IRList *ir_before, const IRList *ir_after, const char *asm_path);

#endif /* REPORT_H */
