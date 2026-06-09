/*
 * main.c — Driver: orquesta lexer → parser → symtable → irgen → optimizer → codegen.
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "../include/common.h"
#include "lexer.h"
#include "parser.h"
#include "symtable.h"
#include "irgen.h"
#include "optimizer.h"
#include "codegen.h"
#include "report.h"

typedef struct Options {
    const char *input;
    const char *output;
    int verbose;
} Options;

static char *read_file(const char *path, size_t *out_len) {
    FILE *f = fopen(path, "rb");
    if (!f) {
        fprintf(stderr, "No se pudo abrir: %s\n", path);
        return NULL;
    }
    fseek(f, 0, SEEK_END);
    long sz = ftell(f);
    fseek(f, 0, SEEK_SET);
    if (sz < 0) { fclose(f); return NULL; }
    char *buf = (char *)malloc((size_t)sz + 1);
    if (!buf) { fclose(f); return NULL; }
    size_t n = fread(buf, 1, (size_t)sz, f);
    fclose(f);
    buf[n] = '\0';
    if (out_len) *out_len = n;
    return buf;
}

static int parse_args(int argc, char **argv, Options *opt) {
    memset(opt, 0, sizeof(*opt));
    opt->input = "quechua.txt";
    opt->output = "output.asm";

    for (int i = 1; i < argc; i++) {
        if (strcmp(argv[i], "-v") == 0 || strcmp(argv[i], "--verbose") == 0)
            opt->verbose = 1;
        else if (strcmp(argv[i], "-o") == 0 && i + 1 < argc)
            opt->output = argv[++i];
        else if (argv[i][0] != '-')
            opt->input = argv[i];
    }
    return 0;
}

int main(int argc, char **argv) {
    Options opt;
    Arena arena;
    Diag diag;
    parse_args(argc, argv, &opt);
    arena_init(&arena);
    diag_reset(&diag);

    size_t src_len;
    char *source = read_file(opt.input, &src_len);
    if (!source) return 1;
    (void)src_len;

    TokenList tokens = lexer_tokenize(source, opt.input, &diag);
    ASTNode *ast = parse_program(&arena, &tokens, &diag);

    /* Tabla lexica una sola vez, con HAY_ERROR por linea (lex + syn) */
    if (opt.verbose)
        report_lexer_phase(&tokens, &diag);
    if (opt.verbose)
        report_parser_phase(&diag);

    if (diag.lex_errors > 0 || diag.syn_errors > 0) {
        free(source);
        token_list_free(&tokens);
        arena_free(&arena);
        return 1;
    }

    SymTable st = {0};
    st.verbose = 0;
    if (opt.verbose)
        report_ast_phase(ast);

    if (symtable_analyze(ast, &st, &diag) != 0 && diag.sem_errors > 0) {
        if (opt.verbose)
            report_semantic_phase(&st, &diag);
        free(source);
        token_list_free(&tokens);
        free(st.items);
        arena_free(&arena);
        return 1;
    }

    if (opt.verbose)
        report_semantic_phase(&st, &diag);

    IRList ir = irgen_generate(ast, &arena);
    IRList ir_before = opt.verbose ? ir_clone(&ir) : (IRList){0};
    optimize_ir(&ir);

    if (opt.verbose)
        report_backend_phase(opt.verbose ? &ir_before : NULL, &ir, opt.output);

    if (codegen_emit(&ir, opt.output) != 0) {
        fprintf(stderr, "Error escribiendo %s\n", opt.output);
        free(source);
        token_list_free(&tokens);
        ir_list_free(&ir_before);
        ir_list_free(&ir);
        free(st.items);
        arena_free(&arena);
        return 1;
    }

    free(source);
    token_list_free(&tokens);
    ir_list_free(&ir_before);
    ir_list_free(&ir);
    free(st.items);
    arena_free(&arena);
    return 0;
}
