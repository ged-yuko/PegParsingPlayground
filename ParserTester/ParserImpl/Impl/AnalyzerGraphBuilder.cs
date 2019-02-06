using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    partial class AnalyzerGraphBuilder : IRuleExpressionVisitor
    {
        class ChildParserNodeEntry
        {
            public readonly ChildParserNodeEntry prev;
            public readonly ParserNode node;

            public ChildParserNodeEntry(ChildParserNodeEntry prev, ParserNode node)
            {
                this.prev = prev;
                this.node = node;
            }
        }

        class GraphContext
        {
            public readonly ExplicitRule rule;
            public readonly RuleExpression expression;
            public readonly GraphContext parentContext;
            public readonly int invocationCount;
            public readonly ChildParserNodeEntry childEntries;

            private GraphContext(ExplicitRule rule, RuleExpression expression, GraphContext parentContext, int invocationCount, ChildParserNodeEntry childs)
            {
                this.rule = rule;
                this.expression = expression;
                this.parentContext = parentContext;
                this.invocationCount = invocationCount;
                this.childEntries = childs;
            }

            public ParserNode[] GetChildNodes()
            {
                var list = new List<ParserNode>();

                for (var e = this.childEntries; e != null; e = e.prev)
                    list.Add(e.node);

                list.Reverse();
                return list.ToArray();
            }

            public GraphContext ForChildExpression(RuleExpression childExpression, ExplicitRule rule = null)
            {
                return new GraphContext(rule ?? this.rule, childExpression, this, 0, null);
            }

            public GraphContext ForParentExpression(ParserNode node)
            {
                return new GraphContext(
                    this.parentContext.rule,
                    this.parentContext.expression,
                    this.parentContext.parentContext,
                    this.parentContext.invocationCount + 1,
                    new ChildParserNodeEntry(this.parentContext.childEntries, node)
                );
            }

            public static GraphContext ForRoot(RuleExpression expr)
            {
                return new GraphContext(null, new RuleExpression.Sequence(expr), null, 0, null);
            }
        }

        RuleSet[] _ruleSets;
        string _rootRuleSetName;

        GrammarNavigator _nav;
        GraphContext _currContext;
        List<ParserNode.RecursiveParserNode> _recursiveNodes = new List<ParserNode.RecursiveParserNode>();

        IRuleExpressionVisitor _expressionsVisitor;

        // IndentedWriter _log = new IndentedWriter();

        private AnalyzerGraphBuilder(string rootRuleSetName, RuleSet[] ruleSets)
        {
            _ruleSets = ruleSets;
            _rootRuleSetName = rootRuleSetName;

            _expressionsVisitor = new RuleExpressionLoggingVisitor(this);
            _nav = new GrammarNavigator(ruleSets);
        }

        //private Rule GetRule(string name)
        //{
        //    GrammarNavigator.RuleInfo ruleInfo;
        //    Rule rule;

        //    if (_nav.TryResolveRule(name, out ruleInfo))
        //    {
        //        if (ruleInfo.IsExplicitRule)
        //        {
        //            rule = ruleInfo.Rule;
        //        }
        //        else
        //        {
        //            rule = this.ExpandExtensibleRule(ruleInfo);
        //        }
        //    }
        //    else
        //    {
        //        rule = null;
        //    }

        //    return rule;
        //}

        Dictionary<string, ExplicitRule> _cachedExpandedRulesByPath = new Dictionary<string, ExplicitRule>();

        private ExplicitRule ExpandExtensibleRule(GrammarNavigator.RuleInfo ruleInfo)
        {
            ExplicitRule expandedRule;

            if (!_cachedExpandedRulesByPath.TryGetValue(ruleInfo.NamePath, out expandedRule))
            {
                expandedRule = new ExplicitRule(
                    ruleInfo.ExtRule.First().TokenId,
                    ruleInfo.ExtRule.First().Name,
                    new RuleExpression.Or(
                        ruleInfo.ExtRule.SelectMany(r => r.GetAlternatives())
                                        .OrderBy(ra => ra.Priority)
                                        .Select(ra => new RuleExpression.RuleUsage(ra.Rule.Name))
                                        .ToArray()
                    )
                );

                foreach (var attr in ruleInfo.ExtRule.SelectMany(r => r.GetAttributes()))
                    expandedRule.Add(attr);

                _cachedExpandedRulesByPath.Add(ruleInfo.NamePath, expandedRule);
            }

            return expandedRule;
        }

        public ParserNode OmitGraph { get; private set; }

        private ParserNode BuildGraphImpl()
        {
            if (!_nav.TryEnter(_rootRuleSetName))
                throw new InvalidOperationException(string.Format("Rule set [{0}] not found!", _rootRuleSetName));

            var omitExpression = _nav.GetRuleSetAttributeArgument("OmitPattern");
            this.OmitGraph = omitExpression == null ? null : this.BuildGraphInternal(omitExpression);

            var rootExpression = _nav.GetRuleSetAttributeArgument("RootRule");
            var parserGraph = this.BuildGraphInternal(rootExpression);

            return parserGraph;
        }

        private ParserNode BuildGraphInternal(RuleExpression rootExpression)
        {
            _currContext = GraphContext.ForRoot(rootExpression);

            _recursiveNodes.Clear();
            do
            {
                var ctx = _currContext;

                if (ctx.expression != null)
                {
                    ctx.expression.Visit(_expressionsVisitor);
                }
                else
                {
                    throw new NotImplementedException(); // should never happen
                }

                if (_currContext == ctx)
                    throw new InvalidOperationException(string.Format("Missing transition for expression [{0}] of rule [{1}] was not created!", _currContext.expression, _currContext.rule));
                if (_currContext == null)
                    throw new InvalidOperationException(string.Format("Invalid handler for expression [{0}] of rule [{1}]!", _currContext.expression, _currContext.rule));
                //if (_currContext.prevContext != ctx)
                //    throw new InvalidOperationException(string.Format("Invalid previos context was set from handler for expression [{0}] of rule [{1}] was not created!", _currContext.expression, _currContext.rule));

            } while (_currContext.parentContext != null);

            if (_currContext.childEntries == null ||
                _currContext.childEntries.prev != null)
                throw new InvalidOperationException();

            foreach (var item in _recursiveNodes)
            {
                ParserNode target = null;
                for (var n = item.Parent; n != null; n = n.Parent)
                {
                    if (n.Rule == item.Rule && n.Parent.Rule != item.Rule && n.Parent is ParserNode.RuleCall)
                        // && item.TargetCallExpr.ToString() && )
                    {
                        target = n.Parent;
                        break;
                    }
                }

                if (target == null)
                    throw new InvalidOperationException(string.Format("Filed to find recursive rule call target for rule [{0}] from rule [{1}]!", item.Rule, item.Parent.Rule));

                item.SetTarget(target);
            }

            return _currContext.childEntries.node;
        }

        #region recursion rewrite

        Dictionary<ExplicitRule, RuleExpression> _cachedRewrites = new Dictionary<ExplicitRule, RuleExpression>();

        bool TryRewriteRecursionCalls(ExplicitRule rule, out RuleExpression expr)
        {
            expr = rule.Expression;
            if (_currContext.rule == null)
                return false;

            GrammarNavigator.RuleInfo parentRuleInfo;
            if (!_nav.TryEnterRule(_nav.RuleParentScopeName, out parentRuleInfo))
                return false;

            _nav.Exit();

            if (parentRuleInfo.IsExplicitRule)
                return false;

            var rewrite = parentRuleInfo.ExtRule[0].GetAttributes().Any(a => a.Name == "RewriteRecursion");
            var expand = parentRuleInfo.ExtRule[0].GetAttributes().Any(a => a.Name == "ExpandRecursion");
            if (!(rewrite || expand))
                return false;

            if (!_cachedRewrites.TryGetValue(rule, out expr))
            {
                //expr = RecursionRewriter.RewriteRecursionCalls(rule, this.ExpandExtensibleRule(parentRuleInfo));
                expr = RewriteRecursionCallsImpl(rule, this.ExpandExtensibleRule(parentRuleInfo), rewrite, expand);
                _cachedRewrites.Add(rule, expr);
            }
            else
            {
                System.Diagnostics.Debug.Print("Rewrited recursion reused for rule [" + rule.Name + "]");
            }

            return true;
        }

        RuleExpression RewriteRecursionCallsImpl(ExplicitRule curr, ExplicitRule parent, bool rewrite, bool expand)
        {
            var alternativesExpr = parent.Expression as RuleExpression.Or;
            if (alternativesExpr == null)
                throw new InvalidOperationException();

            var alternatives = alternativesExpr.Childs.Cast<RuleExpression.RuleUsage>().ToArray();

            var seq = curr.Expression as RuleExpression.Sequence;
            if (seq == null)
                return curr.Expression;

            if (seq.Childs.Count != 3)
                return curr.Expression;

            var arg1 = seq.Childs[0] as RuleExpression.RuleUsage;
            var arg2 = seq.Childs[2] as RuleExpression.RuleUsage;

            if (arg1 == null || arg2 == null || arg1.RuleName != parent.Name || arg2.RuleName != parent.Name)
                return curr.Expression;

            RuleExpression expr;
            if (rewrite)
            {
                expr = this.MakeRewrittenRecursiveExpression(alternatives, curr, seq);
            }
            else if (expand)
            {
                expr = this.MakeExpandedRecursiveExpression(alternatives, curr, seq);
            }
            else
            {
                throw new NotImplementedException("");
            }

            return expr;
        }

        RuleExpression MakeRewrittenRecursiveExpression(RuleExpression.RuleUsage[] alternatives, ExplicitRule curr, RuleExpression.Sequence seq)
        {
            var restAltarnatives = alternatives.SkipWhile(e => e.RuleName != curr.Name).Skip(1).ToArray();

            return new RuleExpression.Sequence(
                new RuleExpression.Or(restAltarnatives),
                new RuleExpression.MatchNumber(1, int.MaxValue,
                    new RuleExpression.Sequence(
                        seq.Childs[1],
                        new RuleExpression.Or(restAltarnatives)
                    )
                )
            );
        }

        RuleExpression MakeExpandedRecursiveExpression(RuleExpression.RuleUsage[] alternatives, ExplicitRule curr, RuleExpression.Sequence seq)
        {
            var association = Association.Left;
            if (curr.GetAttributes().Any(a => a.Name.ToLower() == "left"))
                association = Association.Left;
            if (curr.GetAttributes().Any(a => a.Name.ToLower() == "right"))
                association = Association.Right;

            var restAltarnatives = alternatives.SkipWhile(e => e.RuleName != curr.Name).ToArray();

            RuleExpression[] leftAlts, rightAlts;
            switch (association)
            {
                case Association.Left:
                    {
                        leftAlts = restAltarnatives;
                        rightAlts = restAltarnatives.Skip(1).ToArray();
                    } break;
                case Association.Right:
                    {
                        leftAlts = restAltarnatives.Skip(1).ToArray();
                        rightAlts = restAltarnatives;
                    } break;
                default:
                    throw new NotImplementedException("");
            }

            return new RuleExpression.Sequence(
                new RuleExpression.Or(leftAlts),
                seq.Childs[1],
                new RuleExpression.Or(rightAlts)
            );
        }

        #endregion

        #region IRuleExpressionVisitor impl

        void IRuleExpressionVisitor.VisitCheckNot(RuleExpression.CheckNot checkNot)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForChildExpression(checkNot.Child);
            }
            else if (_currContext.invocationCount == 1)
            {
                if (_currContext.childEntries.node == null ||
                    _currContext.childEntries.prev != null)
                    throw new InvalidOperationException();

                _currContext = _currContext.ForParentExpression(new ParserNode.CheckNot(
                    _currContext.rule, _currContext.expression, _currContext.childEntries.node
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitCheck(RuleExpression.Check check)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForChildExpression(check.Child);
            }
            else if (_currContext.invocationCount == 1)
            {
                if (_currContext.childEntries.node == null ||
                    _currContext.childEntries.prev != null)
                    throw new InvalidOperationException();

                _currContext = _currContext.ForParentExpression(new ParserNode.Check(
                    _currContext.rule, _currContext.expression, _currContext.childEntries.node
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitMatchNumber(RuleExpression.MatchNumber matchNumber)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForChildExpression(matchNumber.Child);
            }
            else if (_currContext.invocationCount == 1)
            {
                if (_currContext.childEntries.node == null ||
                    _currContext.childEntries.prev != null)
                    throw new InvalidOperationException();

                _currContext = _currContext.ForParentExpression(new ParserNode.Number(
                    _currContext.rule, _currContext.expression, _currContext.childEntries.node, matchNumber.CountFrom, matchNumber.CountTo
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitSequence(RuleExpression.Sequence sequence)
        {
            if (_currContext.invocationCount < sequence.Childs.Count)
            {
                _currContext = _currContext.ForChildExpression(sequence.Childs[_currContext.invocationCount]);
            }
            else if (_currContext.invocationCount == sequence.Childs.Count)
            {
                var childs = _currContext.GetChildNodes();
                if (childs.Length != sequence.Childs.Count)
                    throw new InvalidOperationException();

                _currContext = _currContext.ForParentExpression(new ParserNode.Sequence(
                    _currContext.rule, _currContext.expression, childs
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitAlternative(RuleExpression.Or alternatives)
        {
            if (_currContext.invocationCount < alternatives.Childs.Count)
            {
                _currContext = _currContext.ForChildExpression(alternatives.Childs[_currContext.invocationCount]);
            }
            else if (_currContext.invocationCount == alternatives.Childs.Count)
            {
                var childs = _currContext.GetChildNodes();
                if (childs.Length != alternatives.Childs.Count)
                    throw new InvalidOperationException();

                _currContext = _currContext.ForParentExpression(new ParserNode.Alternatives(
                    _currContext.rule, _currContext.expression, childs
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void SetRuleParamsInfo(Rule rule, RuleExpression.RuleUsage ruleUsage)
        {
            var ruleArgsInfo = rule.GetEntities().OfType<RuleParameter>().ToArray();
            if (ruleArgsInfo.Length != ruleUsage.Arguments.Count)
                throw new InvalidOperationException(string.Format("Trying to call rule [{0}] with [{1}] parameters!", rule.ToString(), ruleUsage.Arguments.Count));

            for (int i = 0; i < ruleArgsInfo.Length; i++)
                _nav.SetParamExpression(ruleArgsInfo[i], ruleUsage.Arguments[i]);
        }

        private GraphContext FindRuleRecursiveInvocationNode(Rule rule, RuleExpression.RuleUsage ruleUsage)
        {
            GraphContext entryContext = null;
            for (var c = _currContext.parentContext; c != null; c = c.parentContext)
            {
                //var a = ruleUsage.Arguments;
                //var g = c.expression.ToString();
                //var b = c.rule.GetEntities().OfType<RuleExpression.RuleUsage>().ToArray();
                if (c.rule == rule && c.parentContext.rule != rule && ruleUsage.ToString() == c.parentContext.expression.ToString())
                {
                    entryContext = c;
                    break;
                }
            }
            return entryContext;
        }

        Dictionary<string, ParserNode> _cachedMaterializedRewritedRuleEnterByPath = new Dictionary<string, ParserNode>();

        void IRuleExpressionVisitor.VisitRuleUsage(RuleExpression.RuleUsage ruleUsage)
        {
            if (_currContext.invocationCount == 0)
            {
                // _log.WriteLine("entering from [{0}] to [{1}]", _currContext.rule == null ? "<NULL>" : _currContext.rule.Name, ruleUsage.RuleName).Push();

                RuleExpression expr;
                if (_nav.TryEnterParameterContext(ruleUsage.RuleName, out expr))
                {
                    _currContext = _currContext.ForChildExpression(expr, _nav.CurrRuleInfo.Rule);
                }
                else
                {
                    GrammarNavigator.RuleInfo ruleInfo;
                    //if (!_nav.TryResolveRule(ruleUsage.RuleName, out ruleInfo))
                    if (!_nav.TryEnterRule(ruleUsage.RuleName, out ruleInfo))
                        throw new InvalidOperationException(string.Format("Referenced rule [{0}] not found!", ruleUsage.RuleName));

                    var rule = ruleInfo.IsExplicitRule ? ruleInfo.Rule : this.ExpandExtensibleRule(ruleInfo);
                    RuleExpression expression;
                    if (this.TryRewriteRecursionCalls(rule, out expression))
                    {
                        ParserNode recursiveTargetNode;
                        if (_cachedMaterializedRewritedRuleEnterByPath.TryGetValue(ruleInfo.NamePath, out recursiveTargetNode))
                        {
                            _nav.Exit();
                            var node = new ParserNode.RecursiveParserNode(rule, ruleUsage);
                            node.SetTarget(recursiveTargetNode);
                            _currContext = _currContext.ForParentExpression(node);
                            return;
                        }
                    }

                    this.SetRuleParamsInfo(rule, ruleUsage);

                    var entryContext = this.FindRuleRecursiveInvocationNode(rule, ruleUsage);


                    if (entryContext != null)
                    {
                        //_log.WriteLine("recursively exiting from [{0}] to [{1}]", ruleUsage.RuleName, _currContext.rule.Name).Pop();
                        _nav.Exit();

                        var node = new ParserNode.RecursiveParserNode(rule, ruleUsage);
                        _recursiveNodes.Add(node);
                        _currContext = _currContext.ForParentExpression(node);
                    }
                    else
                    {
                        _currContext = _currContext.ForChildExpression(expression, rule);
                    }

                    // _nav.Enter(ruleUsage.RuleName);
                }
            }
            else if (_currContext.invocationCount == 1)
            {
                if (_currContext.childEntries.node == null ||
                    _currContext.childEntries.prev != null)
                    throw new InvalidOperationException();

                var ruleNamePath = _nav.CurrRuleInfo.NamePath;

                //_log.WriteLine("exiting from [{0}] to [{1}]", ruleUsage.RuleName, _currContext.rule.Name).Pop();
                _nav.Exit();

                var node = new ParserNode.RuleCall(
                    _currContext.rule, _currContext.expression, _currContext.childEntries.node
                );

                if (!_cachedMaterializedRewritedRuleEnterByPath.ContainsKey(ruleNamePath))
                    _cachedMaterializedRewritedRuleEnterByPath.Add(ruleNamePath, node);

                _currContext = _currContext.ForParentExpression(node);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitRegex(RuleExpression.Regex regex)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForParentExpression(new ParserNode.FsmParserNode(
                    _currContext.rule, _currContext.expression, FSM.FromPattern(regex.Pattern)
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitChars(RuleExpression.Chars chars)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForParentExpression(new ParserNode.FsmParserNode(
                    _currContext.rule, _currContext.expression, FSM.FromCharsSequence(chars.Content)
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitCharCode(RuleExpression.CharCode charCode)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForParentExpression(new ParserNode.FsmParserNode(
                    _currContext.rule, _currContext.expression, FSM.FromCharsSequence(charCode.Character.ToString())
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IRuleExpressionVisitor.VisitAnyChar(RuleExpression.AnyChar anyChar)
        {
            if (_currContext.invocationCount == 0)
            {
                _currContext = _currContext.ForParentExpression(new ParserNode.FsmParserNode(
                    _currContext.rule, _currContext.expression, FSM.AnyChar()
                ));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        internal static AnalyzerGraphInfo BuildGraph(string rootRuleSetName, RuleSet[] ruleSets)
        {
            var builder = new AnalyzerGraphBuilder(rootRuleSetName, ruleSets);
            var analyzerGraph = builder.BuildGraphImpl();
            return new AnalyzerGraphInfo(analyzerGraph, builder.OmitGraph, ruleSets);
        }
    }

    class AnalyzerGraphInfo
    {
        public ParserNode AnalyzerGraph { get; private set; }
        public ParserNode OmitGraph { get; private set; }
        public RuleSet[] RuleSets { get; private set; }

        public AnalyzerGraphInfo(ParserNode analyzerGraph, ParserNode omitGraph, RuleSet[] ruleSets)
        {
            this.AnalyzerGraph = new ParserNode.Sequence(null, null, analyzerGraph);
            this.OmitGraph = new ParserNode.Sequence(null, null, omitGraph);
            this.RuleSets = ruleSets;
        }
    }
}
