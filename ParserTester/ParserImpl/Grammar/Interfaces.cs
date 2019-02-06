using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ParserImpl.Grammar
{
    public interface IRuleSetVisitor
    {
        void VisitAttribute(EntityAttribute entityAttribute);

        void VisitRuleSet(RuleSet ruleSet);

        void VisitRuleSetImport(RuleSetImport ruleSetImport);

        void VisitRuleParameter(RuleParameter ruleParameter);

        void VisitExplicitRule(ExplicitRule explicitRule);

        void VisitExtensibleRule(ExtensibleRule extensibleRule);
    }

    public interface IRuleExpressionVisitor
    {
        void VisitCheckNot(RuleExpression.CheckNot checkNot);

        void VisitCheck(RuleExpression.Check check);

        void VisitMatchNumber(RuleExpression.MatchNumber matchNumber);

        void VisitSequence(RuleExpression.Sequence sequence);

        void VisitAlternative(RuleExpression.Or or);

        void VisitRuleUsage(RuleExpression.RuleUsage ruleUsage);

        void VisitRegex(RuleExpression.Regex regex);

        void VisitChars(RuleExpression.Chars chars);

        void VisitCharCode(RuleExpression.CharCode charCode);

        void VisitAnyChar(RuleExpression.AnyChar anyChar);
    }

    public class RuleExpressionLoggingVisitor : IRuleExpressionVisitor
    {
        IRuleExpressionVisitor _v;
        StringBuilder _sb;

        public RuleExpressionLoggingVisitor(IRuleExpressionVisitor v)
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
 
        public void VisitCheckNot(RuleExpression.CheckNot checkNot)
        {
            this.LogLine("{0}", checkNot);
            _v.VisitCheckNot(checkNot);
        }

        public void VisitCheck(RuleExpression.Check check)
        {
            this.LogLine("{0}", check);
            _v.VisitCheck(check);
        }

        public void VisitMatchNumber(RuleExpression.MatchNumber matchNumber)
        {
            this.LogLine("{0}", matchNumber);
            _v.VisitMatchNumber(matchNumber);
        }

        public void VisitSequence(RuleExpression.Sequence sequence)
        {
            this.LogLine("{0}", sequence);
            _v.VisitSequence(sequence);
        }

        public void VisitAlternative(RuleExpression.Or or)
        {
            this.LogLine("{0}", or);
            _v.VisitAlternative(or);
        }

        public void VisitRuleUsage(RuleExpression.RuleUsage ruleUsage)
        {
            this.LogLine("{0}", ruleUsage);
            _v.VisitRuleUsage(ruleUsage);
        }

        public void VisitRegex(RuleExpression.Regex regex)
        {
            this.LogLine("{0}", regex);
            _v.VisitRegex(regex);
        }

        public void VisitChars(RuleExpression.Chars chars)
        {
            this.LogLine("{0}", chars);
            _v.VisitChars(chars);
        }

        public void VisitCharCode(RuleExpression.CharCode charCode)
        {
            this.LogLine("{0}", charCode);
            _v.VisitCharCode(charCode);
        }

        public void VisitAnyChar(RuleExpression.AnyChar anyChar)
        {
            this.LogLine("{0}", anyChar);
            _v.VisitAnyChar(anyChar);
        }
    }
}
