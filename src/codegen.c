/*
 * codegen.c — TAC optimizado → MASM32 (x86 32-bit, flat model, stdcall).
 * Salida compatible con ml.exe / link.exe (MASM32 SDK en Windows).
 */
#include "codegen.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <stdarg.h>
#include <ctype.h>

static FILE *out;
static int stack_off;

static void emit(const char *fmt, ...) {
    va_list ap;
    va_start(ap, fmt);
    vfprintf(out, fmt, ap);
    va_end(ap);
}

static int is_temp(const char *s) {
    return s[0] == 't' && isdigit((unsigned char)s[1]);
}

static void load_to_eax(const char *src) {
    if (!src[0]) {
        emit("    xor eax, eax\n");
        return;
    }
    if (is_temp(src)) {
        int idx = atoi(src + 1);
        emit("    mov eax, [ebp-%d]\n", 4 * (idx + 1));
        return;
    }
    if (isdigit((unsigned char)src[0]) ||
        (src[0] == '-' && isdigit((unsigned char)src[1]))) {
        emit("    mov eax, %s\n", src);
        return;
    }
    emit("    mov eax, var_%s\n", src);
}

static void store_from_eax(const char *dest) {
    if (is_temp(dest)) {
        int idx = atoi(dest + 1);
        emit("    mov [ebp-%d], eax\n", 4 * (idx + 1));
    } else {
        emit("    mov var_%s, eax\n", dest);
    }
}

static int looks_like_string(const char *s) {
    if (!s[0] || is_temp(s) || s[0] == 'L') return 0;
    if (isdigit((unsigned char)s[0]) ||
        (s[0] == '-' && isdigit((unsigned char)s[1])))
        return 0;
    if (strchr(s, ' ')) return 1;
    if (s[0] >= 'A' && s[0] <= 'Z') return 1;
    return 0;
}

static void emit_cmp_set(IROpcode op) {
    switch (op) {
        case IR_EQ:  emit("    cmp eax, ebx\n    sete al\n"); break;
        case IR_NEQ: emit("    cmp eax, ebx\n    setne al\n"); break;
        case IR_LT:  emit("    cmp eax, ebx\n    setl al\n"); break;
        case IR_GT:  emit("    cmp eax, ebx\n    setg al\n"); break;
        case IR_LEQ: emit("    cmp eax, ebx\n    setle al\n"); break;
        case IR_GEQ: emit("    cmp eax, ebx\n    setge al\n"); break;
        default: break;
    }
    emit("    movzx eax, al\n");
}

int codegen_emit(const IRList *ir, const char *out_path) {
    out = fopen(out_path, "w");
    if (!out) return 1;
    stack_off = 128;

    emit("; Generado por quechuac — MASM32 (32-bit)\n");
    emit(".386\n");
    emit(".model flat, stdcall\n");
    emit("option casemap:none\n\n");
    emit("printf PROTO C :ptr byte, :vararg\n");
    emit("ExitProcess PROTO stdcall :DWORD\n\n");

    emit(".data\n");
    emit("fmt_int db \"%%d\", 0Dh, 0Ah, 0\n");
    emit("fmt_str db \"%%s\", 0Dh, 0Ah, 0\n");

    int str_counter = 0;
    for (size_t i = 0; i < ir->count; i++) {
        if (ir->items[i].op == IR_PRINT && looks_like_string(ir->items[i].src1))
            emit("str_%d db \"%s\", 0\n", str_counter++, ir->items[i].src1);
    }

    {
        static char seen[64][MAX_TAC];
        static int nseen;
        for (size_t i = 0; i < ir->count; i++) {
            const char *names[3] = {
                ir->items[i].dest, ir->items[i].src1, ir->items[i].src2
            };
            for (int j = 0; j < 3; j++) {
                const char *name = names[j];
                if (!name[0] || name[0] == 'L' || is_temp(name)) continue;
                if (isdigit((unsigned char)name[0])) continue;
                if (looks_like_string(name)) continue;
                int dup = 0;
                for (int k = 0; k < nseen; k++)
                    if (strcmp(seen[k], name) == 0) { dup = 1; break; }
                if (!dup && nseen < 64) {
                    strncpy(seen[nseen++], name, MAX_TAC - 1);
                    emit("var_%s dd 0\n", name);
                }
            }
        }
    }

    emit("\n.code\n");
    emit("main PROC\n");
    emit("    push ebp\n");
    emit("    mov ebp, esp\n");
    emit("    sub esp, %d\n", stack_off);

    str_counter = 0;
    for (size_t i = 0; i < ir->count; i++) {
        const IROp *o = &ir->items[i];
        switch (o->op) {
            case IR_LABEL:
                emit("%s:\n", o->dest);
                break;
            case IR_ASSIGN:
                load_to_eax(o->src1);
                store_from_eax(o->dest);
                break;
            case IR_ADD: case IR_SUB: case IR_MUL: case IR_DIV:
            case IR_EQ: case IR_NEQ: case IR_LT: case IR_GT: case IR_LEQ: case IR_GEQ: {
                load_to_eax(o->src1);
                emit("    push eax\n");
                load_to_eax(o->src2);
                emit("    mov ebx, eax\n");
                emit("    pop eax\n");
                if (o->op == IR_DIV) {
                    emit("    cdq\n    idiv ebx\n");
                    store_from_eax(o->dest);
                    break;
                }
                if (o->op >= IR_EQ && o->op <= IR_GEQ) {
                    emit_cmp_set(o->op);
                    store_from_eax(o->dest);
                    break;
                }
                if (o->op == IR_MUL)
                    emit("    imul eax, ebx\n");
                else if (o->op == IR_SUB)
                    emit("    sub eax, ebx\n");
                else
                    emit("    add eax, ebx\n");
                store_from_eax(o->dest);
                break;
            }
            case IR_JUMP:
                emit("    jmp %s\n", o->src1);
                break;
            case IR_JUMPIF:
                load_to_eax(o->src1);
                emit("    test eax, eax\n");
                emit("    jnz %s\n", o->src2);
                break;
            case IR_PRINT: {
                if (looks_like_string(o->src1)) {
                    emit("    invoke printf, offset fmt_str, offset str_%d\n", str_counter++);
                } else {
                    load_to_eax(o->src1);
                    emit("    invoke printf, offset fmt_int, eax\n");
                }
                break;
            }
            default:
                break;
        }
    }

    emit("    invoke ExitProcess, 0\n");
    emit("main ENDP\n");
    emit("END main\n");

    fclose(out);
    return 0;
}
