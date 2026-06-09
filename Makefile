# Makefile — quechuac (C99)
CC      = gcc
CFLAGS  = -std=c99 -Wall -Wextra -Iinclude
SRCDIR  = src
OBJDIR  = obj
BINDIR  = .
TARGET  = quechuac

SRCS = $(SRCDIR)/main.c \
       $(SRCDIR)/lexer.c \
       $(SRCDIR)/ast.c \
       $(SRCDIR)/parser.c \
       $(SRCDIR)/symtable.c \
       $(SRCDIR)/irgen.c \
       $(SRCDIR)/optimizer.c \
       $(SRCDIR)/codegen.c \
       $(SRCDIR)/report.c

OBJS = $(patsubst $(SRCDIR)/%.c,$(OBJDIR)/%.o,$(SRCS))

.PHONY: all clean run test

all: $(OBJDIR) $(TARGET)

$(OBJDIR):
	mkdir -p $(OBJDIR)

$(TARGET): $(OBJS)
	$(CC) $(CFLAGS) -o $(BINDIR)/$(TARGET) $(OBJS)

$(OBJDIR)/%.o: $(SRCDIR)/%.c
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	rm -rf $(OBJDIR) $(TARGET) output.asm output.o output

run: all
	./$(TARGET) quechua.txt -o output.asm -v

test: run
