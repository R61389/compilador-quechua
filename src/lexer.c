/*
 * lexer.c — Analizador lexico (quechua boliviano).
 * Palabras clave segun vocabulario del compilador; operadores estilo C; comentarios //.
 */
#include "lexer.h"
#include <ctype.h>
#include <stdio.h>

/* Palabras reservadas de una sola pieza (frases de 2 palabras las une el parser). */
static const struct { const char *kw; const char *tag; } KEYWORDS[] = {
    {"aswan",       "aswan"},
    {"chaykamataq", "chaykamataq"},
    {"chayqa",      "chayqa"},
    {"chiqap",      "chiqap"},
    {"chunka",      "chunka"},
    {"chusaq",      "chusaq"},
    {"hatun",       "hatun"},
    {"imapas",      "imapas"},
    {"kutiy",       "kutiy"},
    {"mana",        "mana"},
    {"pisi",        "pisi"},
    {"qallariy",    "qallariy"},
    {"qillqa",      "qillqa"},
    {"qillqay",     "qillqay"},
    {"qillqasqa",   "qillqasqa"},
    {"rimay",       "rimay"},
    {"rikch'aq",    "rikch'aq"},
    {"rurana",      "rurana"},
    {"ruway",       "ruway"},
    {"sananpa",     "sananpa"},
    {"sayaq",       "sayaq"},
    {"sichus",      "sichus"},
    {"simi",        "simi"},
    {"sinri",       "sinri"},
    {"tikraq",      "tikraq"},
    {"tukukun",     "tukukun"},
    {"tupachiy",    "tupachiy"},
    {"yupay",       "yupay"},
    {NULL, NULL}
};

static void tl_push(TokenList *tl, Token t) {
    if (tl->count >= tl->cap) {
        size_t nc = tl->cap ? tl->cap * 2 : 64;
        Token *ni = (Token *)realloc(tl->items, nc * sizeof(Token));
        if (!ni) { fprintf(stderr, "lexer: sin memoria\n"); exit(1); }
        tl->items = ni;
        tl->cap = nc;
    }
    tl->items[tl->count++] = t;
}

static Token make_tok(TokenType ty, const char *val, int line, int col, int err) {
    Token t;
    t.tipo = ty;
    strncpy(t.valor, val ? val : "", MAX_LEXEME - 1);
    t.valor[MAX_LEXEME - 1] = '\0';
    t.linea = line;
    t.columna = col;
    t.cod_err = err;
    return t;
}

static int is_id_start(char c) {
    return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '\'';
}
static int is_id_char(char c) {
    return is_id_start(c) || (c >= '0' && c <= '9');
}

static TokenType classify_keyword(const char *s, Token *out) {
    for (int i = 0; KEYWORDS[i].kw; i++) {
        if (strcmp(s, KEYWORDS[i].kw) == 0) {
            if (strcmp(s, "chiqap") == 0) {
                out->tipo = TOK_BOOL;
                strcpy(out->valor, "true");
                return TOK_BOOL;
            }
            out->tipo = TOK_KEYWORD;
            strncpy(out->valor, KEYWORDS[i].tag, MAX_LEXEME - 1);
            return TOK_KEYWORD;
        }
    }
    out->tipo = TOK_ID;
    return TOK_ID;
}

static void report_lex(Diag *d, int code, int line, int col, const char *msg) {
    d->lex_errors++;
    diag_mark_err_line(d, line);
    if (d->lex_msg_n < MAX_DIAG_MSGS) {
        DiagMsg *m = &d->lex_msgs[d->lex_msg_n++];
        m->code = code;
        m->line = line;
        m->col = col;
        strncpy(m->text, msg, ERR_BUF - 1);
        m->text[ERR_BUF - 1] = '\0';
    }
}

TokenList lexer_tokenize(const char *source, const char *filename, Diag *diag) {
    (void)filename;
    TokenList tl = {0};
    int line = 1, col = 1;
    size_t i = 0;
    size_t len = strlen(source);

    while (i < len) {
        char c = source[i];
        int start_col = col;

        if (c == ' ' || c == '\t' || c == '\r') {
            i++; col++; continue;
        }
        if (c == '\n') {
            i++; line++; col = 1; continue;
        }
        if (c == '/' && i + 1 < len && source[i + 1] == '/') {
            while (i < len && source[i] != '\n') { i++; col++; }
            continue;
        }

        if (c == '"') {
            char buf[MAX_LEXEME];
            int bi = 0;
            i++; col++;
            while (i < len && source[i] != '"' && source[i] != '\n') {
                if (bi >= MAX_LEXEME - 1) {
                    report_lex(diag, LERR_05, line, start_col, "token demasiado largo");
                    tl_push(&tl, make_tok(TOK_ERROR, "", line, start_col, LERR_05));
                    break;
                }
                buf[bi++] = source[i++];
                col++;
            }
            if (i >= len || source[i] != '"') {
                report_lex(diag, LERR_02, line, start_col, "string no cerrada");
                tl_push(&tl, make_tok(TOK_ERROR, buf, line, start_col, LERR_02));
                if (i < len && source[i] == '\n') { i++; line++; col = 1; }
                continue;
            }
            buf[bi] = '\0';
            i++; col++;
            tl_push(&tl, make_tok(TOK_STRING, buf, line, start_col, LERR_NONE));
            continue;
        }

        if (isdigit((unsigned char)c) || (c == '.' && i + 1 < len && isdigit((unsigned char)source[i+1]))) {
            char buf[MAX_LEXEME];
            int bi = 0;
            int is_float = 0;
            int had_dot = 0;
            if (c == '.' && !isdigit((unsigned char)source[i+1])) {
                report_lex(diag, LERR_03, line, col, "numero mal formado");
                tl_push(&tl, make_tok(TOK_ERROR, ".", line, col, LERR_03));
                i++; col++; continue;
            }
            while (i < len && (isdigit((unsigned char)source[i]) ||
                   source[i] == '.' || source[i] == 'e' || source[i] == 'E' ||
                   source[i] == '+' || source[i] == '-')) {
                if (source[i] == '.') {
                    if (had_dot) { is_float = -1; break; }
                    had_dot = 1; is_float = 1;
                }
                if (bi >= MAX_LEXEME - 1) break;
                buf[bi++] = source[i++];
                col++;
            }
            buf[bi] = '\0';
            if (is_float < 0 || (bi > 0 && !isdigit((unsigned char)buf[bi-1]) && buf[bi-1] != '.')) {
                int bad = 0;
                for (int j = 0; buf[j]; j++)
                    if (!isdigit((unsigned char)buf[j]) && buf[j] != '.' && buf[j] != 'e' && buf[j] != 'E') bad = 1;
                if (bad || strstr(buf, "..")) {
                    report_lex(diag, LERR_03, line, start_col, "numero mal formado");
                    tl_push(&tl, make_tok(TOK_ERROR, buf, line, start_col, LERR_03));
                    continue;
                }
            }
            for (int j = 0; buf[j]; j++)
                if ((buf[j] >= 'a' && buf[j] <= 'z') || (buf[j] >= 'A' && buf[j] <= 'Z')) {
                    report_lex(diag, LERR_03, line, start_col, "numero mal formado");
                    tl_push(&tl, make_tok(TOK_ERROR, buf, line, start_col, LERR_03));
                    goto num_done;
                }
            tl_push(&tl, make_tok(is_float ? TOK_FLOAT : TOK_NUMBER, buf, line, start_col, LERR_NONE));
        num_done:
            continue;
        }

        if (is_id_start(c)) {
            char buf[MAX_LEXEME];
            int bi = 0;
            while (i < len && is_id_char(source[i])) {
                if (bi >= MAX_LEXEME - 1) {
                    report_lex(diag, LERR_05, line, start_col, "token demasiado largo");
                    tl_push(&tl, make_tok(TOK_ERROR, buf, line, start_col, LERR_05));
                    while (i < len && is_id_char(source[i])) { i++; col++; }
                    goto next_tok;
                }
                buf[bi++] = source[i++];
                col++;
            }
            buf[bi] = '\0';
            Token t = make_tok(TOK_ID, buf, line, start_col, LERR_NONE);
            if (strcmp(buf, "chiqap") == 0) {
                t.tipo = TOK_BOOL;
                strcpy(t.valor, "true");
            } else {
                classify_keyword(buf, &t);
            }
            tl_push(&tl, t);
            continue;
        }

        if (c == '(') { tl_push(&tl, make_tok(TOK_LPAREN, "(", line, col, 0)); i++; col++; continue; }
        if (c == ')') { tl_push(&tl, make_tok(TOK_RPAREN, ")", line, col, 0)); i++; col++; continue; }
        if (c == ',') { tl_push(&tl, make_tok(TOK_COMMA, ",", line, col, 0)); i++; col++; continue; }
        if (c == ';') { tl_push(&tl, make_tok(TOK_SEMICOLON, ";", line, col, 0)); i++; col++; continue; }

        {
            char op[4] = {c, 0, 0, 0};
            if (i + 1 < len) {
                char c2 = source[i + 1];
                if ((c == '=' && c2 == '=') || (c == '!' && c2 == '=') ||
                    (c == '<' && c2 == '=') || (c == '>' && c2 == '=') ||
                    (c == '&' && c2 == '&') || (c == '|' && c2 == '|')) {
                    op[1] = c2; op[2] = 0;
                    i += 2; col += 2;
                    tl_push(&tl, make_tok(TOK_OP, op, line, start_col, 0));
                    continue;
                }
            }
            if (strchr("+-*/=<>!", c)) {
                i++; col++;
                tl_push(&tl, make_tok(TOK_OP, op, line, start_col, 0));
                continue;
            }
        }

        if ((unsigned char)c > 127) {
            report_lex(diag, LERR_01, line, col, "caracter invalido o no-ASCII");
            tl_push(&tl, make_tok(TOK_ERROR, "", line, col, LERR_01));
        } else {
            char tmp[2] = {c, 0};
            report_lex(diag, LERR_06, line, col, "secuencia de caracteres ilegal");
            tl_push(&tl, make_tok(TOK_ERROR, tmp, line, col, LERR_06));
        }
        i++; col++;
    next_tok:
        ;
    }

    tl_push(&tl, make_tok(TOK_EOF, "", line, col, 0));
    return tl;
}

void lexer_dump_tokens(const TokenList *tl) {
    printf("TIPO     | VALOR                          | LINEA | COLUMNA | COD_ERR\n");
    printf("---------+--------------------------------+-------+---------+--------\n");
    const char *names[] = {
        "KEYWORD","ID","NUMBER","FLOAT","STRING","BOOL",
        "LPAREN","RPAREN","OP","COMMA","SEMICOLON","EOF","ERROR"
    };
    for (size_t k = 0; k < tl->count; k++) {
        const Token *t = &tl->items[k];
        const char *tn = (t->tipo >= 0 && t->tipo <= TOK_ERROR) ? names[t->tipo] : "?";
        printf("%-8s | %-30s | %5d | %7d | %d\n",
               tn, t->valor, t->linea, t->columna, t->cod_err);
    }
}

void token_list_free(TokenList *tl) {
    free(tl->items);
    tl->items = NULL;
    tl->count = tl->cap = 0;
}
