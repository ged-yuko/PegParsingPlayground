using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    public interface IParserNodeVisitor
    {
        void VisitFsm(ParserNode.FsmParserNode fsmParserNode);

        void VisitRecursive(ParserNode.RecursiveParserNode recursiveParserNode);

        void VisitChek(ParserNode.Check check);

        void VisitCheckNot(ParserNode.CheckNot checkNot);

        void VisitRuleCall(ParserNode.RuleCall ruleCall);

        void VisitNumber(ParserNode.Number number);

        void VisitSequence(ParserNode.Sequence sequence);

        void VisitAlternatives(ParserNode.Alternatives alternatives);
    }

    public abstract class ParserNode
    {
        static long _idCount = 0;
        static long GetNextId() { return System.Threading.Interlocked.Increment(ref _idCount); }

        public ParserNode Parent { get; private set; }
        public int IndexInParentList { get; private set; }

        public Rule Rule { get; private set; }
        public RuleExpression Expression { get; private set; }

        long _id = GetNextId();

        internal ParserNode(Rule rule, RuleExpression expression)
        {
            this.Parent = null;
            this.IndexInParentList = -1;
            this.Rule = rule;
            this.Expression = expression;
        }

        void SetParent(ParserNode parent, int index)
        {
            if (this.Parent != null)
                throw new InvalidOperationException();

            this.Parent = parent;
            this.IndexInParentList = index;
        }

        public void Visit(IParserNodeVisitor visitor)
        {
            this.VisitImpl(visitor);
        }

        protected abstract void VisitImpl(IParserNodeVisitor visitor);

        public override string ToString()
        {
            return "(" + _id + ")" + this.GetType().Name + " [" + (this.Expression == null ? (this.Rule == null ? "<NULL>" : this.Rule.ToString()) : this.Expression.ToString()) + "]";
        }

        #region shared

        public abstract class GroupParserNode : ParserNode
        {
            List<ParserNode> _childs = null;
            ReadOnlyCollection<ParserNode> _childsFreezed = null;

            public ReadOnlyCollection<ParserNode> Childs
            {
                get
                {
                    if (_childsFreezed == null)
                    {
                        _childsFreezed = new ReadOnlyCollection<ParserNode>(_childs.EmptyCollectionIfNull().ToArray());
                        _childs = null;
                    }

                    return _childsFreezed;
                }
            }

            internal GroupParserNode(Rule rule, RuleExpression expression, params ParserNode[] childs)
                : base(rule, expression)
            {
                _childs = new List<ParserNode>(childs);

                for (int i = 0; i < _childs.Count; i++)
                {
                    _childs[i].SetParent(this, i);
                }
            }

            public void AddChild(ParserNode node)
            {
                node.SetParent(this, _childs.Count);
                _childs.Add(node);
            }
        }

        public abstract class SingleChildNode : ParserNode
        {
            public ParserNode Child { get; private set; }

            internal SingleChildNode(Rule rule, RuleExpression expression, ParserNode child)
                : base(rule, expression)
            {
                if (child == null)
                    throw new ArgumentNullException("Argument 'child' cannot be null!");

                this.Child = child;

                child.SetParent(this, 0);
            }

        }

        #endregion

        #region simple

        public sealed class FsmParserNode : ParserNode
        {
            public FSM Fsm { get; private set; }

            internal FsmParserNode(Rule rule, RuleExpression expression, FSM fsm)
                : base(rule, expression)
            {
                this.Fsm = fsm;
            }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitFsm(this);
            }
        }

        public sealed class RecursiveParserNode : ParserNode
        {
            public ParserNode Target { get; private set; }
            public RuleExpression.RuleUsage TargetCallExpr { get; private set; }

            internal RecursiveParserNode(Rule rule, RuleExpression.RuleUsage targetCallExpr)
                : base(rule, null)
            {
                this.Target = null;
                this.TargetCallExpr = targetCallExpr;
            }

            public void SetTarget(ParserNode target)
            {
                if (target == null)
                    throw new ArgumentNullException("Argument 'target' cannot be null!");
                if (this.Target != null)
                    throw new InvalidOperationException();

                this.Target = target;
            }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitRecursive(this);
            }

            public override string ToString()
            {
                return base.ToString() + " --> " + this.Target.ToString();
            }
        }

        #endregion

        #region single

        public class Check : SingleChildNode
        {
            public Check(Rule rule, RuleExpression expression, ParserNode child)
                : base(rule, expression, child) { }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitChek(this);
            }
        }

        public class CheckNot : SingleChildNode
        {
            public CheckNot(Rule rule, RuleExpression expression, ParserNode child)
                : base(rule, expression, child) { }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitCheckNot(this);
            }
        }

        public class RuleCall : SingleChildNode
        {
            public RuleCall(Rule rule, RuleExpression expression, ParserNode child)
                : base(rule, expression, child) { }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitRuleCall(this);
            }
        }

        public class Number : SingleChildNode
        {
            public int CountFrom { get; private set; }
            public int CountTo { get; private set; }

            public Number(Rule rule, RuleExpression expression, ParserNode child, int countFrom, int countTo)
                : base(rule, expression, child)
            {
                this.CountFrom = countFrom;
                this.CountTo = countTo;
            }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitNumber(this);
            }
        }

        #endregion

        #region groups

        public class Sequence : GroupParserNode
        {
            public Sequence(Rule rule, RuleExpression expression, params ParserNode[] childs)
                : base(rule, expression, childs) { }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitSequence(this);
            }
        }

        public class Alternatives : GroupParserNode
        {
            public Alternatives(Rule rule, RuleExpression expression, params ParserNode[] childs)
                : base(rule, expression, childs) { }

            protected override void VisitImpl(IParserNodeVisitor visitor)
            {
                visitor.VisitAlternatives(this);
            }
        }

        #endregion
    }

    class ParserNodeLoggingVisitor : IParserNodeVisitor
    {
        IParserNodeVisitor _v;
        StringBuilder _sb;

        public ParserNodeLoggingVisitor(IParserNodeVisitor v)
        {
            _v = v;
            _sb = new StringBuilder();
        }

        private void LogLine(string format, params object[] args)
        {
            var line = string.Format(format, args);
            _sb.AppendLine(line);
            // Debug.Print(line);
        }

        public void VisitFsm(ParserNode.FsmParserNode fsmParserNode)
        {
            this.LogLine("{0}", fsmParserNode); _v.VisitFsm(fsmParserNode);
        }

        public void VisitRecursive(ParserNode.RecursiveParserNode recursiveParserNode)
        {
            this.LogLine("{0}", recursiveParserNode); _v.VisitRecursive(recursiveParserNode);
        }

        public void VisitChek(ParserNode.Check check)
        {
            this.LogLine("{0}", check); _v.VisitChek(check);
        }

        public void VisitCheckNot(ParserNode.CheckNot checkNot)
        {
            this.LogLine("{0}", checkNot); _v.VisitCheckNot(checkNot);
        }

        public void VisitRuleCall(ParserNode.RuleCall ruleCall)
        {
            this.LogLine("{0}", ruleCall); _v.VisitRuleCall(ruleCall);
        }

        public void VisitNumber(ParserNode.Number number)
        {
            this.LogLine("{0}", number); _v.VisitNumber(number);
        }

        public void VisitSequence(ParserNode.Sequence sequence)
        {
            this.LogLine("{0}", sequence); _v.VisitSequence(sequence);
        }

        public void VisitAlternatives(ParserNode.Alternatives alternatives)
        {
            this.LogLine("{0}", alternatives); _v.VisitAlternatives(alternatives);
        }
    }

}