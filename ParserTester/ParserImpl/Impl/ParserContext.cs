using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ParserImpl.Impl
{
    internal class ParserContext<TResult> : IParserNodeVisitor
        where TResult : class, IParsingResult
    {
        private readonly ParserInitialStateFabric<TResult> _initialStateFabric;
        private readonly Parser<TResult> _owner;
        private readonly ParserNode _grammarRoot;
        private readonly ParserNode _omitRoot;
        private readonly ISourceTextReader _source;
        private readonly TResult _oldResult;
        private readonly Location _limit;
        private readonly bool _materializeOmittedFragments = false;
        private readonly bool _useDelayedStates = false;
        private Stack<ParserState<TResult>> _delayedStates = new Stack<ParserState<TResult>>();
        private ParserState<TResult> _currState;
        private List<ParserState<TResult>> _lastResults = new List<ParserState<TResult>>();
        private IParserNodeVisitor _parserVisitor;

        public ParserContext(Parser<TResult> owner, AnalyzerGraphInfo analyzerInfo, ISourceTextReader source, ParserInitialStateFabric<TResult> initialStateFabric, TResult oldResult, Location limit)
        {
            _owner = owner;
            _grammarRoot = analyzerInfo.AnalyzerGraph;
            _omitRoot = analyzerInfo.OmitGraph;
            _source = source;
            _initialStateFabric = initialStateFabric;

            _currState = null;
            _oldResult = oldResult;
            _limit = limit;
            _materializeOmittedFragments = owner.MaterializeOmittedFragments;
            _useDelayedStates = owner.UseDelayedStates;
            _parserVisitor = this; // new ParserNodeLoggingVisitor(this);
        }

        private string _fname = @"v:\out\" + DateTime.Now.ToString("yyyy-mm-dd@HH-MM-ss") + "_";
        private int _fcount = 0;

        public TResult Parse()
        {
            _delayedStates.Push(_initialStateFabric.CreateInitialState(_grammarRoot, _omitRoot, _source.Clone().GetReader(), _oldResult, _limit, _owner.EnableLog));

            while (_delayedStates.Count > 0)
            {
            nextDelayedBranch:
                _currState = _delayedStates.Pop();

                // GC.Collect();

                Location loc = _currState.Location;
                int locCount = 0;

            parsingLoop:
                ParserNode node, nextNode;
                do
                {
                    node = _currState.CurrNode;

                    // move to next state with respect to grammar node
                    node.Visit(this);

                    if (_currState.ResultReconstructed)
                        break;

                    nextNode = _currState.CurrNode;
                    if (nextNode == node)
                    {
                        // no transition were made
                        throw new InvalidOperationException(string.Format("Invalid handler for node [{0}] of rule [{1}]!", node, node.Rule));
                    }

                    if (_currState.Location == loc) // recursion guard
                    {
                        locCount++;
                        if (locCount > 10000)
                        {
                            System.Diagnostics.Debug.Print("DROPPED");
                            goto nextDelayedBranch;
                        }
                    }
                    else locCount = 0;

                    //if (_useDelayedStates && _currState.LastTerminalFailed == true && _currState.Location < _source.TextEndLocation)
                    //{
                    //    if (_owner.EnableLog)
                    //    {
                    //        System.IO.File.WriteAllText(_fname + (_fcount++).ToString().PadLeft(4, '0') + ".txt", _currState.GetLogSnapshot());
                    //    }


                    //    goto nextDelayedBranch;
                    //}

                    loc = _currState.Location;
                } while (nextNode.Parent != null || nextNode == _omitRoot);

                // System.Diagnostics.Debug.Print(_currState.GetLogSnapshot());

                if (!_currState.ResultReconstructed && _currState.Location > _source.Location)
                {
                    if (_currState.InsideOmittedFragment)
                    {
                        _currState.InsideOmittedFragment = false;
                    }
                    else if (_omitRoot != null)
                    {
                        _currState.InsideOmittedFragment = true;
                        _currState = _currState.EnterNode(_omitRoot, true);
                        goto parsingLoop;
                    }
                }

                if (_lastResults.Count == 0)
                {
                    _lastResults.Add(_currState);
                }
                else
                {
                    //var lastLocation = _lastResults.First().LastStep.GetLocation();
                    //var currLocation = _currState.LastStep.GetLocation();

                    var lastLocation = _lastResults.First().Location;
                    var currLocation = _currState.Location;

                    if (!(currLocation < _source.TextEndLocation))
                        return _currState.MakeResult(_owner.RestoreRewritedRecursion, _owner.RuleSets);

                    if (lastLocation < currLocation)
                    {
                        _lastResults.Clear(); // (drop prev result)
                        _lastResults.Add(_currState);
                    }
                    else if (lastLocation > currLocation)
                    {
                        // do nothing (drop current result)
                    }
                    else
                    {
                        _lastResults.Add(_currState);
                    }
                }
            }

            // var allTextParsed = _lastResults.First().Reader.Character == char.MaxValue;
            // var allTextParsed = !(_lastResults.First().Location < _limit);

            return _lastResults.First().MakeResult(_owner.RestoreRewritedRecursion, _owner.RuleSets);
        }

        #region IParserNodeVisitor implementation

        void IParserNodeVisitor.VisitFsm(ParserNode.FsmParserNode fsmParserNode)
        {
            if (_omitRoot != null)
            {
                if (_currState.PrevNode == fsmParserNode.Parent)
                {
                    if (_currState.InsideOmittedFragment)
                    {
                        this.HandleTerminalFsm(fsmParserNode, _materializeOmittedFragments);
                    }
                    else
                    {
                        _currState.InsideOmittedFragment = true;
                        _currState = _currState.EnterNode(_omitRoot, true);
                    }
                }
                else if (_currState.PrevNode == _omitRoot)
                {
                    if (_currState.InsideOmittedFragment)
                    {
                        _currState.InsideOmittedFragment = false;
                        this.HandleTerminalFsm(fsmParserNode, true);
                    }
                    else
                    {
                        throw new InvalidOperationException("");
                    }
                }
                else
                {
                    throw new InvalidOperationException("");
                }
            }
            else
            {
                if (_currState.PrevNode == fsmParserNode.Parent)
                {
                    this.HandleTerminalFsm(fsmParserNode, true);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void HandleTerminalFsm(ParserNode.FsmParserNode fsmParserNode, bool materialize)
        {
            string text;
            var from = _currState.Location;
            if (_currState.TryRunFsm(fsmParserNode.Fsm, out text))
            {
                var to = _currState.Location;

                if (materialize)
                    _currState = _currState.TerminalParsed(from, to, text);
                //_currState = _currState.TerminalParsed(from, to, text, _omitting);

                _currState = _currState.ExitNode(false);
            }
            else
            {
                if (materialize)
                    _currState = _currState.TerminalFailed();

                _currState = _currState.ExitNode(true);
            }
        }

        /*
        
        private void SkipOmitPattern()
        {
            if (_omitRoot != null)
            {
                string spaces;
                _currState.TryRunFsm(((ParserNode.FsmParserNode)((ParserNode.Sequence)_omitRoot).Childs[0]).Fsm, out spaces);
            }
        }

        void IParserNodeVisitor.VisitFsm(ParserNode.FsmParserNode fsmParserNode)
        {
            if (_currState.PrevNode == fsmParserNode.Parent)
            {
                this.SkipOmitPattern();

                string text;
                var from = _currState.Location;
                if (_currState.TryRunFsm(fsmParserNode.Fsm, out text))
                {
                    var to = _currState.Location;
                    _currState = _currState.TerminalParsed(from, to, text).ExitNode();

                    this.SkipOmitPattern();
                }
                else
                {
                    _currState = _currState.TerminalFailed().ExitNode();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        
        */

        void IParserNodeVisitor.VisitRecursive(ParserNode.RecursiveParserNode recursiveParserNode)
        {
            if (_currState.PrevNode == recursiveParserNode.Parent)
            {
                _currState = _currState.EnterNode(recursiveParserNode.Target);
            }
            else if (_currState.PrevNode == recursiveParserNode.Target)
            {
                _currState = _currState.ExitNode();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IParserNodeVisitor.VisitChek(ParserNode.Check check)
        {
            if (_currState.PrevNode == check.Parent)
            {
                _currState = _currState.EnterNode(check.Child, true);
            }
            else if (_currState.PrevNode == check.Child)
            {
                _currState = _currState.ExitNode(_currState.LastTerminalFailed.Value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IParserNodeVisitor.VisitCheckNot(ParserNode.CheckNot checkNot)
        {
            if (_currState.PrevNode == checkNot.Parent)
            {
                _currState = _currState.EnterNode(checkNot.Child, true);
            }
            else if (_currState.PrevNode == checkNot.Child)
            {
                _currState = _currState.ExitNode(!_currState.LastTerminalFailed.Value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IParserNodeVisitor.VisitRuleCall(ParserNode.RuleCall ruleCall)
        {
            if (_currState.PrevNode == ruleCall.Parent || (_currState.PrevNode is ParserNode.RecursiveParserNode && (_currState.PrevNode as ParserNode.RecursiveParserNode).Target == ruleCall))
            {
                _currState = _currState.EnterNode(ruleCall.Child);
            }
            else if (_currState.PrevNode == ruleCall.Child)
            {
                _currState = _currState.ExitNode();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IParserNodeVisitor.VisitNumber(ParserNode.Number number)
        {
            if (_currState.PrevNode == number.Parent)
            {
                _currState = _currState.EnterNode(number.Child);
            }
            else if (_currState.PrevNode == number.Child)
            {
                // var handledChildIndex = _currState.Stack.ChildsCount;
                // var handledChildIndex = _currState.TreeNode.ChildsCount;
                var handledChildIndex = _currState.ChildsCount;

                if (_currState.LastTerminalFailed.Value)
                {
                    // _currState.PopChildIndex();
                    if (handledChildIndex < number.CountFrom)
                    {
                        _currState = _currState.ExitNode(true);
                    }
                    else
                    {
                        _currState = _currState.ExitNode(false);
                    }
                }
                else
                {
                    if (handledChildIndex + 1 < number.CountTo)
                    {
                        // _currState.IncChildIndex();
                        _currState = _currState.EnterNode(number.Child);
                    }
                    else
                    {
                        // _currState.PopChildIndex();
                        _currState = _currState.ExitNode(false);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IParserNodeVisitor.VisitSequence(ParserNode.Sequence sequence)
        {
            if (_currState.PrevNode == sequence.Parent ||
                (sequence.Parent == null && _currState.PrevNode.Parent != sequence)) // null for graph root
            {
                _currState = _currState.EnterNode(sequence.Childs[0]);
            }
            else if (_currState.PrevNode.Parent == sequence)
            {
                var index = _currState.PrevNode.IndexInParentList + 1;

                if (index < sequence.Childs.Count && !_currState.LastTerminalFailed.Value)
                {
                    _currState = _currState.EnterNode(sequence.Childs[index]);
                }
                else
                {
                    _currState = _currState.ExitNode();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        void IParserNodeVisitor.VisitAlternatives(ParserNode.Alternatives alternatives)
        {
            if (_useDelayedStates)
                this.VisitAlternativesWDelayed(alternatives);
            else
                this.VisitAlternativesWSequential(alternatives);
        }

        // TODO: [Portable.Parser.Impl] parallel alternatives scan

        private ParserState<TResult> _rrr;

        private void VisitAlternativesWDelayed(ParserNode.Alternatives alternatives)
        {
            if (_currState.PrevNode == alternatives.Parent)
            {
                for (int i = alternatives.Childs.Count - 1; i > 0; i--)
                {
                    _delayedStates.Push(_currState.Clone().EnterNode(alternatives.Childs[i]));
                }

                _currState = _currState.EnterNode(alternatives.Childs[0]);

                //for (int i = 0; i < alternatives.Childs.Count - 1; i++)
                //{
                //    _delayedStates.Push(_currState.Clone().EnterNode(alternatives.Childs[i]));
                //}

                //_currState = _currState.EnterNode(alternatives.Childs[alternatives.Childs.Count - 1]);
            }
            else if (_currState.PrevNode.Parent == alternatives)
            {
                if (_currState.LastTerminalFailed == true)
                {
                    if (_owner.EnableLog)
                    {
                        System.IO.File.WriteAllText(_fname + (_fcount++).ToString().PadLeft(4, '0') + ".txt", _currState.GetLogSnapshot());
                    }

                    if (_rrr == null) _rrr = _currState;
                    else if (_rrr.Location < _currState.Location)
                        _rrr = _currState;

                    _currState = _delayedStates.Pop();
                }
                else
                {
                    _currState = _currState.ExitNode();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void VisitAlternativesWSequential(ParserNode.Alternatives alternatives)
        {
            if (_currState.PrevNode == alternatives.Parent)
            {
                _currState = _currState.EnterNode(alternatives.Childs[0]);
            }
            else if (_currState.PrevNode.Parent == alternatives)
            {
                if (_currState.LastTerminalFailed == false)
                {
                    _currState = _currState.ExitNode();
                }
                else
                {
                    var nextIndex = _currState.PrevNode.IndexInParentList + 1;
                    if (nextIndex < alternatives.Childs.Count)
                    {
                        _currState = _currState.EnterNode(alternatives.Childs[nextIndex]);
                    }
                    else
                    {
                        _currState = _currState.ExitNode(true);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        #endregion
    }
}
