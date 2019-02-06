using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    class RecursionRewritingCompensator : IParsingTreeNodeVisitor
    {
        class RuleAlternativeInfo
        {
            public Rule RootRule { get; private set; }
            public Rule Rule { get; private set; }
            public Association Association { get; private set; }
            public RuleAlternativesInfo AlternativesInfo { get; private set; }
            public ExtensibleRuleAlternativeInfo AlternativeInfo { get; private set; }

            public RuleAlternativeInfo(ExtensibleRuleAlternativeInfo info, Rule rootRule, RuleAlternativesInfo alts)
            {
                var association = Association.Left;
                if (info.Rule.GetAttributes().Any(a => a.Name.ToLower() == "left"))
                    association = Association.Left;
                if (info.Rule.GetAttributes().Any(a => a.Name.ToLower() == "right"))
                    association = Association.Right;

                this.Association = association;
                this.Rule = info.Rule;
                this.RootRule = rootRule;
                this.AlternativesInfo = alts;
                this.AlternativeInfo = info;
            }
        }

        class RuleAlternativesInfo
        {
            List<RuleAlternativeInfo> _alternatives = new List<RuleAlternativeInfo>();

            public IEnumerable<RuleAlternativeInfo> Alternatives { get { return _alternatives; } }

            public RuleAlternativesInfo()
            {
            }

            public void AddInfo(ExtensibleRule rule)
            {
                foreach (var item in rule.GetAlternatives())
                {
                    if (IsAlternativeNeeded(item.Rule as ExplicitRule, rule))
                        _alternatives.Add(new RuleAlternativeInfo(item, rule, this));
                }

                _alternatives.Sort((a, b) => a.AlternativeInfo.Priority.CompareTo(b.AlternativeInfo.Priority));
            }

            bool IsAlternativeNeeded(ExplicitRule curr, ExtensibleRule parent)
            {
                if (curr == null)
                    return false;

                var seq = curr.Expression as RuleExpression.Sequence;
                if (seq == null)
                    return false;

                if (seq.Childs.Count != 3)
                    return false;

                var arg1 = seq.Childs[0] as RuleExpression.RuleUsage;
                var arg2 = seq.Childs[2] as RuleExpression.RuleUsage;

                if (arg1 == null || arg2 == null || arg1.RuleName != parent.Name || arg2.RuleName != parent.Name)
                    return false;

                return true;
            }
        }

        class RuleSetsRecursionInfoCollector : IRuleSetVisitor
        {
            Dictionary<string, RuleAlternativesInfo> _info = new Dictionary<string, RuleAlternativesInfo>();

            public RuleSetsRecursionInfoCollector(RuleSet[] sets)
            {
                foreach (var item in sets)
                    item.Visit(this);
            }

            public RuleSetAlternativesInfo GetInfo()
            {
                var result = new Dictionary<Rule, RuleAlternativeInfo>();

                foreach (var item in _info)
                {
                    foreach (var alternative in item.Value.Alternatives)
                    {
                        result.Add(alternative.Rule, alternative);
                    }
                }

                return new RuleSetAlternativesInfo(result);
            }

            #region IRuleSetVisitor implementation

            public void VisitExtensibleRule(ExtensibleRule extensibleRule)
            {
                var rewrite = extensibleRule.GetAttributes().Any(a => a.Name == "RewriteRecursion");

                if (rewrite)
                {
                    RuleAlternativesInfo info;
                    if (!_info.TryGetValue(extensibleRule.Name, out info))
                        _info.Add(extensibleRule.Name, info = new RuleAlternativesInfo());

                    info.AddInfo(extensibleRule);
                }
            }

            public void VisitRuleSet(RuleSet ruleSet)
            {
                foreach (var item in ruleSet)
                    item.Visit(this);
            }

            public void VisitAttribute(EntityAttribute entityAttribute) { }
            public void VisitRuleSetImport(RuleSetImport ruleSetImport) { }
            public void VisitRuleParameter(RuleParameter ruleParameter) { }

            public void VisitExplicitRule(ExplicitRule explicitRule)
            {
                foreach (var item in explicitRule)
                    item.Visit(this);
            }

            #endregion
        }

        class RuleSetAlternativesInfo
        {
            private Dictionary<Rule, RuleAlternativeInfo> _info;

            public RuleSetAlternativesInfo(Dictionary<Rule, RuleAlternativeInfo> info)
            {
                this._info = info;
            }

            public bool TryGetInfo(Rule rule, out RuleAlternativeInfo info)
            {
                return _info.TryGetValue(rule, out info);
            }
        }

        ParsingTreeNode.Group _node;
        ParsingTreeNode.ParsedNode _currChild = null;

        RuleSetAlternativesInfo _alternativesInfo;

        private RecursionRewritingCompensator(ParsingTreeNode.Group node, RuleSetAlternativesInfo alternatives)
        {
            _node = node;
            _alternativesInfo = alternatives;
        }

        public ParsingTreeNode.ParsedNode Recreate(ParsingTreeNode.ParsedNode next)
        {
            foreach (var item in ((IParsingTreeGroup)_node).Childs.Reverse())
                item.Visit(this);

            return new ParsingTreeNode.Group(_node.GrammarNode, _node.Location, next, _currChild);
        }

        ParsingTreeNode.ParsedNode RestoreRecursion(ParsingTreeNode.ParsedNode next, RuleAlternativeInfo info)
        {
            //if (_node.GrammarNode is ParserNode.RecursiveParserNode)
            //{
            //    if (_node.ChildsCount > 1)
            //        throw new NotImplementedException("wtf");

            //    _node = _node.Child as ParsingTreeNode.Group;
            //    _node = _node.Child as ParsingTreeNode.Group;
            //}

            var childs = ((IParsingTreeGroup)_node).GetRuleChilds().Reverse().ToArray();
            if (childs.Length < 3 || childs.Length + 1 % 2 == 0)
            {
                if (childs.Length == 1)
                {
                    _currChild = null;
                    childs[0].Visit(this);
                    return _currChild;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            if (info.Association == Association.Right)
            {

                _currChild = null;
                childs[0].Visit(this);
                for (int i = 1; i < childs.Length; i += 2)
                {
                    childs[i].Visit(this);
                    childs[i + 1].Visit(this);
                    this.WrapChilds(info);
                    _currChild = new ParsingTreeNode.Group(_node.GrammarNode, _node.Location, null, _currChild);
                }
            }
            else if (info.Association == Association.Left)
            {
                var stack = new Stack<ParsingTreeNode.ParsedNode>();

                _currChild = null;
                for (int i = 0; i < childs.Length - 3; i += 2)
                {
                    childs[i].Visit(this);
                    childs[i + 1].Visit(this);
                    stack.Push(_currChild);
                    _currChild = null;
                }
                childs[childs.Length - 3].Visit(this);
                childs[childs.Length - 2].Visit(this);
                childs[childs.Length - 1].Visit(this);

                //this.WrapChilds(info);
                //_currChild = new ParsingTreeNode.Group(_node.GrammarNode, _node.Location, null, _currChild);
                while (stack.Count > 0)
                {
                    this.WrapChilds(info);
                    _currChild = new ParsingTreeNode.Group(_node.GrammarNode, _node.Location, stack.Pop(), _currChild);
                }
                this.WrapChilds(info);
                _currChild = new ParsingTreeNode.Group(_node.GrammarNode, _node.Location, next, _currChild);
            }
            else
            {
                throw new NotImplementedException("");
            }

            return _currChild;
        }

        private void WrapChilds(RuleAlternativeInfo info)
        {
            var lst = new List<ParsingTreeNode.ParsedNode>();
            for (var n = _currChild; n != null; n = n.Next)
                lst.Add(n);

            var callExpressions = info.AlternativesInfo.Alternatives.Select(a => new { alternative = a, expr = new RuleExpression.RuleUsage(a.Rule.Name) }).ToArray();
            var parentAlternatives = new ParserNode.Alternatives(
                info.RootRule,
                new RuleExpression.Or(callExpressions.Select(a => a.expr).ToArray()),
                callExpressions.Select(a => new ParserNode.RuleCall(a.alternative.Rule, a.expr, new ParserNode.FsmParserNode(null, null, null))).ToArray()
            );
            var parentCall = new ParserNode.RuleCall(_currChild.Rule, new RuleExpression.RuleUsage(info.RootRule.Name), parentAlternatives);

            ParsingTreeNode.ParsedNode next = null;
            for (int i = lst.Count - 1; i >= 0; i--)
            {
                var curr = lst[i];
                if (i % 2 != 0)
                {
                    next = new ParsingTreeNode.Group(curr.GrammarNode.Parent, curr.Location, next,
                        curr.UpdateNext(null)
                    );
                }
                else
                {
                    next = new ParsingTreeNode.Group(parentCall, curr.Location, next,
                        new ParsingTreeNode.Group(parentAlternatives, curr.Location, null,
                            new ParsingTreeNode.Group(new ParserNode.RuleCall(parentAlternatives.Rule, new RuleExpression.RuleUsage(curr.Rule.Name), new ParserNode.FsmParserNode(null, null, null)), curr.Location, null,
                                curr.UpdateNext(null)
                            )
                        )
                    );
                }

            }

            _currChild = next;
        }


        #region IParsingTreeNodeVisitor implementation

        void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
        {
            var compensator = new RecursionRewritingCompensator((ParsingTreeNode.Group)group, _alternativesInfo);

            RuleAlternativeInfo info;
            if (group.Rule != null && _alternativesInfo.TryGetInfo(group.Rule, out info))
            {
                _currChild = compensator.RestoreRecursion(_currChild, info);
            }
            else
            {
                _currChild = compensator.Recreate(_currChild);
            }
        }

        void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
        {
            var t = (ParsingTreeNode.Terminal)terminal;
            _currChild = new ParsingTreeNode.Terminal(t.GrammarNode, t.Location, _currChild, t.From, t.To, t.Content);
        }

        #endregion

        public static ParsingTreeNode RestoreRecursion(ParsingTreeNode.ParsedNode tree, RuleSet[] ruleSets)
        {
            var infoCollector = new RuleSetsRecursionInfoCollector(ruleSets);
            var info = infoCollector.GetInfo();
            return new RecursionRewritingCompensator((ParsingTreeNode.Group)tree, info).Recreate(null);
        }
    }
}
