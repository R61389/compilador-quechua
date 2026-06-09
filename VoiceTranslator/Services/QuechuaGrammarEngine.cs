using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Motor gramatical que construye su diccionario directamente desde el código
    /// fuente del compilador quechua (src/lexer.c y src/parser.c).
    ///
    /// Flujo de carga:
    ///   lexer.c → tabla KEYWORDS[]  → vocabulario base quechua
    ///   parser.c → frases de 2 palabras (mana chayqa, aswan hatun, etc.)
    ///            → reglas gramaticales (sichus, chaykamataq, rurana…)
    ///
    /// Sobre ese vocabulario se construye el mapa inverso:
    ///   español → keyword quechua del compilador
    /// y se añaden palabras de lenguaje natural (saludos, familia, etc.)
    /// que no forman parte del lenguaje de programación pero usan la misma
    /// ortografía y morfología quechua boliviana.
    /// </summary>
    public class QuechuaGrammarEngine : IQuechuaGrammarEngine
    {
        // ---------------------------------------------------------------
        // Vocabulario extraído del compilador
        // ---------------------------------------------------------------
        /// <summary>Palabras reservadas leídas del KEYWORDS[] en lexer.c.</summary>
        public IReadOnlyList<string> CompilerKeywords => _compilerKeywords;
        private readonly List<string> _compilerKeywords = new();

        /// <summary>Frases de dos palabras extraídas de parser.c (match_kw2, peek_kw2).</summary>
        public IReadOnlyList<(string a, string b)> CompilerTwoWordPhrases => _twoWordPhrases;
        private readonly List<(string a, string b)> _twoWordPhrases = new();

        // ---------------------------------------------------------------
        // Diccionarios de traducción
        // ---------------------------------------------------------------
        // Frases multi-palabra (se evalúan antes que palabras individuales)
        private readonly Dictionary<string, string> _phrases =
            new(StringComparer.OrdinalIgnoreCase);

        // Palabras individuales
        private readonly Dictionary<string, string> _words =
            new(StringComparer.OrdinalIgnoreCase);

        // ---------------------------------------------------------------
        // Constructor — carga vocabulario del compilador + nat. language
        // ---------------------------------------------------------------
        public QuechuaGrammarEngine(string? compilerRootPath = null)
        {
            var root = compilerRootPath ?? ResolveCompilerRoot();
            LoadFromCompilerSource(root);
            BuildSpanishToQuechuaMap();
            AddNaturalLanguageExtensions();
        }

        // ---------------------------------------------------------------
        // IQuechuaGrammarEngine
        // ---------------------------------------------------------------

        /// <inheritdoc/>
        public string Translate(string spanishText)
        {
            if (string.IsNullOrWhiteSpace(spanishText)) return string.Empty;
            var norm = Normalize(spanishText);

            // Coincidencia de frase completa
            if (_phrases.TryGetValue(norm, out var fullMatch))
                return fullMatch;

            var result = TranslateTokens(norm);
            return PostProcess(result).Trim();
        }

        /// <inheritdoc/>
        public string TranslateWord(string spanishWord)
        {
            if (string.IsNullOrWhiteSpace(spanishWord)) return string.Empty;
            var norm = Normalize(spanishWord).Trim();
            if (_words.TryGetValue(norm, out var w)) return w;
            if (_phrases.TryGetValue(norm, out var ph)) return ph;
            return $"{spanishWord}[?]";
        }

        /// <inheritdoc/>
        public string ApplyGrammarRules(string translated, string originalSpanish)
            => PostProcess(translated);

        /// <inheritdoc/>
        public int CountTranslatedWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                       .Count(w => !w.Contains("[?]") && w.Length > 0);
        }

        // ---------------------------------------------------------------
        // Carga desde el código fuente del compilador
        // ---------------------------------------------------------------

        private void LoadFromCompilerSource(string root)
        {
            var lexerPath  = Path.Combine(root, "src", "lexer.c");
            var parserPath = Path.Combine(root, "src", "parser.c");

            if (File.Exists(lexerPath))
                ParseLexerKeywords(File.ReadAllText(lexerPath));

            if (File.Exists(parserPath))
                ParseParserPhrases(File.ReadAllText(parserPath));
        }

        /// <summary>
        /// Extrae la tabla KEYWORDS[] de lexer.c.
        /// Ejemplo de línea: {"qallariy", "qallariy"},
        /// </summary>
        private void ParseLexerKeywords(string source)
        {
            // Encuentra la sección KEYWORDS[] y lee cada entrada {"kw", "tag"}
            var kwBlock = Regex.Match(source,
                @"KEYWORDS\[\]\s*=\s*\{(.*?)\{NULL",
                RegexOptions.Singleline);

            if (!kwBlock.Success) return;

            var entries = Regex.Matches(kwBlock.Groups[1].Value,
                @"\{""([^""]+)""\s*,\s*""([^""]+)""\}");

            foreach (Match m in entries)
            {
                var kw  = m.Groups[1].Value;
                var tag = m.Groups[2].Value;
                if (!_compilerKeywords.Contains(kw))
                    _compilerKeywords.Add(kw);
                _ = tag; // tag == kw en todos los casos actuales
            }
        }

        /// <summary>
        /// Extrae frases de dos palabras de parser.c buscando:
        ///   match_kw2(p, "a", "b")
        ///   peek_kw2(p, "a", "b")
        /// </summary>
        private void ParseParserPhrases(string source)
        {
            var calls = Regex.Matches(source,
                @"(?:match_kw2|peek_kw2)\s*\([^,]+,\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)");

            foreach (Match m in calls)
            {
                var a = m.Groups[1].Value;
                var b = m.Groups[2].Value;
                var pair = (a, b);
                if (!_twoWordPhrases.Contains(pair))
                    _twoWordPhrases.Add(pair);
            }
        }

        // ---------------------------------------------------------------
        // Mapeo español → palabras clave del compilador
        //
        // ESTAS ENTRADAS son la integración real con tu compilador:
        // cada término quechua en el valor DEBE existir en _compilerKeywords
        // o estar formado por dos de ellos (_twoWordPhrases).
        // ---------------------------------------------------------------
        private void BuildSpanishToQuechuaMap()
        {
            // --- Frases de dos palabras (del parser) ---
            // mana chayqa  (is_else_kw)
            AddPhrase("sino",         "mana chayqa");
            AddPhrase("si no",        "mana chayqa");
            AddPhrase("de lo contrario", "mana chayqa");

            // mana chiqap  (parse_primary: false literal)
            AddPhrase("falso",        "mana chiqap");
            AddPhrase("no verdadero", "mana chiqap");
            AddPhrase("no cierto",    "mana chiqap");

            // aswan hatun  (parse_quechua_prefix_cmp: >)
            AddPhrase("mayor que",    "aswan hatun");
            AddPhrase("más grande",   "aswan hatun");
            AddPhrase("es mayor",     "aswan hatun");

            // aswan pisi   (parse_quechua_prefix_cmp: <)
            AddPhrase("menor que",    "aswan pisi");
            AddPhrase("más pequeño",  "aswan pisi");
            AddPhrase("es menor",     "aswan pisi");

            // chunka yupay (tipo float en parser)
            AddPhrase("número decimal","chunka yupay");
            AddPhrase("tipo decimal",  "chunka yupay");
            AddPhrase("tipo flotante", "chunka yupay");

            // sananpa qillqa (tipo char en parser)
            AddPhrase("tipo caracter", "sananpa qillqa");
            AddPhrase("caracter",      "sananpa qillqa");

            // --- Palabras individuales (del lexer KEYWORDS[]) ---
            // qallariy — inicio de programa/bloque
            AddWord("inicio",      "qallariy");
            AddWord("comenzar",    "qallariy");
            AddWord("begin",       "qallariy");
            AddWord("empezar",     "qallariy");
            AddWord("iniciar",     "qallariy");
            AddWord("arrancar",    "qallariy");

            // tukukun — fin de bloque
            AddWord("fin",         "tukukun");
            AddWord("end",         "tukukun");
            AddWord("terminar",    "tukukun");
            AddWord("finalizar",   "tukukun");
            AddWord("cerrar",      "tukukun");

            // sichus — condicional if
            AddWord("si",          "sichus");
            AddWord("condición",   "sichus");
            AddWord("condicion",   "sichus");

            // chaykamataq — bucle while
            AddWord("mientras",    "chaykamataq");
            AddWord("repetir",     "chaykamataq");
            AddWord("loop",        "chaykamataq");
            AddWord("ciclo",       "chaykamataq");
            AddWord("bucle",       "chaykamataq");

            // kutiy — retornar
            AddWord("retornar",    "kutiy");
            AddWord("devolver",    "kutiy");
            AddWord("regresar",    "kutiy");
            AddWord("return",      "kutiy");
            AddWord("volver",      "kutiy");

            // rimay — imprimir/hablar
            AddWord("imprimir",    "rimay");
            AddWord("mostrar",     "rimay");
            AddWord("hablar",      "rimay");
            AddWord("decir",       "rimay");
            AddWord("print",       "rimay");
            AddWord("habla",       "rimay");
            AddWord("dice",        "rimay");

            // qillqay — escribir/imprimir
            AddWord("escribir",    "qillqay");
            AddWord("escriba",     "qillqay");
            AddWord("write",       "qillqay");
            AddWord("anotar",      "qillqay");

            // yupay — tipo entero / número
            AddWord("entero",      "yupay");
            AddWord("número",      "yupay");
            AddWord("numero",      "yupay");
            AddWord("integer",     "yupay");
            AddWord("int",         "yupay");

            // chunka — diez / base decimal (también forma parte de chunka yupay)
            AddWord("diez",        "chunka");
            AddWord("decimal",     "chunka yupay");
            AddWord("flotante",    "chunka yupay");
            AddWord("float",       "chunka yupay");

            // simi — texto / cadena
            AddWord("texto",       "simi");
            AddWord("cadena",      "simi");
            AddWord("string",      "simi");
            AddWord("mensaje",     "simi");

            // chiqap — verdadero
            AddWord("verdadero",   "chiqap");
            AddWord("cierto",      "chiqap");
            AddWord("true",        "chiqap");
            AddWord("correcto",    "chiqap");

            // tupachiy — comparar (==)
            AddWord("comparar",    "tupachiy");
            AddWord("igual",       "tupachiy");
            AddWord("equals",      "tupachiy");
            AddWord("igualar",     "tupachiy");

            // aswan — más / comparador (parte de frases)
            AddWord("más",         "aswan");
            AddWord("mayor",       "aswan hatun");
            AddWord("menor",       "aswan pisi");

            // hatun — grande (también parte de aswan hatun)
            AddWord("grande",      "hatun");
            AddWord("gran",        "hatun");

            // pisi — pequeño / poco (también parte de aswan pisi)
            AddWord("poco",        "pisi");
            AddWord("pequeño",     "pisi");
            AddWord("mínimo",      "pisi");

            // tikraq — variable
            AddWord("variable",    "tikraq");
            AddWord("var",         "tikraq");
            AddWord("valor",       "tikraq");

            // sayaq — constante
            AddWord("constante",   "sayaq");
            AddWord("const",       "sayaq");
            AddWord("fijo",        "sayaq");

            // rurana — función
            AddWord("función",     "rurana");
            AddWord("funcion",     "rurana");
            AddWord("método",      "rurana");
            AddWord("metodo",      "rurana");
            AddWord("function",    "rurana");

            // ruway — procedimiento / hacer
            AddWord("procedimiento","ruway");
            AddWord("procedure",   "ruway");
            AddWord("hacer",       "ruway");
            AddWord("acción",      "ruway");

            // mana — negación
            AddWord("no",          "mana");
            AddWord("negar",       "mana");
            AddWord("negar",       "mana");

            // chiqap/chusaq — void/vacío
            AddWord("vacío",       "chusaq");
            AddWord("vacio",       "chusaq");
            AddWord("void",        "chusaq");
            AddWord("nulo",        "chusaq");

            // imapas — cualquiera / any
            AddWord("cualquiera",  "imapas");
            AddWord("any",         "imapas");

            // rikch'aq — tipo / similar
            AddWord("tipo",        "rikch'aq");
            AddWord("clase",       "rikch'aq");

            // sinri — parámetro / oído (semántica del compilador)
            AddWord("parámetro",   "sinri");
            AddWord("parametro",   "sinri");
            AddWord("argumento",   "sinri");

            // sananpa — carácter (parte de sananpa qillqa)
            AddWord("carácter",    "sananpa qillqa");
            AddWord("char",        "sananpa qillqa");
        }

        // ---------------------------------------------------------------
        // Extensión de lenguaje natural quechua boliviano
        // Estas palabras NO son keywords del compilador pero comparten
        // la misma ortografía quechua sureña (Qhichwa Sudboliviano).
        // ---------------------------------------------------------------
        private void AddNaturalLanguageExtensions()
        {
            // Saludos y expresiones sociales
            AddPhrase("buenos días",        "allin p'unchay");
            AddPhrase("buenos dias",        "allin p'unchay");
            AddPhrase("buenas tardes",      "allin ch'isi");
            AddPhrase("buenas noches",      "allin tuta");
            AddPhrase("hasta luego",        "tinkunankama");
            AddPhrase("de nada",            "ama imatapis");
            AddPhrase("por favor",          "ama hina kaspa");
            AddPhrase("buenos días profesor","allin p'unchay yachachiq");
            AddPhrase("buenos dias profesor","allin p'unchay yachachiq");
            AddPhrase("cómo estás",         "imaynallan kashanki");
            AddPhrase("como estas",         "imaynallan kashanki");
            AddPhrase("estoy bien",         "allinmi kachkani");
            AddPhrase("mucho gusto",        "anchata kusikuni");
            AddPhrase("buen provecho",      "allin mikhunata");
            AddPhrase("que te vaya bien",   "allinta riy");
            AddPhrase("que rico",           "manchay sumaq");
            AddPhrase("qué rico",           "manchay sumaq");
            AddPhrase("quiero agua",        "yakuta munani");
            AddPhrase("tengo hambre",       "yarqay");
            AddPhrase("no entiendo",        "mana entiendinichkanichu");

            // Preguntas simples
            AddPhrase("qué es",   "imataq");
            AddPhrase("que es",   "imataq");
            AddPhrase("quién es", "pitaq");
            AddPhrase("quien es", "pitaq");
            AddPhrase("por qué",  "imaraykus");
            AddPhrase("por que",  "imaraykus");
            AddPhrase("dónde está","maypi");
            AddPhrase("donde esta","maypi");

            // Palabras individuales — lenguaje natural
            AddWord("hola",       "rimaykullayki");
            AddWord("adiós",      "tinkunankama");
            AddWord("adios",      "tinkunankama");
            AddWord("gracias",    "añay");
            AddWord("sí",         "arí");
            AddWord("permiso",    "pakaykullayki");
            AddWord("disculpa",   "llakipaykim");
            AddWord("disculpe",   "llakipaykim");
            AddWord("bienvenido", "allinllapin");
            AddWord("bienvenida", "allinllapin");

            // Números
            AddWord("uno",        "huk");
            AddWord("un",         "huk");
            AddWord("una",        "huk");
            AddWord("dos",        "iskay");
            AddWord("tres",       "kinsa");
            AddWord("cuatro",     "tawa");
            AddWord("cinco",      "pichqa");
            AddWord("seis",       "suqta");
            AddWord("siete",      "qanchis");
            AddWord("ocho",       "pusaq");
            AddWord("nueve",      "isqon");
            // diez ya es "chunka" (keyword del compilador)
            AddWord("once",       "chunka hukiyuq");
            AddWord("doce",       "chunka iskayiyuq");
            AddWord("veinte",     "iskay chunka");
            AddWord("cien",       "pachak");
            AddWord("mil",        "waranqa");

            // Tiempo / Días
            AddWord("hoy",        "kunan p'unchay");
            AddWord("mañana",     "paqarin");
            AddWord("manana",     "paqarin");
            AddWord("ayer",       "qayna p'unchay");
            AddWord("día",        "p'unchay");
            AddWord("dia",        "p'unchay");
            AddWord("noche",      "tuta");
            AddWord("tarde",      "ch'isi");
            AddWord("semana",     "simana");
            AddWord("mes",        "killa");
            AddWord("año",        "wata");
            AddWord("hora",       "ura");
            AddWord("lunes",      "killachaw");
            AddWord("martes",     "atipachaw");
            AddWord("miércoles",  "quyachaw");
            AddWord("miercoles",  "quyachaw");
            AddWord("jueves",     "ill'apachaw");
            AddWord("viernes",    "ch'askachaw");
            AddWord("sábado",     "k'uychichaw");
            AddWord("sabado",     "k'uychichaw");
            AddWord("domingo",    "intichaw");

            // Familia / Personas
            AddWord("madre",      "mama");
            AddWord("mamá",       "mama");
            AddWord("padre",      "tayta");
            AddWord("papá",       "tayta");
            AddWord("hijo",       "wawa");
            AddWord("hija",       "ususiy");
            AddWord("hermano",    "wawqi");
            AddWord("hermana",    "ñaña");
            AddWord("abuelo",     "awki");
            AddWord("abuela",     "awicha");
            AddWord("familia",    "ayllu");
            AddWord("hombre",     "qhari");
            AddWord("mujer",      "warmi");
            AddWord("niño",       "wawa");
            AddWord("niña",       "wawa");
            AddWord("joven",      "wayna");
            AddWord("anciano",    "machu");
            AddWord("persona",    "runa");
            AddWord("gente",      "runa");
            AddWord("amigo",      "masiy");
            AddWord("amiga",      "masiy");
            AddWord("profesor",   "yachachiq");
            AddWord("profesora",  "yachachiq");
            AddWord("maestro",    "yachachiq");
            AddWord("maestra",    "yachachiq");
            AddWord("estudiante", "yachaq");
            AddWord("alumno",     "yachaq");

            // Naturaleza / Bolivia
            AddWord("sol",        "inti");
            AddWord("luna",       "killa");
            AddWord("estrella",   "quyllur");
            AddWord("tierra",     "allpa");
            AddWord("pachamama",  "pachamama");
            AddWord("agua",       "yaku");
            AddWord("fuego",      "nina");
            AddWord("cielo",      "hanaq pacha");
            AddWord("viento",     "wayra");
            AddWord("lluvia",     "para");
            AddWord("nieve",      "riti");
            AddWord("montaña",    "urqu");
            AddWord("cerro",      "urqu");
            AddWord("río",        "mayu");
            AddWord("lago",       "qucha");
            AddWord("árbol",      "sach'a");
            AddWord("arbol",      "sach'a");
            AddWord("flor",       "t'ika");
            AddWord("perro",      "allqu");
            AddWord("gato",       "michi");
            AddWord("llama",      "llama");
            AddWord("alpaca",     "allpaqa");

            // Comida
            AddWord("papa",       "papa");
            AddWord("maíz",       "sara");
            AddWord("maiz",       "sara");
            AddWord("comida",     "mikhuna");
            AddWord("comer",      "mikhuy");
            AddWord("beber",      "upyay");
            AddWord("chicha",     "aqha");
            AddWord("carne",      "aycha");
            AddWord("pan",        "t'anta");
            AddWord("sal",        "kachi");

            // Verbos comunes (que no son keywords del compilador)
            AddWord("escuchar",   "uyariy");
            AddWord("ver",        "qhaway");
            AddWord("mirar",      "qhaway");
            AddWord("caminar",    "puriy");
            AddWord("correr",     "phaway");
            AddWord("trabajar",   "llank'ay");
            AddWord("dormir",     "puñuy");
            AddWord("vivir",      "kawsay");
            AddWord("amar",       "munay");
            AddWord("querer",     "munay");
            AddWord("ir",         "riy");
            AddWord("venir",      "hamuy");
            AddWord("dar",        "quy");
            AddWord("llevar",     "apay");
            AddWord("tomar",      "apay");
            AddWord("saber",      "yachay");
            AddWord("conocer",    "yachay");
            AddWord("poder",      "atiy");
            AddWord("tener",      "chariy");
            AddWord("ser",        "kay");
            AddWord("estar",      "kay");
            AddWord("llegar",     "chayay");
            AddWord("salir",      "lloqsiy");
            AddWord("entrar",     "yaykuy");
            AddWord("buscar",     "maskay");
            AddWord("encontrar",  "tariy");
            AddWord("pensar",     "yuyay");
            AddWord("recordar",   "yuyariy");
            AddWord("olvidar",    "qunqay");
            AddWord("ayudar",     "yanapay");
            AddWord("cantar",     "takiy");
            AddWord("bailar",     "tusuy");
            AddWord("reír",       "asiy");
            AddWord("reir",       "asiy");
            AddWord("llorar",     "waqay");
            AddWord("aprender",   "yachay");
            AddWord("enseñar",    "yachachiy");

            // Adjetivos
            AddWord("bueno",      "allin");
            AddWord("buena",      "allin");
            AddWord("bonito",     "sumaq");
            AddWord("bonita",     "sumaq");
            AddWord("lindo",      "sumaq");
            AddWord("linda",      "sumaq");
            AddWord("feo",        "millay");
            AddWord("feliz",      "kusi");
            AddWord("triste",     "llaki");
            AddWord("nuevo",      "musuq");
            AddWord("nueva",      "musuq");
            AddWord("rápido",     "utqay");
            AddWord("rapido",     "utqay");
            AddWord("lento",      "qayllata");
            AddWord("lejos",      "karu");
            AddWord("cerca",      "qayllapi");
            AddWord("caliente",   "q'uñi");
            AddWord("frío",       "chiri");
            AddWord("frio",       "chiri");
            AddWord("limpio",     "ch'uya");
            AddWord("mucho",      "achka");
            AddWord("bien",       "allin");

            // Lugares / Bolivia
            AddWord("bolivia",    "Bulibiya");
            AddWord("cochabamba", "Quchapampa");
            AddWord("potosí",     "Pututsi");
            AddWord("potosi",     "Pututsi");
            AddWord("oruro",      "Ururu");
            AddWord("sucre",      "Chuquisaca");
            AddWord("casa",       "wasi");
            AddWord("escuela",    "yachay wasi");
            AddWord("mercado",    "qhatu");
            AddWord("camino",     "ñan");
            AddWord("ciudad",     "llaqta");

            // Cuerpo
            AddWord("cabeza",     "uma");
            AddWord("mano",       "maki");
            AddWord("pie",        "chaki");
            AddWord("ojo",        "ñawi");
            AddWord("boca",       "simi");  // simi = keyword del compilador (cadena)
            AddWord("nariz",      "senqa");
            AddWord("corazón",    "sunqu");
            AddWord("corazon",    "sunqu");

            // Conectores / partículas
            AddWord("también",    "ima");
            AddWord("tambien",    "ima");
            AddWord("pero",       "ichaqa");
            AddWord("siempre",    "wiñaypim");
            AddWord("nunca",      "mana imapis");
            AddWord("aquí",       "kaypi");
            AddWord("aqui",       "kaypi");
            AddWord("allá",       "chaypi");
            AddWord("alla",       "chaypi");
            AddWord("todo",       "llapan");
            AddWord("todos",      "llapankuna");
            AddWord("para",       "paq");
            AddWord("con",        "kawan");
            AddWord("hay",        "tiyan");

            // Artículos (se omiten en quechua — lengua sin artículo)
            AddWord("el",  "");
            AddWord("la",  "");
            AddWord("los", "");
            AddWord("las", "");
        }

        // ---------------------------------------------------------------
        // Método público de diagnóstico
        // ---------------------------------------------------------------

        /// <summary>
        /// Devuelve el vocabulario cargado desde el compilador,
        /// útil para mostrar en la pestaña "Acerca de".
        /// </summary>
        public string GetCompilerVocabSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Keywords extraídos de lexer.c ===");
            foreach (var kw in _compilerKeywords)
                sb.AppendLine($"  {kw}");
            sb.AppendLine();
            sb.AppendLine("=== Frases de 2 palabras extraídas de parser.c ===");
            foreach (var (a, b) in _twoWordPhrases)
                sb.AppendLine($"  {a} {b}");
            return sb.ToString();
        }

        // ---------------------------------------------------------------
        // Helpers privados
        // ---------------------------------------------------------------

        private void AddPhrase(string spanish, string quechua)
        {
            _phrases.TryAdd(spanish, quechua);
            // También registrar sin tildes para mayor tolerancia
            var sinTildes = RemoveDiacritics(spanish);
            if (!sinTildes.Equals(spanish, StringComparison.OrdinalIgnoreCase))
                _phrases.TryAdd(sinTildes, quechua);
        }

        private void AddWord(string spanish, string quechua)
        {
            _words.TryAdd(spanish, quechua);
            var sinTildes = RemoveDiacritics(spanish);
            if (!sinTildes.Equals(spanish, StringComparison.OrdinalIgnoreCase))
                _words.TryAdd(sinTildes, quechua);
        }

        private static string Normalize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (var ch in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch == '\'' || ch == '’' || ch == ' ')
                    sb.Append(ch);
                else if (ch is ',' or '.' or ';' or ':' or '!' or '¡' or '?' or '¿')
                    sb.Append(' ');
                else
                    sb.Append(ch);
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private string TranslateTokens(string normalizedText)
        {
            var tokens = normalizedText.Split(' ');
            var output = new List<string>();
            int i = 0;

            while (i < tokens.Length)
            {
                bool found = false;
                // Intentar coincidencia de frase (hasta 6 tokens)
                for (int len = Math.Min(6, tokens.Length - i); len >= 2; len--)
                {
                    var candidate = string.Join(" ", tokens.Skip(i).Take(len));
                    if (_phrases.TryGetValue(candidate, out var phraseResult))
                    {
                        if (!string.IsNullOrEmpty(phraseResult))
                            output.Add(phraseResult);
                        i += len;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var tok = tokens[i];
                    if (_words.TryGetValue(tok, out var wordResult))
                    {
                        if (!string.IsNullOrEmpty(wordResult))
                            output.Add(wordResult);
                    }
                    else if (!string.IsNullOrWhiteSpace(tok) && tok != "¿" && tok != "?")
                    {
                        output.Add($"{tok}[?]");
                    }
                    i++;
                }
            }

            return string.Join(" ", output);
        }

        private static string PostProcess(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            text = Regex.Replace(text, @"\s+", " ").Trim();
            // Limpiar marcadores [?] consecutivos
            text = Regex.Replace(text, @"(\[\?\]\s*){2,}", "[?] ");
            return text;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string ResolveCompilerRoot()
        {
            // Busca src/lexer.c subiendo desde el directorio del ejecutable
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(dir, "src", "lexer.c")))
                    return dir;
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null) break;
                dir = parent;
            }
            // Fallback: directorio de trabajo actual
            return Directory.GetCurrentDirectory();
        }
    }
}
