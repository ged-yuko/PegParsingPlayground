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
    //    class FakeHighlightingDefinition : IHighlightingDefinition
    //    {
    //        public string Name { get { throw new NotImplementedException(""); } }

    //        public HighlightingRuleSet MainRuleSet { get { throw new NotImplementedException(); } }
    //        public IDictionary<string, string> Properties { get { throw new NotImplementedException(); } }
    //        public IEnumerable<HighlightingColor> NamedHighlightingColors { get { throw new NotImplementedException(); } }

    //        public FakeHighlightingDefinition()
    //        {
    //        }

    //        public HighlightingRuleSet GetNamedRuleSet(string name) { throw new NotImplementedException(); }
    //        public HighlightingColor GetNamedColor(string name) { throw new NotImplementedException(); }
    //    }


    //    public class CustomTextEditor : TextEditor
    //    {
    //        public CustomTextEditor()
    //        {
    //            this.SyntaxHighlighting = new FakeHighlightingDefinition();
    //        }

    //        protected override IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
    //        {
    //            return new HighlightingColorizer(new CustomHighlighter(this));
    //        }
    //    }

    //    class CustomHighlighter : IHighlighter
    //    {
    //        static readonly HighlightingColor _defaultColor = new HighlightingColor() {
    //            Foreground = new SimpleHighlightingBrush(Colors.Green)
    //        };

    //        static CustomHighlighter()
    //        {
    //            _defaultColor.Freeze();
    //        }

    //        public event HighlightingStateChangedEventHandler HighlightingStateChanged;

    //        TextEditor _editor;

    //        public IDocument Document { get { return _editor.Document; } }
    //        public HighlightingColor DefaultTextColor { get { return _defaultColor; } }

    //        List<List<KeyValuePair<IParsingTreeTerminal, HighlightingColor>>> _parsedLines;
    //        List<HighlightedLine> _highlightedLines = new List<HighlightedLine>();
    //        GrammarSyntaxLexer _lexer = new GrammarSyntaxLexer();
    //        StringSourceTextReader _reader;

    //        public CustomHighlighter(TextEditor editor)
    //        {
    //            _editor = editor;
    //            //_editor.TextChanged += (sender, ea) => this.ReParse();
    //            this.ReParse();
    //        }

    //        private void RaizeHighlightingStateChanged(int fromLineNumber, int toLineNumber)
    //        {
    //            var handler = this.HighlightingStateChanged;

    //            if (handler != null)
    //                handler(fromLineNumber, toLineNumber);
    //        }

    //        public IEnumerable<HighlightingColor> GetColorStack(int lineNumber)
    //        {
    //            // not used? O_o
    //            throw new NotImplementedException("");
    //        }

    //        public HighlightedLine HighlightLine(int lineNumber)
    //        {
    //            lineNumber--;
    //            if (lineNumber >= _parsedLines.Count)
    //                return new HighlightedLine(_editor.Document, _editor.Document.Lines[lineNumber]); ;

    //            return _highlightedLines[lineNumber];
    //        }

    //        public void UpdateHighlightingState(int lineNumber)
    //        {
    //            System.Diagnostics.Debug.Print(lineNumber.ToString());

    //            if (_parsedLines.Count == 0)
    //            {
    //                this.ReParse();
    //            }
    //            else
    //            {
    //                _reader = new StringSourceTextReader(_editor.Text);
    //                _reader.MoveTo(new Location(lineNumber, 0));

    //                var view = _editor.TextArea.TextView;
    //                var lastVisibleLine = view.GetDocumentLineByVisualTop(view.ScrollOffset.Y + view.ActualHeight).LineNumber;
    //                _parsedLines = _lexer.ReParse(_reader, new Location(lastVisibleLine + 1, 0));
    //                this.ReHighlight();
    //            }
    //        }

    //        private void ReParse()
    //        {
    //            _reader = new StringSourceTextReader(_editor.Text);
    //            _parsedLines = _lexer.Parse(_reader);
    //            this.ReHighlight();
    //        }

    //        private void ReHighlight()
    //        {
    //            _highlightedLines.Clear();
    //            for (int i = 0; i < _parsedLines.Count; i++)
    //            {
    //                var line = _parsedLines[i];
    //                var hline = new HighlightedLine(_editor.Document, _editor.Document.Lines[i]);

    //                foreach (var item in line)
    //                {
    //                    var from = _reader.GetPosition(item.Key.From);
    //                    var to = _reader.GetPosition(item.Key.To);

    //                    hline.Sections.Add(new HighlightedSection() {
    //                        Offset = from,
    //                        Length = to - from,
    //                        Color = item.Value
    //                    });
    //                }

    //                _highlightedLines.Add(hline);
    //            }

    //            this.RaizeHighlightingStateChanged(1, _editor.LineCount);
    //        }

    //        public void BeginHighlighting()
    //        {
    //        }

    //        public void EndHighlighting()
    //        {
    //        }

    //        public HighlightingColor GetNamedColor(string name)
    //        {
    //            return null;
    //        }

    //        public void Dispose()
    //        {
    //            _editor = null;
    //        }
    //    }

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
