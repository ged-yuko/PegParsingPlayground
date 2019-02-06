using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    enum Association
    {
        Left,
        Right
    }

    partial class AnalyzerGraphBuilder
    {
        /*
        class RecursionRewriter : IRuleExpressionVisitor
        {
            class RewritedChildNode
            {
                public readonly RewritedChildNode prev;
                public readonly RuleExpression node;

                public RewritedChildNode(RewritedChildNode prev, RuleExpression node)
                {
                    this.prev = prev;
                    this.node = node;
                }
            }

            class RewritingContext
            {
                public readonly RuleExpression expression;
                public readonly RewritingContext parentContext;
                public readonly int invocationCount;
                public readonly RewritedChildNode childEntries;

                public RewritingContext(RuleExpression expression, RewritingContext parentContext, int invocationCount, RewritedChildNode childs)
                {
                    this.expression = expression;
                    this.parentContext = parentContext;
                    this.invocationCount = invocationCount;
                    this.childEntries = childs;
                }

                public RuleExpression[] GetChildNodes()
                {
                    var list = new List<RuleExpression>();

                    for (var e = this.childEntries; e != null; e = e.prev)
                        list.Add(e.node);

                    list.Reverse();
                    return list.ToArray();
                }

                public RewritingContext ForChildExpression(RuleExpression childExpression)
                {
                    return new RewritingContext(childExpression, this, 0, null);
                }

                public RewritingContext ForParentExpression(RuleExpression rewritedChild)
                {
                    return new RewritingContext(
                        this.parentContext.expression,
                        this.parentContext.parentContext,
                        this.parentContext.invocationCount + 1,
                        new RewritedChildNode(this.parentContext.childEntries, rewritedChild)
                    );
                }

                public static RewritingContext ForRoot(RuleExpression expr)
                {
                    return new RewritingContext(new RuleExpression.Sequence(expr), null, 0, null);
                }
            }

            readonly ExplicitRule _ctxRule;
            readonly Rule _alternativesRule;
            readonly RuleExpression.RuleUsage[] _alternatives;

            readonly IRuleExpressionVisitor _expressionsVisitor;

            RewritingContext _currContext;
            Association _association = Association.Left;

            private RecursionRewriter(ExplicitRule curr, ExplicitRule parent)
            {
                _ctxRule = curr;
                _alternativesRule = parent;
                _expressionsVisitor = new RuleExpressionLoggingVisitor(this);

                var alternativesExpr = parent.Expression as RuleExpression.Or;
                if (alternativesExpr == null)
                    throw new InvalidOperationException();

                _alternatives = alternativesExpr.Childs.Cast<RuleExpression.RuleUsage>().ToArray();

                if (curr.GetAttributes().Any(a => a.Name.ToLower() == "left"))
                    _association = Association.Left;
                if (curr.GetAttributes().Any(a => a.Name.ToLower() == "right"))
                    _association = Association.Right;
            }

            private RuleExpression Rewrite(RuleExpression ruleExpression)
            {
                _currContext = RewritingContext.ForRoot(ruleExpression);

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
                        throw new InvalidOperationException(string.Format("Missing transition for expression [{0}] was not created!", _currContext.expression));
                    if (_currContext == null)
                        throw new InvalidOperationException(string.Format("Invalid handler for expression [{0}]!", _currContext.expression));

                } while (_currContext.parentContext != null);

                return _currContext.childEntries.node;
            }

            /*

    #expr: sum | product | braces | num; // alternatives

    sum: expr sumOp expr; // current

    sum: (product | braces | num) sumOp (sum | product | braces | num); // result

    sum: (product | braces | num) (sumOp (product | braces | num))+;

             */
        /*

            int _argsCount = 0;
            int _termsCount = 0;
            bool _fallback = false;

            RuleExpression MakeArgExpression()
            {
                if (_argsCount > 2)
                    throw new NotSupportedException();

                var restAltarnatives = _alternatives.SkipWhile(e => e.RuleName != _ctxRule.Name);

                switch (_association)
                {
                    case Association.Left: restAltarnatives = restAltarnatives.Skip(_argsCount > 0 ? 1 : 0); break;
                    case Association.Right: restAltarnatives = restAltarnatives.Skip(_argsCount > 0 ? 0 : 1); break;
                    default:
                        throw new NotImplementedException("");
                }


                _argsCount++;

                return new RuleExpression.Or(restAltarnatives.ToArray());
            }

            #region IRuleExpressionVisitor impl

            void IRuleExpressionVisitor.VisitRuleUsage(RuleExpression.RuleUsage ruleUsage)
            {
                if (_currContext.invocationCount == 0)
                {
                    if (_termsCount > 0 || _fallback)
                    {
                        _fallback = true;
                        _currContext = _currContext.ForParentExpression(ruleUsage);
                    }
                    else
                    {
                        _currContext = _currContext.ForParentExpression(
                            ruleUsage.RuleName == _alternativesRule.Name ? this.MakeArgExpression() : ruleUsage
                        );
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

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

                    _currContext = _currContext.ForParentExpression(new RuleExpression.CheckNot(
                        _currContext.childEntries.node
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

                    _currContext = _currContext.ForParentExpression(new RuleExpression.Check(
                        _currContext.childEntries.node
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

                    _currContext = _currContext.ForParentExpression(new RuleExpression.MatchNumber(
                        matchNumber.CountFrom, matchNumber.CountTo, _currContext.childEntries.node
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

                    _currContext = _currContext.ForParentExpression(new RuleExpression.Sequence(
                        childs
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

                    _currContext = _currContext.ForParentExpression(new RuleExpression.Or(
                        childs
                    ));
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
                    _termsCount++;
                    _currContext = _currContext.ForParentExpression(regex);
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
                    _termsCount++;
                    _currContext = _currContext.ForParentExpression(chars);
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
                    _termsCount++;
                    _currContext = _currContext.ForParentExpression(charCode);
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
                    _termsCount++;
                    _currContext = _currContext.ForParentExpression(anyChar);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            #endregion

            public static RuleExpression RewriteRecursionCalls(ExplicitRule curr, ExplicitRule parent)
            {
                return new RecursionRewriter(curr, parent).Rewrite(curr.Expression);
            }
        }
        */
    }
}