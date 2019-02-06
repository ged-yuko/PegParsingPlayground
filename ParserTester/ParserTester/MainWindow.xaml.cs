using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SyntaxHighlight;
using ParserImpl;
using ParserImpl.Impl;
using ParserImpl.Grammar;

namespace ParserTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region RuleSet[] CurrentRules

        public RuleSet[] CurrentRules
        {
            get { return (RuleSet[])GetValue(CurrentRulesProperty); }
            set { SetValue(CurrentRulesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentRules.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentRulesProperty =
            DependencyProperty.Register("CurrentRules", typeof(RuleSet[]), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region ParsingTreeNodeInfo FilteredTree

        public ParsingTreeNodeInfo FilteredTree
        {
            get { return (ParsingTreeNodeInfo)GetValue(FilteredTreeProperty); }
            set { SetValue(FilteredTreeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilteredTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilteredTreeProperty =
            DependencyProperty.Register("FilteredTree", typeof(ParsingTreeNodeInfo), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region ParsingTreeNodeInfo FullTree

        public ParsingTreeNodeInfo FullTree
        {
            get { return (ParsingTreeNodeInfo)GetValue(FullTreeProperty); }
            set { SetValue(FullTreeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FullTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FullTreeProperty =
            DependencyProperty.Register("FullTree", typeof(ParsingTreeNodeInfo), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region bool EnableGrammarParsingLog

        public bool EnableGrammarParsingLog
        {
            get { return (bool)GetValue(EnableGrammarParsingLogProperty); }
            set { SetValue(EnableGrammarParsingLogProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableGrammarParsingLog.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableGrammarParsingLogProperty =
            DependencyProperty.Register("EnableGrammarParsingLog", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool EnableTextParsingLog

        public bool EnableTextParsingLog
        {
            get { return (bool)GetValue(EnableTextParsingLogProperty); }
            set { SetValue(EnableTextParsingLogProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableTextParsingLog.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableTextParsingLogProperty =
            DependencyProperty.Register("EnableTextParsingLog", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool MaterializeOmitFragments

        public bool MaterializeOmitFragments
        {
            get { return (bool)GetValue(MaterializeOmitFragmentsProperty); }
            set { SetValue(MaterializeOmitFragmentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaterializeOmitFragments.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaterializeOmitFragmentsProperty =
            DependencyProperty.Register("MaterializeOmitFragments", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool RestoreRecursion

        public bool RestoreRecursion
        {
            get { return (bool)GetValue(RestoreRecursionProperty); }
            set { SetValue(RestoreRecursionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RestoreRecursion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RestoreRecursionProperty =
            DependencyProperty.Register("RestoreRecursion", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        #endregion

        IParserFabric _fabric;

        public MainWindow()
        {
            InitializeComponent();

            txtGrammar.SyntaxLexer = new GrammarSyntaxLexer();

            txtText.Text = "9 - (4 + (8 / 2) * 3) + 4 * (2 + 3 / 3 - 1)";
            //txtGrammar.Text = ParserResources.CalcGrammarNew; // ToolResources.CalcGrammar;
            txtGrammar.Text = ToolResources.CalcGrammar;

            this.FullTree = new ParsingTreeNodeInfo(null, null, null, false);

            this.AppendLog(DefinitionGrammar.ParserFabric.GetDebugInfo());
        }

        private void btnApply_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ClearLog();

                 _fabric = null;
                this.CurrentRules = null;

                var grammarResult = DefinitionGrammar.Parse(txtGrammar.Text, this.EnableGrammarParsingLog);
                this.AppendLog(grammarResult.ParsingResult.GetDebugInfo());
                this.AppendLog("Parsed in " + grammarResult.Statistics);

                this.SetTrees(grammarResult.ParsingResult.Tree, new StringSourceTextReader(txtGrammar.Text));
                this.CurrentRules = grammarResult.Rules.Cast<RuleSet>().ToArray();

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                _fabric = Parsers.CreateFabric(this.CurrentRules.First().Name, this.CurrentRules);
                sw.Stop();

                this.AppendLog("Analyzer built in " + sw.Elapsed);
                this.AppendLog();
                this.AppendLog(_fabric.GetDebugInfo());

                this.AppendLog();
            }
            catch (Exception ex)
            {
                this.AppendLog(ex.ToString());
            }
        }

        private void btnParse_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textReader = new StringSourceTextReader(txtText.Text);
                var parser = _fabric.CreateTreeParser();
                parser.EnableLog = this.EnableTextParsingLog;
                parser.MaterializeOmittedFragments = this.MaterializeOmitFragments;
                parser.UseDelayedStates = false;
                parser.RestoreRewritedRecursion = this.RestoreRecursion;
                var result = parser.Parse(textReader);

                this.AppendLog(result.GetDebugInfo());
                this.AppendLog("Parsed in " + parser.ParsingStatistics);

                this.SetTrees(result.Tree, textReader);
            }
            catch (Exception ex)
            {
                this.AppendLog(ex.ToString());
            }
        }

        private void SetTrees(IParsingTreeNode root, ISourceTextReader textReader)
        {
            Func<IParsingTreeGroup, ParsingTreeNodeInfo, IEnumerable<ParsingTreeNodeInfo>> childsAccessor = null;
            childsAccessor = (n, p) => n.Childs.Select(nc => new ParsingTreeNodeInfo(nc, textReader, childsAccessor, true, p)).ToArray();
            this.FullTree = new ParsingTreeNodeInfo(root, textReader, childsAccessor, true);

            Func<IParsingTreeGroup, ParsingTreeNodeInfo, IEnumerable<ParsingTreeNodeInfo>> filteredChildsAccessor = null;
            filteredChildsAccessor = (n, p) => n.GetRuleChilds().Select(nc => new ParsingTreeNodeInfo(nc, textReader, filteredChildsAccessor, false, p)).ToArray();
            this.FilteredTree = new ParsingTreeNodeInfo(root, textReader, filteredChildsAccessor, false);
        }

        private void AppendLog(string str = "")
        {
            txtLog.AppendText(str + Environment.NewLine);
            txtLog.ScrollToEnd();
        }

        private void ClearLog()
        {
            txtLog.Clear();
        }

        private void txtGrammar_OnKeyDown(object sender, KeyEventArgs e)
        {
            lblGrammarCaretPosition.Content = this.FormatCaretPosition(txtGrammar);
        }

        private void txtText_OnKeyDown(object sender, KeyEventArgs e)
        {
            lblTextCaretPosition.Content = this.FormatCaretPosition(txtText);
        }

        private string FormatCaretPosition(TextBox textBox)
        {
            var pos = textBox.CaretIndex;
            var line = textBox.GetLineIndexFromCharacterIndex(pos);
            var loc = new Location(line, pos - textBox.GetCharacterIndexFromLineIndex(line));

            return string.Format("{0} {1}/{2}", loc, pos, textBox.Text.Length);
        }

        //private string FormatCaretPosition(CustomTextEditor textBox)
        //{
        //    var pos = textBox.CaretOffset;
        //    var cloc = textBox.Document.GetLocation(textBox.CaretOffset);
        //    var loc = new Location(cloc.Line, cloc.Column);

        //    return string.Format("{0} {1}/{2}", loc, pos, textBox.Document.TextLength);
        //}
    }

    public class ParsingTreeNodeInfo// : IEnumerable<ParsingTreeNodeInfo>
    {
        public IParsingTreeNode Node { get; private set; }
        public string Text { get; private set; }
        public IEnumerable<ParsingTreeNodeInfo> Childs { get; private set; }
        public ParsingTreeNodeInfo Parent { get; private set; }

        public ParsingTreeNodeInfo(IParsingTreeNode node, ISourceTextReader textReader, Func<IParsingTreeGroup, ParsingTreeNodeInfo, IEnumerable<ParsingTreeNodeInfo>> childsAccessor, bool fullInfo, ParsingTreeNodeInfo parent = null)
        {
            this.Node = node;
            this.Parent = parent;

            var terminal = node as IParsingTreeTerminal;
            if (terminal != null)
            {
                this.Text = "'" + textReader.GetText(terminal.From, terminal.To) + "'";

                if (fullInfo)
                    this.Text = (node.Rule == null ? "<NULL>" : node.Rule.Name) + ": " +
                                (node.Expression == null ? "<NULL>" : node.Expression.ToString()) + "; " + this.Text;
            }

            var group = node as IParsingTreeGroup;
            if (group != null)
            {
                this.Childs = childsAccessor(group, this);

                this.Text = node.Rule == null ? "<NULL>" : node.Rule.Name;
                if (fullInfo)
                    this.Text += ": " + (node.Expression == null ? "<NULL>" : node.Expression.ToString()) + ";";
            }
        }

    }

    //    public class GrammarSyntaxLexer : ISyntaxLexer, IParsingTreeNodeVisitor
    //    {
    //        IParser<ITreeParsingResult> _parser;
    //        List<CodeToken> _newTokens = new List<CodeToken>();

    //        StringSourceTextReader _currReader;

    //        public GrammarSyntaxLexer()
    //        {
    //            _parser = DefinitionGrammar.ParserFabric.CreateTreeParser();
    //        }

    //        public override bool CanShowSuggestionList(int caretPosition)
    //        {
    //            return false;
    //        }

    //        public override void Parse(string text, int caretPosition)
    //        {
    //            _newTokens.Clear();

    //            try
    //            {
    //                _currReader = new StringSourceTextReader(text);
    //                var result = _parser.Parse(_currReader);
    //                if (result.Tree != null)
    //                {
    //                    result.Tree.Visit(this);

    //                    if (_newTokens.Count <= 0)
    //                    {
    //                        var index = _tokens.IndexOf(t => t.Start >= caretPosition || t.End >= caretPosition);
    //                        if (index >= 0)
    //                        {
    //                            var start = index > 0 ? _tokens[index - 1].End : 0;
    //                            _tokens.RemoveRange(index, _tokens.Count - index);

    //                            _tokens.Add(new CodeToken() {
    //                                TokenType = CodeTokenType.Comment,
    //                                Start = start,
    //                                End = text.Length
    //                            });
    //                        }

    //                        return;
    //                    }
    //                }
    //            }
    //            catch
    //            {
    //                _newTokens.Clear();
    //                _newTokens.Add(new CodeToken() {
    //                    TokenType = CodeTokenType.Comment,
    //                    Start = 0,
    //                    End = text.Length
    //                });
    //            }

    //            var tt = _tokens;
    //            _tokens = _newTokens;
    //            _newTokens = tt;
    //        }

    //        public override Key SuggestionListTriggerKey
    //        {
    //            get
    //            {
    //                return Key.OemPeriod;
    //            }
    //        }

    //        LinkedList<string> _rules = new LinkedList<string>();

    //        void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
    //        {
    //            _rules.AddLast(group.Rule == null ? string.Empty : group.Rule.Name);

    //            foreach (var item in group.GetRuleChilds())
    //            {
    //                item.Visit(this);
    //            }

    //            _rules.RemoveLast();
    //        }

    //        string[] _keywords = @"attributesCollection
    //alternatives
    //group
    //check
    //checkNot
    //flag
    //extendable
    //alternative
    //subRules
    //ruleSet
    //ruleSetImport
    //quantor
    //ruleDef
    //attributeUsageArgList
    //qnumber".Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

    //        void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
    //        {
    //            CodeTokenType tt;

    //            var rn = _rules.Last;
    //            if (rn.Value == "name")
    //            {
    //                tt = CodeTokenType.Indentifier;
    //            }
    //            else if (rn.Value == "number")
    //            {
    //                tt = CodeTokenType.Number;
    //            }
    //            else if (rn.Value == "string" || rn.Value == "chars" || rn.Value == "charCode" || rn.Value == "anyChar")
    //            {
    //                tt = CodeTokenType.String;
    //            }
    //            else if (_keywords.Contains(rn.Value) || rn.Previous.Value == "qnumbers")
    //            {
    //                tt = CodeTokenType.Keyword;
    //            }
    //            else
    //            {
    //                tt = CodeTokenType.None;
    //            }

    //            _newTokens.Add(new CodeToken() {
    //                TokenType = tt,
    //                Start = _currReader.GetPosition(terminal.From),
    //                End = _currReader.GetPosition(terminal.To)
    //            });
    //        }
    //    }
}