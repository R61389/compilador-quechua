#ifndef CODEGEN_H
#define CODEGEN_H

#include "irgen.h"

/* Genera ensamblador MASM32 (32-bit) listo para ml.exe / link.exe */
int codegen_emit(const IRList *ir, const char *out_path);

#endif /* CODEGEN_H */
