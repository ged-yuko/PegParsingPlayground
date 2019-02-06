using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    abstract class ParserState<TResult>
        where TResult : IParsingResult
    {
        public ParserNode CurrNode { get; private set; }
        public ParserNode PrevNode { get; private set; }

        private ISourceTextReaderHolder _readerHolder;
        private ISourceTextReader _reader;
        protected ISourceTextReader Reader { get { return _reader ?? (_reader = _readerHolder.GetReader()); } }

        public Location Location { get { return this.Reader.Location; } }

        public bool? LastTerminalFailed { get; private set; }

        protected readonly IndentedWriter _log;
        public string GetLogSnapshot() { return _log == null ? null : _log.GetContentAsString(); }

        public abstract int ChildsCount { get; }
        protected abstract ParserNode.RecursiveParserNode RecursionNode { get; }

        public bool ResultReconstructed { get; protected set; }
        public bool InsideOmittedFragment { get; set; }

        internal ParserState(IndentedWriter log, ParserNode curr, ParserNode prev, bool? lastTerminalFailed, ISourceTextReader reader, ISourceTextReaderHolder holder = null)
        {
            this.CurrNode = curr;
            this.PrevNode = prev;
            this.ResultReconstructed = false;

            _log = log;

            this.LastTerminalFailed = lastTerminalFailed;

            _reader = reader;
            _readerHolder = holder;
        }

        public TResult MakeResult(bool restoreRecursion, RuleSet[] ruleSets)
        {
            return this.MakeResultImpl(restoreRecursion, ruleSets);
        }

        protected abstract TResult MakeResultImpl(bool restoreRecursion, RuleSet[] ruleSets);

        public bool TryRunFsm(FSM fsm, out string text)
        {
            return fsm.TryRun(this.Reader, out text);
        }

        public ParserState<TResult> EnterNode(ParserNode nextNode, bool offTree = false)
        {
            if (nextNode.Parent != this.CurrNode && !(this.CurrNode is ParserNode.RecursiveParserNode) && !offTree)
                throw new InvalidOperationException();

            if (_log != null)
            {
                _log.Push().WriteLine("EnterNode @{2} {0} --> {1}", this.CurrNode, nextNode, this.Reader.Location).Push();

                if (this.CurrNode is ParserNode.RecursiveParserNode)
                    _log.WriteLine("Enter to recursion");
            }

            return this.EnterNodeImpl(nextNode);
        }

        protected abstract ParserState<TResult> EnterNodeImpl(ParserNode nextNode);

        public ParserState<TResult> ExitNode(bool? lastTerminalFailed = null)
        {
            if (!this.LastTerminalFailed.HasValue && !lastTerminalFailed.HasValue)
                throw new InvalidOperationException();

            var terminalFailed = lastTerminalFailed.HasValue ? lastTerminalFailed : this.LastTerminalFailed;

            var recursionCallNode = this.RecursionNode;
            var isExitFromRecursiveCall = recursionCallNode != null && recursionCallNode.Target == this.CurrNode;
            var nextNode = isExitFromRecursiveCall ? recursionCallNode : this.CurrNode.Parent;

            if (_log != null)
            {
                if (isExitFromRecursiveCall)
                    _log.WriteLine("Exit from recursion");

                _log.Pop().WriteLine("ExitNode @{3} {0} {1} --> {2}",
                    terminalFailed.HasValue ? ("(" + terminalFailed + ")") : string.Empty,
                    this.CurrNode, nextNode, this.Reader.Location
                ).Pop();
            }

            return this.ExitNodeImpl(terminalFailed, nextNode);
        }

        protected abstract ParserState<TResult> ExitNodeImpl(bool? terminalFailed, ParserNode nextNode);

        public ParserState<TResult> TerminalParsed(Location from, Location to, string text) //, bool omitFragment)
        {
            if (_log != null)
                _log.WriteLine("TerminalParsed '{0}'@{1} {2}", text, this.Reader.GetPosition(), this.CurrNode);
            //_log.WriteLine("TerminalParsed '{0}'@{1} {2} {3}", text, this.Reader.GetPosition(), this.CurrNode, omitFragment ? "Omit" : string.Empty);

            return this.TerminalParsedImpl(from, to, text);
        }

        protected abstract ParserState<TResult> TerminalParsedImpl(Location from, Location to, string text);

        public ParserState<TResult> TerminalFailed()
        {
            if (_log != null)
                _log.WriteLine("TerminalFailed @{0} {1}", this.Reader.GetPosition(), this.CurrNode);

            return this.TerminalFailedImpl();
        }

        protected abstract ParserState<TResult> TerminalFailedImpl();

        public ParserState<TResult> Clone()
        {
            return this.CloneImpl();
        }

        protected abstract ParserState<TResult> CloneImpl();
    }

    class ParserTreeState : ParserState<ParserTreeState.Result>
    {
        public sealed class Result : IParsingResult, ITreeParsingResult
        {
            public bool AllTextParsed { get; private set; }
            public bool Successed { get; private set; }

            public IParsingTreeNode Tree { get; private set; }
            public IParsingTreeNode[] Trees { get; private set; }

            IndentedWriter _log;

            internal Result(bool allTextParsed, bool treeReconstructed, IndentedWriter log, params IParsingTreeNode[] trees)
            {
                _log = log;
                this.AllTextParsed = allTextParsed;
                this.Successed = allTextParsed || treeReconstructed;
                this.Tree = trees.Length == 1 ? trees.First() : null;
                this.Trees = trees.Length == 1 ? null : trees;
            }

            public string GetDebugInfo()
            {
                return _log == null ? null : _log.GetContentAsString();
            }
        }

        public override int ChildsCount { get { return this.TreeNode.ChildsCount; } }
        protected override ParserNode.RecursiveParserNode RecursionNode { get { return this.TreeNode.Parent.GrammarNode as ParserNode.RecursiveParserNode; } }

        public ParsingTreeNode.TemporaryGroup TreeNode { get; private set; }

        protected ParserTreeState(IndentedWriter log, ParserNode prev, ParsingTreeNode.TemporaryGroup treeNode, bool? lastTerminalFailed, ISourceTextReader reader, ISourceTextReaderHolder holder = null)
            : base(log, treeNode.GrammarNode, prev, lastTerminalFailed, reader, holder)
        {
            this.TreeNode = treeNode;
        }

        private ParserState<Result> MayBeReconstruct(ParserNode nextNode)
        {
            ParsingTreeNode.TemporaryGroup reconstructedTree;
            if (this.TreeNode.TryReconstruct(nextNode, this.Location, out reconstructedTree))
            {
                if (_log != null)
                {
                    while (_log.Level > 2)
                        _log.Pop();

                    _log.WriteLine("Tree reconstruction completed!");
                }

                return new ParserTreeState(
                  _log,
                  this.CurrNode,
                  reconstructedTree,
                  null,
                  this.Reader
                ) {
                    ResultReconstructed = true,
                    InsideOmittedFragment = false
                };
            }
            else
            {
                return null;
            }
        }

        protected override ParserState<Result> EnterNodeImpl(ParserNode nextNode)
        {
            return this.MayBeReconstruct(nextNode) ?? new ParserTreeState(
                _log,
                this.CurrNode,
                this.TreeNode.CreateChildGroup(nextNode, this.Reader.Location),
                null,
                this.Reader
            ) {
                InsideOmittedFragment = this.InsideOmittedFragment
            };
        }

        protected override ParserState<Result> ExitNodeImpl(bool? terminalFailed, ParserNode nextNode)
        {
            // var nextNode = this.TreeNode.Parent.GrammarNode;

            // fallback
            if (terminalFailed == true)
            {
                var cloc = this.Reader.Location;
                var oloc = this.TreeNode.Location;

                if (cloc != oloc)
                {
                    if (!this.Reader.MoveTo(oloc))
                        throw new InvalidOperationException();
                }
            }

            // var removeCurr = this.TreeNode.ChildsCount == 0 || terminalFailed.Value;
            var removeCurr = terminalFailed.Value;

            if (!this.TreeNode.HasChilds && this.InsideOmittedFragment)
                removeCurr = true;

            //var treeNode = removeCurr ? this.TreeNode.RemoveCurrent() : (
            //    this.CurrNode is ParserNode.RecursiveParserNode
            //            ? this.TreeNode.TakeOffCurrent()
            //            : this.TreeNode.ExitChild()
            //);

            var treeNode = removeCurr ? this.TreeNode.RemoveCurrent() : this.TreeNode.ExitChild();

            return new ParserTreeState(
                _log,
                this.CurrNode,
                treeNode,
                terminalFailed,
                this.Reader
            ) {
                InsideOmittedFragment = this.InsideOmittedFragment
            };
        }


        protected override ParserState<Result> TerminalParsedImpl(Location from, Location to, string text)
        {
            return this.MayBeReconstruct(this.CurrNode) ?? new ParserTreeState(
                _log,
                this.CurrNode,
                this.TreeNode.CreateChildTerminal(this.CurrNode, from, from, to, text).ExitChild(),
                false,
                this.Reader
            ) {
                InsideOmittedFragment = this.InsideOmittedFragment
            };
        }

        protected override ParserState<Result> TerminalFailedImpl()
        {
            return new ParserTreeState(
                _log,
                this.CurrNode,
                this.TreeNode,
                true,
                this.Reader
            ) {
                InsideOmittedFragment = this.InsideOmittedFragment
            };
        }

        //public static ParserTreeState ForStart(ParserNode root, ISourceTextReader reader, bool enableLog)
        //{
        //    IndentedWriter w;
        //    if (enableLog)
        //    {
        //        w = new IndentedWriter("  ");
        //        w.Push().WriteLine("Start @{0} {1}", reader.GetPosition(), root).Push();
        //    }
        //    else
        //    {
        //        w = null;
        //    }

        //    if (root.Parent != null)
        //        throw new ArgumentException();

        //    return new ParserTreeState(
        //        w,
        //        null,
        //        ParsingTreeNode.CreateRootGroup(root, reader.Location),
        //        null,
        //        reader
        //    );
        //}

        public static ParserState<Result> ForStart(ParserNode grammarRoot, ParserNode omitRoot, ISourceTextReader reader, IParsingTreeNode oldRoot, Location limit, bool enableLog)
        {
            IndentedWriter w;
            if (enableLog)
            {
                w = new IndentedWriter("  ");

                if (oldRoot != null)
                    w.WriteLine("Incrementally");

                w.Push().WriteLine("Start @{0} {1}", reader.GetPosition(), grammarRoot).Push();
            }
            else
            {
                w = null;
            }

            if (oldRoot != null)
            {
                ParserNode prevGrammarNode = null;
                bool insideOmittedFragment;
                var treeNode = ParsingTreeNode.CreateRootGroup(oldRoot, omitRoot, reader.Location, limit, out prevGrammarNode, out insideOmittedFragment);
                if (!reader.MoveTo(treeNode.Location))
                    throw new NotImplementedException("");

                if (w != null)
                {
                    w.WriteLine("Reconstructing log indentation...");

                    var t = treeNode;
                    var path = new List<ParsingTreeNode.TemporaryGroup>();
                    while (t != null)
                    {
                        path.Add(t);
                        t = t.Parent;
                    }

                    path.Reverse();

                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        w.Push().WriteLine("EnterNode @{2} {0} --> {1}", path[i].GrammarNode, path[i + 1].GrammarNode, path[i].Location).Push();

                        if (path[i].GrammarNode is ParserNode.RecursiveParserNode)
                            w.WriteLine("Enter to recursion");
                    }

                    w.WriteLine("...Identation reconstructed.");
                }

                ParserState<Result> state = new ParserTreeState(
                    w,
                    prevGrammarNode,
                    treeNode,
                    null,
                    reader
                );

                state.InsideOmittedFragment = insideOmittedFragment;

                if (state.LastTerminalFailed == false)
                    state = state.ExitNode();
                else
                    System.Diagnostics.Debug.Print(string.Empty);

                return state;
            }
            else
            {
                return new ParserTreeState(
                    w,
                    null,
                    ParsingTreeNode.CreateRootGroup(grammarRoot, reader.Location),
                    null,
                    reader
                );
            }
        }

        protected override Result MakeResultImpl(bool restoreRecursion, RuleSet[] ruleSets)
        {
            var tree = this.TreeNode.Recreate().Recreate(null);
            var resultTree = restoreRecursion ? RecursionRewritingCompensator.RestoreRecursion(tree, ruleSets) : tree;

            return new Result(
                this.Reader.Location == this.Reader.TextEndLocation,
                this.ResultReconstructed,
                _log,
                resultTree
            );
        }

        protected override ParserState<Result> CloneImpl()
        {
            return new ParserTreeState(
                _log == null ? null : _log.Clone(),
                this.PrevNode,
                this.TreeNode,
                this.LastTerminalFailed,
                null,
                this.Reader.Clone()
            ) {
                InsideOmittedFragment = this.InsideOmittedFragment,
                ResultReconstructed = this.ResultReconstructed
            };
        }
    }

    class LinearParserState : ParserState<LinearParserState.Result>
    {
        public sealed class Result : IParsingResult, ILinearParsingResult
        {
            public bool AllTextParsed { get; private set; }
            public bool Successed { get; private set; }

            //public IParsingTreeNode Tree { get; private set; }
            //public IParsingTreeNode[] Trees { get; private set; }
            //public string[] PrintedTrees { get; private set; }
            //public string[] PrintedSteps { get; private set; }

            public IParserStep[] Steps { get; private set; }

            IndentedWriter _log;

            internal Result(bool allTextParsed, IndentedWriter log, params ParserStep[] steps)
            {
                this.Steps = steps;

                _log = log;

                //var trees = steps.Select(s => ParsingTreeMaterializer.Materialize(s)).ToArray();

                //this.AllTextParsed = allTextParsed;
                //this.Tree = trees.Length == 1 ? trees.First() : null;
                //this.Trees = trees.Length == 1 ? null : trees;
                //this.PrintedTrees = trees.Length == 1 ? null : trees.Select(t => new ParseringTreePrinter().Collect(t)).ToArray();
                //this.PrintedSteps = steps.Select(s => ParserStepsPrinter.Collect(s)).ToArray();
            }

            public string GetDebugInfo()
            {
                return _log == null ? null : _log.GetContentAsString();
            }
        }


        public class StatefullStackNode
        {
            private StatefullStackNode Prev { get; set; }
            public int ChildsCount { get; private set; }
            public ParserNode.RecursiveParserNode RecursionSource { get; private set; }

            private StatefullStackNode(StatefullStackNode prev, int childsCount, ParserNode.RecursiveParserNode source)
            {
                this.Prev = prev;
                this.ChildsCount = childsCount;
                this.RecursionSource = source;
            }

            public StatefullStackNode StartChildCount()
            {
                return new StatefullStackNode(this, 0, null);
            }

            public StatefullStackNode CountNextChild()
            {
                if (this.RecursionSource != null || this.ChildsCount < 0)
                    throw new InvalidOperationException();

                return new StatefullStackNode(this.Prev, this.ChildsCount + 1, null);
            }

            public StatefullStackNode EndChildCount()
            {
                if (this.RecursionSource != null || this.ChildsCount < 0)
                    throw new InvalidOperationException();

                return this.Prev;
            }

            public StatefullStackNode StartRecursionRuleCall(ParserNode.RecursiveParserNode source)
            {
                if (source == null)
                    throw new ArgumentNullException();

                return new StatefullStackNode(this, -1, source);
            }

            public StatefullStackNode EndRecursionRuleCall()
            {
                if (this.RecursionSource == null || this.ChildsCount >= 0)
                    throw new InvalidOperationException();

                return this.Prev;
            }

            public static StatefullStackNode ForRoot()
            {
                return new StatefullStackNode(null, -1, null);
            }

            public override string ToString()
            {
                return "{" + (this.RecursionSource == null ? this.ChildsCount.ToString() : this.RecursionSource.ToString()) + "}" +
                    (this.Prev == null ? string.Empty : this.Prev.ToString());
            }
        }

        public override int ChildsCount { get { return this.Stack.ChildsCount; } }
        protected override ParserNode.RecursiveParserNode RecursionNode { get { return this.Stack.RecursionSource; } }

        public ParserStep LastStep { get; private set; }

        public StatefullStackNode Stack { get; private set; }

        private LinearParserState(IndentedWriter w, ParserNode currNode, ParserNode prevNode, ParserStep lastStep, StatefullStackNode stack, bool? lastTerminalFailed, ISourceTextReader reader, ISourceTextReaderHolder holder = null)
            : base(w, currNode, prevNode, lastTerminalFailed, reader, holder)
        {
            this.LastStep = lastStep;
            this.Stack = stack;

            if (_log != null)
                _log.WriteLine(this.Stack.ToString());
        }

        private void PushChildIndex()
        {
            if (_log != null)
                _log.WriteLine("PushChildIndex 0");

            this.Stack = this.Stack.StartChildCount();
        }

        private int IncChildIndex()
        {
            if (this.Stack.RecursionSource != null)
                throw new InvalidOperationException();

            this.Stack = this.Stack.CountNextChild();
            if (_log != null)
                _log.WriteLine("IncChildIndex {0}", this.Stack.ChildsCount);

            return this.Stack.ChildsCount;
        }

        private int PopChildIndex()
        {
            if (this.Stack.RecursionSource != null)
                throw new InvalidOperationException();

            var value = this.Stack.ChildsCount;
            this.Stack = this.Stack.EndChildCount();
            if (_log != null)
                _log.WriteLine("PopChildIndex {0}", value);

            return value;
        }

        protected override ParserState<Result> EnterNodeImpl(ParserNode nextNode)
        {
            var recursionCallNode = this.CurrNode as ParserNode.RecursiveParserNode;

            if (nextNode is ParserNode.Number)
            {
                if (this.CurrNode == nextNode.Parent)
                {
                    this.PushChildIndex();
                }
                else if (this.CurrNode.Parent == nextNode)
                {
                    this.IncChildIndex();
                }
            }

            return new LinearParserState(
                _log,
                nextNode,
                this.CurrNode,
                this.LastStep.CreateEnterNode(nextNode, this.Location),
                recursionCallNode == null ? this.Stack : this.Stack.StartRecursionRuleCall(recursionCallNode),
                null,
                this.Reader
            );
        }

        protected override ParserState<Result> ExitNodeImpl(bool? terminalFailed, ParserNode nextNode)
        {
            if (this.CurrNode is ParserNode.Number && nextNode == this.CurrNode.Parent)
            {
                this.PopChildIndex();
            }

            // fallback
            if (terminalFailed == true)
            {
                var cloc = this.Reader.Location;
                var oloc = this.LastStep.Location;
                if (cloc.Line != oloc.Line)
                    throw new NotImplementedException();

                if (cloc.Column != oloc.Column)
                {
                    this.Reader.Move(oloc.Column - cloc.Column);
                }
            }

            return new LinearParserState(
                _log,
                nextNode,
                this.CurrNode,
                this.LastStep is ParserStep.EnterNode && this.LastStep.Node == this.CurrNode && this.LastStep.PrevStep.Node == nextNode
                    ? this.LastStep.PrevStep
                    : this.LastStep.CreateExitNode(this.CurrNode, this.Location),
                nextNode is ParserNode.RecursiveParserNode ? this.Stack.EndRecursionRuleCall() : this.Stack,
                terminalFailed,
                this.Reader
            );
        }

        protected override ParserState<Result> TerminalParsedImpl(Location from, Location to, string text)
        {
            return new LinearParserState(
                _log,
                this.CurrNode,
                this.PrevNode,
                this.LastStep.CreateTerminal(this.CurrNode, from, to, text),
                this.Stack,
                false,
                this.Reader
            );
        }

        protected override ParserState<Result> TerminalFailedImpl()
        {
            return new LinearParserState(
                _log,
                this.CurrNode,
                this.PrevNode,
                this.LastStep,
                this.Stack,
                true,
                this.Reader
            );
        }

        public static ParserState<Result> ForStart(ParserNode root, ISourceTextReader reader, bool enableLog)
        {
            IndentedWriter w;
            if (enableLog)
            {
                w = new IndentedWriter("  ");
                w.Push().WriteLine("Start @{0} {1}", reader.GetPosition(), root).Push();
            }
            else
            {
                w = null;
            }

            if (root.Parent != null)
                throw new ArgumentException();

            return new LinearParserState(
                w,
                root,
                null,
                new ParserStep.EnterNode(null, root, reader.Location),
                StatefullStackNode.ForRoot(),
                null,
                reader
            );
        }

        protected override Result MakeResultImpl(bool restoreRecursion, RuleSet[] sets)
        {
            return new Result(this.Reader.Location == this.Reader.TextEndLocation, _log, this.LastStep);
        }

        protected override ParserState<Result> CloneImpl()
        {
            return new LinearParserState(
                _log == null ? null : _log.Clone(),
                this.CurrNode,
                this.PrevNode,
                this.LastStep,
                this.Stack,
                this.LastTerminalFailed,
                null,
                this.Reader.Clone()
            );
        }
    }

}
