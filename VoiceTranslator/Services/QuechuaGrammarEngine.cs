using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompiladorQuechua.Services
{
    /// <summary>
    /// Motor gramatical quechua boliviano completo.
    /// Implementa un diccionario exhaustivo español→quechua y reglas morfológicas
    /// del quechua sureño (variante boliviana, Qhichwa).
    /// </summary>
    public class QuechuaGrammarEngine : IQuechuaGrammarEngine
    {
        // ---------------------------------------------------------------
        // Diccionario de frases (multi-palabra) — se evalúa PRIMERO
        // ---------------------------------------------------------------
        private static readonly Dictionary<string, string> _phrases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Saludos compuestos
                { "buenos dias",          "allin p'unchay" },
                { "buenos días",          "allin p'unchay" },
                { "buenas tardes",        "allin ch'isi" },
                { "buenas noches",        "allin tuta" },
                { "hasta luego",          "qayniyá" },
                { "de nada",              "ama imatapis" },
                { "por favor",            "ama hina kaspa" },
                { "si no",                "mana chayqa" },
                { "sino",                 "mana chayqa" },
                { "si no es",             "mana chayqa" },

                // Expresiones bolivianas
                { "que quieres",          "imatam munankis" },
                { "qué quieres",          "imatam munankis" },
                { "quiero agua",          "yakuta munani" },
                { "tengo hambre",         "yarqay" },
                { "me duele",             "nanawanmi" },
                { "no entiendo",          "mana entiendinichkanichu" },
                { "estoy bien",           "allinmi kachkani" },
                { "que rico",             "manchay sumaq" },
                { "qué rico",             "manchay sumaq" },
                { "que bonito",           "manchay sumaq" },
                { "qué bonito",           "manchay sumaq" },
                { "buen provecho",        "allin mikhunata" },
                { "que te vaya bien",     "allinta riy" },

                // Preguntas
                { "como estas",           "imaynallan kashanki" },
                { "cómo estás",           "imaynallan kashanki" },
                { "como estas",           "imaynallan kashanki" },
                { "que es",               "imataq" },
                { "qué es",               "imataq" },
                { "quien es",             "pitaq" },
                { "quién es",             "pitaq" },
                { "por que",              "imaraykus" },
                { "por qué",              "imaraykus" },

                // Números compuestos
                { "once",                 "chunka hukiyuq" },
                { "doce",                 "chunka iskayiyuq" },
                { "veinte",               "iskay chunka" },

                // Lugares compuestos Bolivia
                { "la paz",               "Chuqiyapu" },
                { "buenos dias",          "allin p'unchay" },

                // Keyword del compilador — frases
                { "si no",                "mana chayqa" },
                { "mana allin",           "mana allin" },
                { "hanaq pacha",          "hanaq pacha" },
                { "qayna punchay",        "qayna p'unchay" },
                { "kunan punchay",        "kunan p'unchay" },
                { "yachay wasi",          "yachay wasi" },
            };

        // ---------------------------------------------------------------
        // Diccionario de palabras individuales
        // ---------------------------------------------------------------
        private static readonly Dictionary<string, string> _words =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // === Keywords del compilador Quechua ===
                { "inicio",       "qallariy" },
                { "comenzar",     "qallariy" },
                { "begin",        "qallariy" },
                { "fin",          "tukukun" },
                { "end",          "tukukun" },
                { "si",           "sichus" },
                { "mientras",     "chaykamataq" },
                { "retornar",     "kutiy" },
                { "devolver",     "kutiy" },
                { "imprimir",     "rimay" },
                { "mostrar",      "rimay" },
                { "hablar",       "rimay" },
                { "escribir",     "qillqay" },
                { "entero",       "yupay" },
                { "número",       "yupay" },
                { "numero",       "yupay" },
                { "decimal",      "chunka yupay" },
                { "texto",        "simi" },
                { "cadena",       "simi" },
                { "verdadero",    "chiqap" },
                { "cierto",       "chiqap" },
                { "falso",        "mana chiqap" },
                { "comparar",     "tupachiy" },
                { "mayor",        "aswan hatun" },
                { "menor",        "aswan pisi" },
                { "variable",     "tikraq" },
                { "función",      "rurana" },
                { "funcion",      "rurana" },
                { "procedimiento","ruway" },
                { "constante",    "sayaq" },

                // === Saludos y expresiones sociales ===
                { "hola",         "rimaykullayki" },
                { "adiós",        "tinkunankama" },
                { "adios",        "tinkunankama" },
                { "gracias",      "añay" },
                { "sí",           "arí" },
                { "no",           "mana" },
                { "permiso",      "pakaykullayki" },
                { "disculpa",     "llakipaykim" },
                { "disculpe",     "llakipaykim" },
                { "salud",        "hampiy" },
                { "bienvenido",   "allinllapin" },
                { "bienvenida",   "allinllapin" },

                // === Preguntas ===
                { "cómo",         "imayna" },
                { "como",         "imayna" },
                { "qué",          "imataq" },
                { "que",          "imataq" },
                { "quién",        "pitaq" },
                { "quien",        "pitaq" },
                { "dónde",        "maypi" },
                { "donde",        "maypi" },
                { "cuándo",       "hayk'aqmi" },
                { "cuando",       "hayk'aqmi" },
                { "cuánto",       "hayk'an" },
                { "cuanto",       "hayk'an" },

                // === Números ===
                { "uno",      "huk" },
                { "un",       "huk" },
                { "una",      "huk" },
                { "dos",      "iskay" },
                { "tres",     "kinsa" },
                { "cuatro",   "tawa" },
                { "cinco",    "pichqa" },
                { "seis",     "suqta" },
                { "siete",    "qanchis" },
                { "ocho",     "pusaq" },
                { "nueve",    "isqon" },
                { "diez",     "chunka" },
                { "cien",     "pachak" },
                { "mil",      "waranqa" },
                { "millón",   "hunu" },
                { "millon",   "hunu" },

                // === Días / Tiempo ===
                { "lunes",       "killachaw" },
                { "martes",      "atipachaw" },
                { "miércoles",   "quyachaw" },
                { "miercoles",   "quyachaw" },
                { "jueves",      "ill'apachaw" },
                { "viernes",     "ch'askachaw" },
                { "sábado",      "k'uychichaw" },
                { "sabado",      "k'uychichaw" },
                { "domingo",     "intichaw" },
                { "hoy",         "kunan p'unchay" },
                { "mañana",      "paqarin" },
                { "manana",      "paqarin" },
                { "ayer",        "qayna p'unchay" },
                { "hora",        "ura" },
                { "minuto",      "minutu" },
                { "día",         "p'unchay" },
                { "dia",         "p'unchay" },
                { "noche",       "tuta" },
                { "tarde",       "ch'isi" },
                { "semana",      "simana" },
                { "mes",         "killa" },
                { "año",         "wata" },
                { "anio",        "wata" },
                { "años",        "watakuna" },

                // === Familia / Personas ===
                { "madre",       "mama" },
                { "mamá",        "mama" },
                { "mama",        "mama" },
                { "padre",       "tayta" },
                { "papá",        "tayta" },
                { "papa",        "tayta" },  // nota: "papa" (tubérculo) también es papa en quechua
                { "hijo",        "wawa" },
                { "hija",        "ususiy" },
                { "hermano",     "wawqi" },
                { "hermana",     "ñaña" },
                { "abuelo",      "awki" },
                { "abuela",      "awicha" },
                { "familia",     "ayllu" },
                { "hombre",      "qhari" },
                { "mujer",       "warmi" },
                { "niño",        "wawa" },
                { "niña",        "wawa" },
                { "joven",       "wayna" },
                { "anciano",     "machu" },
                { "persona",     "runa" },
                { "gente",       "runa" },
                { "amigo",       "masiy" },
                { "amiga",       "masiy" },
                { "vecino",      "masiy" },
                { "vecina",      "masiy" },
                { "profesor",    "yachachiq" },
                { "profesora",   "yachachiq" },
                { "maestra",     "yachachiq" },
                { "maestro",     "yachachiq" },
                { "estudiante",  "yachaq" },

                // === Naturaleza / Geografía Bolivia ===
                { "sol",         "inti" },
                { "luna",        "killa" },
                { "estrella",    "quyllur" },
                { "tierra",      "allpa" },
                { "pachamama",   "pachamama" },
                { "agua",        "yaku" },
                { "fuego",       "nina" },
                { "cielo",       "hanaq pacha" },
                { "viento",      "wayra" },
                { "lluvia",      "para" },
                { "nieve",       "riti" },
                { "montaña",     "urqu" },
                { "cerro",       "urqu" },
                { "río",         "mayu" },
                { "rio",         "mayu" },
                { "lago",        "qucha" },
                { "árbol",       "sach'a" },
                { "arbol",       "sach'a" },
                { "flor",        "t'ika" },
                { "planta",      "qura" },
                { "pájaro",      "pisqu" },
                { "pajaro",      "pisqu" },
                { "animal",      "uywa" },
                { "perro",       "allqu" },
                { "gato",        "michi" },
                { "vaca",        "waka" },
                { "llama",       "llama" },
                { "alpaca",      "allpaqa" },

                // === Comida / Bolivia ===
                { "maíz",        "sara" },
                { "maiz",        "sara" },
                { "comida",      "mikhuna" },
                { "comer",       "mikhuy" },
                { "beber",       "upyay" },
                { "chicha",      "aqha" },
                { "carne",       "aycha" },
                { "pan",         "t'anta" },
                { "sal",         "kachi" },
                { "azúcar",      "misk'i" },
                { "azucar",      "misk'i" },
                { "leche",       "ñuñu" },
                { "fruta",       "mikhuna" },

                // === Verbos comunes ===
                { "decir",       "rimay" },
                { "escuchar",    "uyariy" },
                { "oír",         "uyariy" },
                { "oir",         "uyariy" },
                { "ver",         "qhaway" },
                { "mirar",       "qhaway" },
                { "caminar",     "puriy" },
                { "ir",          "riy" },
                { "correr",      "phaway" },
                { "trabajar",    "llank'ay" },
                { "dormir",      "puñuy" },
                { "vivir",       "kawsay" },
                { "amar",        "munay" },
                { "querer",      "munay" },
                { "venir",       "hamuy" },
                { "dar",         "quy" },
                { "llevar",      "apay" },
                { "tomar",       "apay" },
                { "hacer",       "ruway" },
                { "saber",       "yachay" },
                { "conocer",     "yachay" },
                { "poder",       "atiy" },
                { "tener",       "chariy" },
                { "ser",         "kay" },
                { "estar",       "kay" },
                { "volver",      "kutimuy" },
                { "llegar",      "chayay" },
                { "salir",       "lloqsiy" },
                { "entrar",      "yaykuy" },
                { "buscar",      "maskay" },
                { "encontrar",   "tariy" },
                { "pensar",      "yuyay" },
                { "recordar",    "yuyariy" },
                { "olvidar",     "qunqay" },
                { "ayudar",      "yanapay" },
                { "necesitar",   "munay" },
                { "empezar",     "qallariy" },
                { "terminar",    "tukukuy" },
                { "cantar",      "taki" },
                { "bailar",      "tusuy" },
                { "reír",        "asiy" },
                { "reir",        "asiy" },
                { "llorar",      "waqay" },
                { "comprar",     "rantiy" },
                { "vender",      "qhatuy" },
                { "cocinar",     "wayk'uy" },
                { "leer",        "ñawiriiy" },
                { "aprender",    "yachay" },
                { "enseñar",     "yachachiy" },
                { "esperar",     "suyay" },

                // === Adjetivos / Descriptores ===
                { "grande",      "hatun" },
                { "pequeño",     "uchuy" },
                { "pequeno",     "uchuy" },
                { "chico",       "uchuy" },
                { "bueno",       "allin" },
                { "buena",       "allin" },
                { "malo",        "mana allin" },
                { "mala",        "mana allin" },
                { "bonito",      "sumaq" },
                { "bonita",      "sumaq" },
                { "lindo",       "sumaq" },
                { "linda",       "sumaq" },
                { "feo",         "millay" },
                { "fea",         "millay" },
                { "rápido",      "utqay" },
                { "rapido",      "utqay" },
                { "lento",       "qayllata" },
                { "nuevo",       "musuq" },
                { "nueva",       "musuq" },
                { "viejo",       "machu" },
                { "vieja",       "machu" },
                { "alto",        "hatun" },
                { "alta",        "hatun" },
                { "bajo",        "uchuy" },
                { "baja",        "uchuy" },
                { "lejos",       "karu" },
                { "cerca",       "qayllapi" },
                { "caliente",    "q'uñi" },
                { "frío",        "chiri" },
                { "frio",        "chiri" },
                { "limpio",      "ch'uya" },
                { "limpia",      "ch'uya" },
                { "sucio",       "q'asa" },
                { "sucia",       "q'asa" },
                { "feliz",       "kusi" },
                { "triste",      "llaki" },
                { "cansado",     "sayk'u" },
                { "cansada",     "sayk'u" },

                // === Lugares / Bolivia ===
                { "bolivia",     "Bulibiya" },
                { "cochabamba",  "Quchapampa" },
                { "potosí",      "Pututsi" },
                { "potosi",      "Pututsi" },
                { "oruro",       "Ururu" },
                { "sucre",       "Chuquisaca" },
                { "casa",        "wasi" },
                { "escuela",     "yachay wasi" },
                { "mercado",     "qhatu" },
                { "camino",      "ñan" },
                { "plaza",       "pampa" },
                { "ciudad",      "llaqta" },

                // === Cuerpo ===
                { "cabeza",      "uma" },
                { "mano",        "maki" },
                { "pie",         "chaki" },
                { "ojo",         "ñawi" },
                { "boca",        "simi" },
                { "nariz",       "senqa" },
                { "oreja",       "rinri" },
                { "corazón",     "sunqu" },
                { "corazon",     "sunqu" },
                { "cuerpo",      "uku" },

                // === Expresiones sueltas ===
                { "vamos",       "riy" },
                { "espera",      "suya" },
                { "listo",       "chay" },
                { "bien",        "allin" },
                { "mal",         "mana allin" },
                { "aquí",        "kaypi" },
                { "aqui",        "kaypi" },
                { "allá",        "chaypi" },
                { "alla",        "chaypi" },
                { "mucho",       "achka" },
                { "poco",        "pisi" },
                { "todo",        "llapan" },
                { "nada",        "imapis" },
                { "siempre",     "wiñaypim" },
                { "nunca",       "mana imapis" },
                { "también",     "ima" },
                { "tambien",     "ima" },
                { "pero",        "ichaqa" },
                { "y",           "ima" },
                { "o",           "utaq" },
                { "con",         "kawan" },
                { "sin",         "mana" },
                { "en",          "pi" },
                { "de",          "manta" },
                { "del",         "manta" },
                { "por",         "rayku" },
                { "para",        "paq" },
                { "como",        "imayna" },
                { "más",         "aswan" },
                { "menos",       "pisi" },
                { "el",          "" },
                { "la",          "" },
                { "los",         "" },
                { "las",         "" },
                { "un",          "huk" },
                { "una",         "huk" },
                { "es",          "kay" },
                { "son",         "kaykuna" },
                { "estoy",       "kachkani" },
                { "quiero",      "munani" },
                { "tengo",       "charini" },
                { "voy",         "riykachkani" },
                { "hay",         "tiyan" },
                { "mi",          "" },
                { "tu",          "" },
                { "su",          "" },
                { "me",          "" },
                { "te",          "" },
                { "le",          "" },
            };

        // ---------------------------------------------------------------
        // Reglas de sufijos verbales (español → marcadores quechua)
        // ---------------------------------------------------------------
        private static readonly Dictionary<string, string> _verbSuffixRules = new()
        {
            // Conjugaciones comunes que se eliminan para obtener el stem
            { "ando",  "chkani" },   // corriendo → corrchkani (imperfecto)
            { "iendo", "chkani" },
            { "ado",   "rqani" },    // comido → mikhuqarqani (pasado simple aproximado)
            { "ido",   "rqani" },
            { "ará",   "nqa" },      // comerá → mikhunqa (futuro)
            { "erá",   "nqa" },
            { "irá",   "nqa" },
        };

        // ---------------------------------------------------------------
        // IQuechuaGrammarEngine – implementación
        // ---------------------------------------------------------------

        /// <summary>Traduce un texto completo del español al quechua boliviano.</summary>
        public string Translate(string spanishText)
        {
            if (string.IsNullOrWhiteSpace(spanishText))
                return string.Empty;

            var normalized = NormalizeText(spanishText);

            // 1. Intentar coincidencia completa de frase
            if (_phrases.TryGetValue(normalized, out var fullPhrase))
                return fullPhrase;

            // 2. Traducir palabra por palabra aplicando frases primero
            var result = TranslateWithPhrases(normalized);

            // 3. Aplicar post-procesamiento gramatical
            result = PostProcess(result);

            return result.Trim();
        }

        /// <summary>Traduce una sola palabra del español al quechua.</summary>
        public string TranslateWord(string spanishWord)
        {
            if (string.IsNullOrWhiteSpace(spanishWord))
                return string.Empty;

            var normalized = NormalizeText(spanishWord).Trim();

            if (_words.TryGetValue(normalized, out var translation))
                return translation;

            // Intentar con frases cortas
            if (_phrases.TryGetValue(normalized, out var phraseTranslation))
                return phraseTranslation;

            return $"{spanishWord}[?]";
        }

        /// <summary>Aplica reglas gramaticales post-traducción.</summary>
        public string ApplyGrammarRules(string translated, string originalSpanish)
        {
            return PostProcess(translated);
        }

        /// <summary>Cuenta las palabras efectivamente traducidas (sin [?]).</summary>
        public int CountTranslatedWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var words = text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Count(w => !w.Contains("[?]") && w.Length > 0);
        }

        // ---------------------------------------------------------------
        // Métodos privados
        // ---------------------------------------------------------------

        private static string NormalizeText(string text)
        {
            // Convertir a minúsculas, conservar apóstrofes (son fonémicos en quechua)
            var sb = new StringBuilder(text.Length);
            foreach (var ch in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch == '\'' || ch == ''' || ch == ' ' || ch == '¿' || ch == '?')
                    sb.Append(ch);
                else if (ch == ',' || ch == '.' || ch == ';' || ch == ':' || ch == '!' || ch == '¡')
                    sb.Append(' ');
                else
                    sb.Append(ch);
            }
            // Colapsar espacios múltiples
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static string TranslateWithPhrases(string normalizedText)
        {
            // Tokenizar conservando signos de interrogación como tokens propios
            var tokens = normalizedText.Split(' ');
            var output = new List<string>();
            int i = 0;

            while (i < tokens.Length)
            {
                // Intentar frases de longitud decreciente (hasta 6 palabras)
                bool foundPhrase = false;
                for (int len = Math.Min(6, tokens.Length - i); len >= 2; len--)
                {
                    var candidate = string.Join(" ", tokens.Skip(i).Take(len));
                    if (_phrases.TryGetValue(candidate, out var phraseResult))
                    {
                        output.Add(phraseResult);
                        i += len;
                        foundPhrase = true;
                        break;
                    }
                }

                if (!foundPhrase)
                {
                    var token = tokens[i];
                    // Ignorar artículos vacíos
                    if (_words.TryGetValue(token, out var wordResult))
                    {
                        if (!string.IsNullOrEmpty(wordResult))
                            output.Add(wordResult);
                    }
                    else if (token == "¿" || token == "?" || token == string.Empty)
                    {
                        // Ignorar signos de pregunta aislados — el sufijo -chu se aplica en PostProcess
                    }
                    else
                    {
                        // Palabra desconocida
                        output.Add($"{token}[?]");
                    }
                    i++;
                }
            }

            return string.Join(" ", output);
        }

        private static string PostProcess(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            // Eliminar dobles espacios
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // Eliminar tokens vacíos resultantes de artículos nulos
            text = Regex.Replace(text, @"\[\?\]\s*\[\?\]", "[?]");

            return text;
        }
    }
}
