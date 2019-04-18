//using ICSharpCode.AvalonEdit;
//using ICSharpCode.AvalonEdit.CodeCompletion;
//using ICSharpCode.AvalonEdit.Document;
//using ICSharpCode.AvalonEdit.Folding;
//using ICSharpCode.AvalonEdit.Highlighting;
//using ICSharpCode.AvalonEdit.Highlighting.Xshd;
//using ICSharpCode.AvalonEdit.Rendering;
//using Microsoft.Win32;

using ParserImpl;
using ParserImpl.Grammar;
using ParserImpl.Impl;
using SyntaxHighlight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;


namespace ParserTester
{
    public class GrammarSyntaxLexer : SyntaxHighlight.ISyntaxLexer, IParsingTreeNodeVisitor
    {
        private IParser<ITreeParsingResult> _parser;
        private StringSourceTextReader _currReader;
        private static readonly List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> _empty = new List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>>();
        private List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> _lines = new List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>>();
        private ITreeParsingResult _result;
        private System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();

        public override Key SuggestionListTriggerKey
        {
            get { return Key.Escape; }
        }
        public override bool CanShowSuggestionList(int caret_position)
        {
            return false;
        }

        public GrammarSyntaxLexer()
        {
            _parser = DefinitionGrammar.ParserFabric.CreateTreeParser();
            // _parser.EnableLog = true;
            //base.Tokens = _tokens;
        }

        public override void Parse(string text, int caret_position)
        {
            _tokens.Clear();
            this.Parse(new StringSourceTextReader(text));
        }

        public List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> ReParse(StringSourceTextReader reader, Location limit)
        {
            try
            {
                _currReader = reader;

                _sw.Reset();
                _sw.Start();
                var result = _parser.ReParse(_currReader, _result, limit);
                _sw.Stop();
                System.Diagnostics.Debug.WriteLine(_sw.Elapsed);

                if (result.Tree != null)
                {
                    _lines.Clear();
                    result.Tree.Visit(this);
                    _result = result;

                    // System.IO.File.WriteAllText(@"v:\reparsed.txt", RulesParsingTreePrinter.CollectTree(result.Tree));
                    return _lines;
                }
            }
            catch
            {
            }

            return _empty;
        }

        public List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> Parse(StringSourceTextReader reader)
        {
            try
            {
                _currReader = reader;

                _sw.Reset();
                _sw.Start();
                var result = _parser.Parse(_currReader);
                _sw.Stop();
                System.Diagnostics.Debug.WriteLine(_sw.Elapsed);

                if (result.Tree != null)
                {
                    _lines.Clear();
                    result.Tree.Visit(this);
                    _result = result;

                    // System.IO.File.WriteAllText(@"v:\fullparsed.txt", RulesParsingTreePrinter.CollectTree(result.Tree));

                    return _lines;
                }
            }
            catch
            {
            }

            return _empty;
        }

        private LinkedList<string> _rules = new LinkedList<string>();

        void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
        {
            _rules.AddLast(group.Rule == null ? string.Empty : group.Rule.Name);

            foreach (var item in group.GetRuleChilds())
            {
                item.Visit(this);
            }

            _rules.RemoveLast();
        }

        private string[] _keywords = @"attributesCollection
alternatives
group
check
checkNot
flag
extendable
alternative
subRules
ruleSet
ruleSetImport
quantor
ruleDef
attributeUsageArgList
qnumber".Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
        {
            CodeTokenType tt;

            var rn = _rules.Last;
            if (rn.Value == "name")
            {
                tt = CodeTokenType.Indentifier;
            }
            else if (rn.Value == "number")
            {
                tt = CodeTokenType.Number;
            }
            else if (rn.Value == "string" || rn.Value == "chars" || rn.Value == "charCode" || rn.Value == "anyChar")
            {
                tt = CodeTokenType.String;
            }
            else if (_keywords.Contains(rn.Value) || rn.Previous.Value == "qnumbers")
            {
                tt = CodeTokenType.Keyword;
            }
            else
            {
                tt = CodeTokenType.None;
            }

            this.AddToken(terminal, tt);
        }

        //public enum CodeTokenType
        //{
        //    Keyword = 0,
        //    String = 1,
        //    Number = 2,
        //    Comment = 3,
        //    Indentifier = 4,
        //    None = 5,
        //}

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
                _lines.Add(new List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>());

            _lines[from.Line].Add(new KeyValuePair<IParsingTreeTerminal, HighlightingColor>(term, _brushes[type]));

            _tokens.Add(new SyntaxHighlight.CodeToken() {
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
                this.Color = color;
            }
        }
    }
}
