using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using ParserImpl.Impl;

namespace ParserImpl.Grammar
{
    public abstract class RuleExpression
    {
        internal RuleExpression()
        {

        }

        public void Visit(IRuleExpressionVisitor visitor)
        {
            this.VisitImpl(visitor);
        }

        protected abstract void VisitImpl(IRuleExpressionVisitor visitor);
        
        public abstract override string ToString();

        #region impls

        public abstract class LowLevelExpression : RuleExpression
        {
            internal LowLevelExpression()
            {

            }
        }

        public sealed class AnyChar : LowLevelExpression
        {
            public AnyChar() { }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitAnyChar(this);
            }

            public override string ToString()
            {
                return ".";
            }
        }

        public sealed class CharCode : LowLevelExpression
        {
            public char Character { get; private set; }

            public CharCode(char character)
            {
                this.Character = character;
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitCharCode(this);
            }

            public override string ToString()
            {
                var c = this.Character;
                var asCode = char.IsControl(c) || char.IsHighSurrogate(c) || char.IsLowSurrogate(c) || char.IsSurrogate(c) || char.IsWhiteSpace(c);
                return "'" + (asCode ? "\\u" + Convert.ToString(this.Character, 16).PadLeft(4, '0') : this.Character.ToString()) + "'";
            }
        }

        public sealed class Chars : LowLevelExpression
        {
            public string Content { get; private set; }

            public Chars(string content)
            {
                if (string.IsNullOrEmpty(content))
                    throw new ArgumentNullException();

                this.Content = content;
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitChars(this);
            }

            public override string ToString()
            {
                return "\'" + this.Content + "\'";
            }
        }

        public sealed class Regex : LowLevelExpression
        {
            public string Pattern { get; private set; }

            public Regex(string pattern)
            {
                if (string.IsNullOrEmpty(pattern))
                    throw new ArgumentNullException();

                this.Pattern = pattern;
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitRegex(this);
            }

            public override string ToString()
            {
                return "\"" + this.Pattern + "\"";
            }
        }

        public sealed class RuleUsage : RuleExpression
        {
            public bool ExpandSubnodes { get; private set; }
            public string RuleName { get; private set; }
            public ReadOnlyCollection<RuleExpression> Arguments { get; private set; }

            public RuleUsage(string ruleName, params RuleExpression[] args)
                : this(false, ruleName, args) { }

            public RuleUsage(bool expandSubnodes, string ruleName, params RuleExpression[] args)
            {
                if (string.IsNullOrWhiteSpace(ruleName))
                    throw new ArgumentNullException();

                this.ExpandSubnodes = expandSubnodes;
                this.RuleName = ruleName;
                this.Arguments = new ReadOnlyCollection<RuleExpression>(args.EmptyCollectionIfNull());
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitRuleUsage(this);
            }

            public override string ToString()
            {
                return this.RuleName + (this.Arguments.Count > 0 ? "<" + string.Join(", ", this.Arguments) + ">" : string.Empty);
            }
        }

        public abstract class ExpressionsGroup : RuleExpression
        {
            public ReadOnlyCollection<RuleExpression> Childs { get; private set; }

            internal ExpressionsGroup(params RuleExpression[] childs)
            {
                if (childs == null)
                    throw new ArgumentNullException();

                this.Childs = new ReadOnlyCollection<RuleExpression>(childs);
            }
        }

        public abstract class UnaryExpressionGroup : RuleExpression
        {
            public RuleExpression Child { get; private set; }

            internal UnaryExpressionGroup(RuleExpression child)
            {
                if (child == null)
                    throw new ArgumentNullException();

                this.Child = child;
            }
        }

        public sealed class Or : ExpressionsGroup
        {
            public Or(params RuleExpression[] exprs)
                : base(exprs)
            {
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitAlternative(this);
            }

            public override string ToString()
            {
                return string.Join("|", this.Childs.Select(c => "(" + c.ToString() + ")"));
            }
        }

        public sealed class Sequence : ExpressionsGroup
        {
            public Sequence(params RuleExpression[] exprs)
                : base(exprs)
            {
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitSequence(this);
            }

            public override string ToString()
            {
                return string.Join(" ", this.Childs);
            }
        }

        public sealed class MatchNumber : UnaryExpressionGroup
        {
            public int CountFrom { get; private set; }
            public int CountTo { get; private set; }

            public MatchNumber(int countFrom, int countTo, RuleExpression child)
                : base(child)
            {
                if (countFrom < 0)
                    throw new ArgumentOutOfRangeException("countFrom");
                if (countTo < 1 || countTo < countFrom)
                    throw new ArgumentOutOfRangeException("countTo");

                this.CountFrom = countFrom;
                this.CountTo = countTo;
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitMatchNumber(this);
            }

            public override string ToString()
            {
                string ret;

                if (this.CountFrom == 0 && this.CountTo == 1)
                {
                    ret = "?";
                }
                else if (this.CountFrom == 0 && this.CountTo == int.MaxValue)
                {
                    ret = "*";
                }
                else if (this.CountFrom == 1 && this.CountTo == int.MaxValue)
                {
                    ret = "+";
                }
                else
                {
                    ret = "{" + CountFrom + ((CountTo != int.MaxValue) ? ("," + CountTo) : ("")) + "}";
                }

                return "(" + this.Child.ToString() + ")" + ret;
            }
        }

        public sealed class Check : UnaryExpressionGroup
        {
            public Check(RuleExpression expr)
                : base(expr)
            {
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitCheck(this);
            }

            public override string ToString()
            {
                return "&(" + this.Child.ToString() + ")";
            }
        }

        public sealed class CheckNot : UnaryExpressionGroup
        {
            public CheckNot(RuleExpression expr)
                : base(expr)
            {
            }

            protected override void VisitImpl(IRuleExpressionVisitor visitor)
            {
                visitor.VisitCheckNot(this);
            }

            public override string ToString()
            {
                return "!(" + this.Child.ToString() + ")";
            }
        }

        #endregion
    }
}
