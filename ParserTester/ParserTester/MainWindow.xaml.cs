using ParserImpl;
using ParserImpl.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            get => (RuleSet[])GetValue(CurrentRulesProperty);
            set => SetValue(CurrentRulesProperty, value);
        }

        // Using a DependencyProperty as the backing store for CurrentRules.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentRulesProperty =
            DependencyProperty.Register("CurrentRules", typeof(RuleSet[]), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region ParsingTreeNodeInfo FilteredTree

        public ParsingTreeNodeInfo FilteredTree
        {
            get => (ParsingTreeNodeInfo)GetValue(FilteredTreeProperty);
            set => SetValue(FilteredTreeProperty, value);
        }

        // Using a DependencyProperty as the backing store for FilteredTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilteredTreeProperty =
            DependencyProperty.Register("FilteredTree", typeof(ParsingTreeNodeInfo), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region ParsingTreeNodeInfo FullTree

        public ParsingTreeNodeInfo FullTree
        {
            get => (ParsingTreeNodeInfo)GetValue(FullTreeProperty);
            set => SetValue(FullTreeProperty, value);
        }

        // Using a DependencyProperty as the backing store for FullTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FullTreeProperty =
            DependencyProperty.Register("FullTree", typeof(ParsingTreeNodeInfo), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region bool EnableGrammarParsingLog

        public bool EnableGrammarParsingLog
        {
            get => (bool)GetValue(EnableGrammarParsingLogProperty);
            set => SetValue(EnableGrammarParsingLogProperty, value);
        }

        // Using a DependencyProperty as the backing store for EnableGrammarParsingLog.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableGrammarParsingLogProperty =
            DependencyProperty.Register("EnableGrammarParsingLog", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool EnableTextParsingLog

        public bool EnableTextParsingLog
        {
            get => (bool)GetValue(EnableTextParsingLogProperty);
            set => SetValue(EnableTextParsingLogProperty, value);
        }

        // Using a DependencyProperty as the backing store for EnableTextParsingLog.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableTextParsingLogProperty =
            DependencyProperty.Register("EnableTextParsingLog", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool MaterializeOmitFragments

        public bool MaterializeOmitFragments
        {
            get => (bool)GetValue(MaterializeOmitFragmentsProperty);
            set => SetValue(MaterializeOmitFragmentsProperty, value);
        }

        // Using a DependencyProperty as the backing store for MaterializeOmitFragments.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaterializeOmitFragmentsProperty =
            DependencyProperty.Register("MaterializeOmitFragments", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion

        #region bool RestoreRecursion

        public bool RestoreRecursion
        {
            get => (bool)GetValue(RestoreRecursionProperty);
            set => SetValue(RestoreRecursionProperty, value);
        }

        // Using a DependencyProperty as the backing store for RestoreRecursion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RestoreRecursionProperty =
            DependencyProperty.Register("RestoreRecursion", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        #endregion

        private IParserFabric _fabric;

        private IEvolutionalParser _evolutionalParser;

        public MainWindow()
        {
            InitializeComponent();

            txtGrammar.SyntaxLexer = new GrammarSyntaxLexer();

            txtText.Text = "9 - (4 + (8 / 2) * 3) + 4 * (2 + 3 / 3 - 1)";
            //txtGrammar.Text = ParserResources.CalcGrammarNew; // ToolResources.CalcGrammar;
            txtGrammar.Text = ToolResources.CalcGrammar;

            FullTree = new ParsingTreeNodeInfo(null, null, null, false);

            _evolutionalParser = new EvolutionalParser();

            AppendLog(DefinitionGrammar.ParserFabric.GetDebugInfo());
        }

        private void btnApply_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearLog();

                _fabric = null;
                CurrentRules = null;

                RuleSetParsingResult grammarResult = DefinitionGrammar.Parse(txtGrammar.Text, EnableGrammarParsingLog);
                AppendLog(grammarResult.ParsingResult.GetDebugInfo());
                AppendLog("Parsed in " + grammarResult.Statistics);

                SetTrees(grammarResult.ParsingResult.Tree, new StringSourceTextReader(txtGrammar.Text));
                CurrentRules = grammarResult.Rules.Cast<RuleSet>().ToArray();

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                _fabric = Parsers.CreateFabric(CurrentRules.First().Name, CurrentRules);
                sw.Stop();

                AppendLog("Analyzer built in " + sw.Elapsed);
                AppendLog();
                AppendLog(_fabric.GetDebugInfo());

                AppendLog();
            }
            catch (Exception ex)
            {
                AppendLog(ex.ToString());
            }
        }

        private void btnParse_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textReader = new StringSourceTextReader(txtText.Text);
                IParser<ITreeParsingResult> parser = _fabric.CreateTreeParser();
                parser.EnableLog = EnableTextParsingLog;
                parser.MaterializeOmittedFragments = MaterializeOmitFragments;
                parser.UseDelayedStates = false;
                parser.RestoreRewritedRecursion = RestoreRecursion;
                ITreeParsingResult result = parser.Parse(textReader);

                AppendLog(result.GetDebugInfo());
                AppendLog("Parsed in " + parser.ParsingStatistics);

                SetTrees(result.Tree, textReader);
            }
            catch (Exception ex)
            {
                AppendLog(ex.ToString());
            }
        }

        private void SetTrees(IParsingTreeNode root, ISourceTextReader textReader)
        {
            IEnumerable<ParsingTreeNodeInfo> childsAccessor(IParsingTreeGroup n, ParsingTreeNodeInfo p) => n.Childs.Select(nc => new ParsingTreeNodeInfo(nc, textReader, childsAccessor, true, p)).ToArray();
            FullTree = new ParsingTreeNodeInfo(root, textReader, childsAccessor, true);

            Func<IParsingTreeGroup, ParsingTreeNodeInfo, IEnumerable<ParsingTreeNodeInfo>> filteredChildsAccessor = null;
            filteredChildsAccessor = (n, p) => n.GetRuleChilds().Select(nc => new ParsingTreeNodeInfo(nc, textReader, filteredChildsAccessor, false, p)).ToArray();
            FilteredTree = new ParsingTreeNodeInfo(root, textReader, filteredChildsAccessor, false);
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

        private void GrammarChanged(object sender, KeyEventArgs e)
        {
            lblGrammarCaretPosition.Content = FormatCaretPosition(txtGrammar);
        }

        private void InputTextChanged(object sender, KeyEventArgs e)
        {
            lblTextCaretPosition.Content = FormatCaretPosition(txtText);

            if (CurrentRules != null)
            {
                try
                {
                    var textReader = new StringSourceTextReader(txtText.Text);
                    IParsingTreeNode parsingResult = _evolutionalParser.Parse(FullTree.Node, textReader, CurrentRules, GetTextChangesLocation(txtText), false);
                    SetTrees(parsingResult, textReader);
                }
                catch (Exception ex) {
                    AppendLog(ex.ToString());
                }
            }
        }

        private Location GetTextChangesLocation(TextBox textBox)
        {
            int pos = textBox.CaretIndex;
            int line = textBox.GetLineIndexFromCharacterIndex(pos);
            return new Location(line, pos - textBox.GetCharacterIndexFromLineIndex(line));
        }

        private string FormatCaretPosition(TextBox textBox)
        {
            int line = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
            Location loc = GetTextChangesLocation(textBox);

            return string.Format("{0} {1}/{2}", loc, textBox.CaretIndex, textBox.Text.Length);
        }
    }

    public class ParsingTreeNodeInfo
    {
        public IParsingTreeNode Node { get; private set; }
        public string Text { get; private set; }
        public IEnumerable<ParsingTreeNodeInfo> Childs { get; private set; }
        public ParsingTreeNodeInfo Parent { get; private set; }

        public ParsingTreeNodeInfo(IParsingTreeNode node, ISourceTextReader textReader, Func<IParsingTreeGroup, ParsingTreeNodeInfo, IEnumerable<ParsingTreeNodeInfo>> childsAccessor, bool fullInfo, ParsingTreeNodeInfo parent = null)
        {
            Node = node;
            Parent = parent;

            if (node is IParsingTreeTerminal terminal)
            {
                Text = "'" + textReader.GetText(terminal.From, terminal.To) + "'";

                if (fullInfo)
                {
                    Text = (node.Rule == null ? "<NULL>" : node.Rule.Name) + ": " +
                                (node.Expression == null ? "<NULL>" : node.Expression.ToString()) + "; " + Text;
                }
            }

            if (node is IParsingTreeGroup group)
            {
                Childs = childsAccessor(group, this);

                Text = node.Rule == null ? "<NULL>" : node.Rule.Name;
                if (fullInfo)
                {
                    Text += ": " + (node.Expression == null ? "<NULL>" : node.Expression.ToString()) + ";";
                }
            }
        }
    }
}