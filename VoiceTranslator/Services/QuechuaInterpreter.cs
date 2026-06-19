using System;
using System.Collections.Generic;
using System.Text;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Intérprete sencillo para Quechua Boliviano.
    /// Evalúa yupay, rimay, kutiy, sichus/mana chayqa, chaykamataq.
    /// Complementa al compilador: muestra el resultado real en pantalla.
    /// </summary>
    public class QuechuaInterpreter
    {
        private readonly Dictionary<string, double> _vars = new();
        private readonly StringBuilder _output = new();
        private int _line;
        private string[] _lines = Array.Empty<string>();

        public string Run(string source)
        {
            _vars.Clear();
            _output.Clear();
            _lines = source.Replace("\r\n", "\n").Split('\n');
            _line = 0;
            SkipHeader();
            try
            {
                RunBlock(topLevel: true);
            }
            catch (ReturnException ret)
            {
                _output.AppendLine($"\n[kutiy {ret.Value}]");
            }
            catch (Exception ex)
            {
                _output.AppendLine($"\n[Error: {ex.Message}]");
            }
            return _output.ToString();
        }

        // ── navigation ──────────────────────────────────────────────────

        private string? CurrentLine()
        {
            while (_line < _lines.Length)
            {
                var t = _lines[_line].Trim();
                if (t.Length > 0) return t;
                _line++;
            }
            return null;
        }

        private void SkipHeader()
        {
            // consume qallariy main()
            var l = CurrentLine();
            if (l != null && l.StartsWith("qallariy"))
                _line++;
        }

        // ── block runner ─────────────────────────────────────────────────

        private void RunBlock(bool topLevel = false)
        {
            while (_line < _lines.Length)
            {
                var raw = CurrentLine();
                if (raw == null) break;

                if (raw == "tukukun")
                {
                    _line++;
                    if (topLevel) break;
                    return;
                }
                if (raw.StartsWith("mana chayqa")) return;

                RunStatement(raw);
            }
        }

        // ── statement dispatch ───────────────────────────────────────────

        private void RunStatement(string line)
        {
            _line++;

            if (line.StartsWith("qallariy")) return;

            // yupay / tikraq / sayaq — declaración/asignación
            if (line.StartsWith("yupay ") || line.StartsWith("tikraq ") || line.StartsWith("sayaq "))
            {
                var rest = line.Substring(line.IndexOf(' ') + 1).Trim();
                ParseAssign(rest);
                return;
            }

            // asignación directa: nombre = expr
            if (IsAssignment(line, out var aName, out var aExpr))
            {
                _vars[aName!] = Eval(aExpr!);
                return;
            }

            // rimay
            if (line.StartsWith("rimay "))
            {
                var arg = line.Substring(6).Trim();
                if (arg.StartsWith("\"") && arg.EndsWith("\""))
                    _output.AppendLine(arg[1..^1]);
                else
                    _output.AppendLine(FormatNum(Eval(arg)));
                return;
            }

            // kutiy
            if (line.StartsWith("kutiy "))
            {
                var val = Eval(line.Substring(6).Trim());
                throw new ReturnException(val);
            }

            // sichus
            if (line.StartsWith("sichus "))
            {
                RunIf(line);
                return;
            }

            // chaykamataq
            if (line.StartsWith("chaykamataq "))
            {
                RunWhile(line);
                return;
            }
        }

        // ── if ───────────────────────────────────────────────────────────

        private void RunIf(string line)
        {
            var cond = ExtractParens(line.Substring(7).Trim());
            bool result = EvalBool(cond);

            int savedLine = _line;

            if (result)
            {
                RunBlock();
                // skip else block if present
                var cur = CurrentLine();
                if (cur != null && cur.StartsWith("mana chayqa"))
                {
                    _line++;
                    SkipBlock();
                }
            }
            else
            {
                SkipBlock();
                var cur = CurrentLine();
                if (cur != null && cur.StartsWith("mana chayqa"))
                {
                    _line++;
                    RunBlock();
                }
                else
                {
                    // consume tukukun
                    if (cur == "tukukun") _line++;
                }
            }
        }

        // ── while ────────────────────────────────────────────────────────

        private void RunWhile(string line)
        {
            var cond = ExtractParens(line.Substring(12).Trim());
            int loopStart = _line;
            int guard = 0;

            while (EvalBool(cond) && guard++ < 10_000)
            {
                _line = loopStart;
                RunBlock();
            }
            if (guard >= 10_000)
                _output.AppendLine("[bucle detenido: más de 10000 iteraciones]");
        }

        // ── skip block (branch not taken) ────────────────────────────────

        private void SkipBlock()
        {
            int depth = 0;
            while (_line < _lines.Length)
            {
                var l = _lines[_line].Trim();
                _line++;
                if (l.StartsWith("sichus") || l.StartsWith("chaykamataq")) depth++;
                if (l == "tukukun")
                {
                    if (depth == 0) return;
                    depth--;
                }
                if (l.StartsWith("mana chayqa") && depth == 0) { _line--; return; }
            }
        }

        // ── expression evaluator ─────────────────────────────────────────

        private double Eval(string expr)
        {
            expr = expr.Trim();

            // parentheses
            if (expr.StartsWith("(") && expr.EndsWith(")"))
                return Eval(expr[1..^1]);

            // string literal → 0
            if (expr.StartsWith("\"")) return 0;

            // binary +
            int i = FindOp(expr, '+');
            if (i > 0) return Eval(expr[..i]) + Eval(expr[(i + 1)..]);

            // binary -
            i = FindLastOp(expr, '-');
            if (i > 0) return Eval(expr[..i]) - Eval(expr[(i + 1)..]);

            // binary *
            i = FindOp(expr, '*');
            if (i > 0) return Eval(expr[..i]) * Eval(expr[(i + 1)..]);

            // binary /
            i = FindOp(expr, '/');
            if (i > 0)
            {
                var d = Eval(expr[(i + 1)..]);
                return d != 0 ? Eval(expr[..i]) / d : 0;
            }

            // number literal
            if (double.TryParse(expr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
                return num;

            // variable
            if (_vars.TryGetValue(expr, out var v)) return v;

            return 0;
        }

        private bool EvalBool(string expr)
        {
            expr = expr.Trim();

            // aswan hatun a b  →  a > b
            if (expr.StartsWith("aswan hatun "))
            {
                var parts = expr.Substring(12).Trim().Split(' ', 2);
                return Eval(parts[0]) > Eval(parts.Length > 1 ? parts[1] : "0");
            }
            // aswan pisi a b   →  a < b
            if (expr.StartsWith("aswan pisi "))
            {
                var parts = expr.Substring(11).Trim().Split(' ', 2);
                return Eval(parts[0]) < Eval(parts.Length > 1 ? parts[1] : "0");
            }
            // tupachiy a b     →  a == b
            if (expr.StartsWith("tupachiy "))
            {
                var parts = expr.Substring(9).Trim().Split(' ', 2);
                return Math.Abs(Eval(parts[0]) - Eval(parts.Length > 1 ? parts[1] : "0")) < 1e-9;
            }
            // mana chiqap a    →  !a
            if (expr.StartsWith("mana chiqap "))
                return Eval(expr.Substring(12).Trim()) == 0;

            return Eval(expr) != 0;
        }

        // ── helpers ──────────────────────────────────────────────────────

        private void ParseAssign(string rest)
        {
            var eq = rest.IndexOf('=');
            if (eq < 0) return;
            var name = rest[..eq].Trim();
            var val  = rest[(eq + 1)..].Trim();
            _vars[name] = Eval(val);
        }

        private static bool IsAssignment(string line, out string? name, out string? expr)
        {
            var eq = line.IndexOf('=');
            if (eq > 0 && !line.Contains(' ', 0, eq) && eq < line.Length - 1
                && line[eq - 1] != '!' && line[eq - 1] != '<' && line[eq - 1] != '>')
            {
                name = line[..eq].Trim();
                expr = line[(eq + 1)..].Trim();
                return true;
            }
            name = expr = null;
            return false;
        }

        private static string ExtractParens(string s)
        {
            s = s.Trim();
            if (s.StartsWith("(") && s.EndsWith(")")) return s[1..^1].Trim();
            return s;
        }

        private static int FindOp(string s, char op)
        {
            for (int i = 1; i < s.Length - 1; i++)
                if (s[i] == op) return i;
            return -1;
        }

        private static int FindLastOp(string s, char op)
        {
            for (int i = s.Length - 2; i > 0; i--)
                if (s[i] == op && s[i - 1] != 'e') return i;
            return -1;
        }

        private static string FormatNum(double v)
            => v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("G6");

        private class ReturnException : Exception
        {
            public double Value { get; }
            public ReturnException(double v) { Value = v; }
        }
    }
}
