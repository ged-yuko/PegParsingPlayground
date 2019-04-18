using ParserImpl;
using ParserImpl.Grammar;
using SyntaxHighlight;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;


namespace ParserTester
{
    public class GrammarSyntaxLexer : ISyntaxLexer, IParsingTreeNodeVisitor
    {
        private IParser<ITreeParsingResult> _parser;
        private StringSourceTextReader _currReader;
        private List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> _lines = new List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>>();
        private ITreeParsingResult _result;
        private Stopwatch _sw = new Stopwatch();
        private LinkedList<string> _rules = new LinkedList<string>();

        private readonly string[] _keywords = new string[] {
            "attributesCollection",
            "alternatives",
            "group",
            "check",
            "checkNot",
            "flag",
            "extendable",
            "alternative",
            "subRules",
            "ruleSet",
            "ruleSetImport",
            "quantor",
            "ruleDef",
            "attributeUsageArgList",
            "qnumber",
        };

        public override Key SuggestionListTriggerKey => Key.Escape;
        public override bool CanShowSuggestionList(int caret_position)
        {
            return false;
        }

        public GrammarSyntaxLexer()
        {
            _parser = DefinitionGrammar.ParserFabric.CreateTreeParser();
        }

        public override void Parse(string text, int caret_position)
        {
            _tokens.Clear();
            Parse(new StringSourceTextReader(text));
        }

        public List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> ReParse(StringSourceTextReader reader, Location limit)
        {
            ITreeParsingResult result = null;
            try
            {
                _currReader = reader;

                _sw.Reset();
                _sw.Start();
                result = _parser.ReParse(_currReader, _result, limit);
                _sw.Stop();
                Debug.WriteLine(_sw.Elapsed);
            }
            catch
            {
            }

            return SaveParseResult(result);
        }

        public List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> Parse(StringSourceTextReader reader)
        {
            ITreeParsingResult result = null;

            try
            {
                _currReader = reader;

                _sw.Reset();
                _sw.Start();
                result = _parser.Parse(_currReader);
                _sw.Stop();
                Debug.WriteLine(_sw.Elapsed);

                return SaveParseResult(result);
            }
            catch
            {
            }
            return SaveParseResult(result);
        }

        public void VisitGroup(IParsingTreeGroup group)
        {
            _rules.AddLast(group?.Rule?.Name ?? string.Empty);

            foreach (IParsingTreeNode item in group.GetRuleChilds())
            {
                item.Visit(this);
            }

            _rules.RemoveLast();
        }

        public void VisitTerminal(IParsingTreeTerminal terminal)
        {
            CodeTokenType tt;

            switch (_rules.Last.Value)
            {
                case "name":
                    tt = CodeTokenType.Indentifier;
                    break;

                case "number":
                    tt = CodeTokenType.Number;
                    break;

                case "string":
                case "chars":
                case "charCode":
                case "antChar":
                    tt = CodeTokenType.String;
                    break;

                default:
                    if (_keywords.Contains(_rules.Last.Value) || _rules.Last.Previous.Value == "qnumbers")
                    {
                        tt = CodeTokenType.Keyword;
                    }
                    else
                    {
                        tt = CodeTokenType.None;
                    }
                    break;
            }
            AddToken(terminal, tt);
        }

        private List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> SaveParseResult(ITreeParsingResult parsingResult)
        {
            if (parsingResult?.Tree != null)
            {
                _lines.Clear();
                parsingResult.Tree.Visit(this);
                _result = parsingResult;

                return _lines;
            }

            return new List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>>();
        }

        private static readonly Dictionary<CodeTokenType, HighlightingColor> _brushes = new Dictionary<CodeTokenType, HighlightingColor>() {
            { CodeTokenType.Keyword, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.Blue) } },
            { CodeTokenType.Number, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.DarkCyan) } },
            { CodeTokenType.Comment, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.Black) } },
            { CodeTokenType.String, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.Red) } },
            { CodeTokenType.Indentifier, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.Gray) } },
            { CodeTokenType.None, new HighlightingColor() { Foreground = new SimpleHighlightingBrush(Colors.Green) } }
        };

        private void AddToken(IParsingTreeTerminal term, CodeTokenType type)
        {
            var from = term.From;
            while (_lines.Count <= from.Line)
            {
                _lines.Add(new List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>());
            }

            _lines[from.Line].Add(new KeyValuePair<IParsingTreeTerminal, HighlightingColor>(term, _brushes[type]));

            _tokens.Add(new CodeToken()
            {
                Start = _currReader.GetPosition(term.From),
                End = _currReader.GetPosition(term.To),
                TokenType = type
            });
        }

        public class HighlightingColor
        {
            public SimpleHighlightingBrush Foreground { get; set; }
        }

        public class SimpleHighlightingBrush
        {
            public Color Color { get; private set; }

            public SimpleHighlightingBrush(Color color)
            {
                Color = color;
            }
        }
    }
}
