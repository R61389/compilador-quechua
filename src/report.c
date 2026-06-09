/*
 * report.c — Salida formateada por fases del compilador (modo -v).
 */
#include "report.h"
#include <stdio.h>
#include <string.h>

void report_phase(const char *title) {
    printf("\n--- %s ---\n", title);
    fflush(stdout);
}

static const char *token_type_name(TokenType t) {
    static const char *names[] = {
        "KEYWORD", "ID", "NUMBER", "FLOAT", "STRING", "BOOL",
        "LPAREN", "RPAREN", "OP", "COMMA", "SEMICOLON", "EOF", "ERROR"
    };
    if (t >= 0 && t <= TOK_ERROR) return names[t];
    return "?";
}

static int line_has_error(const Diag *d, int line) {
    if (line <= 0 || line >= DIAG_MAX_LINE) return 0;
    return d->err_line[line] ? 1 : 0;
}

void report_lexer_phase(const TokenList *tl, const Diag *d) {
    int i;
    report_phase("FASE 1: ANALISIS LEXICO");
    if (d->lex_errors == 0)
        printf("[OK] Lexico sin errores.\n");
    else
        printf("[FALLO] Lexico con %d error(es).\n", d->lex_errors);

    printf("+-------------------+-------------------+--------+----------+\n");
    printf("| TIPO              | VALOR             | LINEA  | HAY_ERROR|\n");
    printf("+-------------------+-------------------+--------+----------+\n");

    for (size_t k = 0; k < tl->count; k++) {
        const Token *t = &tl->items[k];
        int hay_err;
        char valor[MAX_LEXEME + 2];
        if (t->tipo == TOK_EOF) continue;
        hay_err = line_has_error(d, t->linea);
        if (t->tipo == TOK_STRING)
            snprintf(valor, sizeof(valor), "\"%s\"", t->valor);
        else
            snprintf(valor, sizeof(valor), "%s", t->valor);
        printf("| %-17s | %-17s | %-6d | %-8d |\n",
               token_type_name(t->tipo), valor, t->linea, hay_err);
    }
    printf("+-------------------+-------------------+--------+----------+\n");
    printf("  HAY_ERROR=1 si esa LINEA tuvo error lexico o sintactico; 0 si no.\n");

    if (d->lex_msg_n > 0) {
        printf("\nDetalle errores lexicos:\n");
        for (i = 0; i < d->lex_msg_n; i++) {
            const DiagMsg *m = &d->lex_msgs[i];
            printf("  [ERR-LEX-%02d] linea %d, columna %d: %s\n",
                   m->code, m->line, m->col, m->text);
        }
    }
    fflush(stdout);
}

void report_parser_phase(const Diag *d) {
    int i;
    report_phase("FASE 2: VALIDACION GRAMATICAL");
    if (d->syn_errors == 0) {
        printf("[OK] La oracion es gramaticalmente correcta (nombres y estructura).\n");
        fflush(stdout);
        return;
    }
    printf("[FALLO] Errores sintacticos: %d\n", d->syn_errors);
    for (i = 0; i < d->syn_msg_n; i++) {
        const DiagMsg *m = &d->syn_msgs[i];
        printf("  [ERR-SYN-%03d] linea %d, columna %d: %s\n",
               m->code, m->line, m->col, m->text);
    }
    fflush(stdout);
}

void report_ast_phase(ASTNode *ast) {
    report_phase("FASE 3: AST + ANALISIS SEMANTICO");
    printf("[AST] Arbol de sintaxis abstracta:\n");
    ast_dump(ast, 0);
    fflush(stdout);
}

void report_semantic_phase(const SymTable *st, const Diag *d) {
    int vars = 0, funcs = 0, params = 0;
    for (size_t i = 0; i < st->count; i++) {
        if (st->items[i].tipo == SYM_VAR) vars++;
        else if (st->items[i].tipo == SYM_FUNC) funcs++;
        else if (st->items[i].tipo == SYM_PARAM) params++;
    }

    printf("\n[SEM] Verificaciones sobre AST:\n");
    if (d->sem_errors == 0) {
        printf("  - Resolucion de nombres/ambitos: OK\n");
        printf("  - Coherencia de llamadas basica: OK\n");
        printf("  - Tipado basico de atomos/operadores: OK\n");
    } else {
        printf("  - Se encontraron %d error(es) semanticos.\n", d->sem_errors);
    }
    printf("  - Simbolos registrados: %lu (func=%d, var=%d, param=%d)\n",
           (unsigned long)st->count, funcs, vars, params);

    if (st->count == 0) {
        fflush(stdout);
        return;
    }
    printf("\n[TABLA DE SIMBOLOS]\n");
    printf("+----------+--------+-----------+-------+-------+\n");
    printf("| NOMBRE   | CLASE  | TIPO_DATO | SCOPE | LINEA |\n");
    printf("+----------+--------+-----------+-------+-------+\n");
    {
        static const char *sk[] = {"VAR", "FUNC", "PARAM"};
        static const char *dt[] = {"UNK", "INT", "FLOAT", "STR", "BOOL", "VOID"};
        size_t i;
        for (i = 0; i < st->count; i++) {
            const Symbol *s = &st->items[i];
            const char *cls = (s->tipo >= 0 && s->tipo <= SYM_PARAM) ? sk[s->tipo] : "?";
            const char *td = (s->tipo_dato >= 0 && s->tipo_dato <= DTYPE_VOID) ? dt[s->tipo_dato] : "?";
            printf("| %-8s | %-6s | %-9s | %-5d | %-5d |\n",
                   s->nombre, cls, td, s->scope, s->linea);
        }
    }
    printf("+----------+--------+-----------+-------+-------+\n");
    fflush(stdout);
}

void report_backend_phase(const IRList *ir_before, const IRList *ir_after, const char *asm_path) {
    report_phase("FASE 4: BACKEND (IR / OPT / CODEGEN)");
    printf("[TAC] Antes de optimizar:\n");
    if (!ir_before || ir_before->count == 0)
        printf("  (vacio)\n");
    else
        ir_dump(ir_before);
    printf("[TAC] Despues de optimizar:\n");
    if (!ir_after || ir_after->count == 0)
        printf("  (vacio)\n");
    else
        ir_dump(ir_after);
    printf("[CODEGEN] Ensamblador MASM32 -> %s\n", asm_path);
    fflush(stdout);
}
