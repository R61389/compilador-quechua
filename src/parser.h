#ifndef PARSER_H
#define PARSER_H

#include "lexer.h"
#include "ast.h"

ASTNode *parse_program(Arena *a, TokenList *tokens, Diag *diag);
void     parser_dump_errors(Diag *diag);

#endif /* PARSER_H */
