using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ParserImpl.Grammar
{
    public sealed class RuleParameter : NamedEntityBase
    {
        bool _expandable;

        public bool IsExpandable
        {
            get { return _expandable; }
            set
            {
                this.ValidateNotFrozen();
                _expandable = value;
            }
        }

        public RuleParameter(string name)
            : base(name)
        {
            _expandable = false;
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitRuleParameter(this);
        }
    }

    public abstract class Rule : RuleSetBase
    {
        readonly int _tokenId;

        public int TokenId { get { return _tokenId; } }

        bool _expandable;

        public bool IsExpandable
        {
            get { return _expandable; }
            set
            {
                this.ValidateNotFrozen();
                _expandable = value;
            }
        }

        internal Rule(int tokenId, string name)
            : base(name)
        {
            _tokenId = tokenId;
            _expandable = false;
        }

        public void Add(RuleParameter param)
        {
            if (param == null)
                throw new ArgumentNullException();

            this.ValidateNotFrozen();
            _entities.Add(param.Name, param);
        }

        public override string ToString()
        {
            var ruleArgsInfo = this.GetEntities().OfType<RuleParameter>().ToArray();
            var args = ruleArgsInfo.Length == 0 ? string.Empty 
                : "<" + string.Join(", ", ruleArgsInfo.Select(a => (a.IsExpandable ? "#" : string.Empty) + a.Name)) + ">";
            
            return this.GetType().Name + " [" + this.Name + args + "]";
        }
    }

    public sealed class ExplicitRule : Rule
    {
        public RuleExpression Expression { get; private set; }

        public ExplicitRule(int tokenId, string name, RuleExpression expression)
            : base(tokenId, name)
        {
            this.Expression = expression ?? throw new ArgumentNullException();
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitExplicitRule(this);
        }
    }

    public sealed class ExtensibleRuleAlternativeInfo
    {
        public int Priority { get; private set; }
        public Rule Rule { get; private set; }

        internal ExtensibleRuleAlternativeInfo(int priority, Rule rule)
        {
            if (rule == null)
                throw new ArgumentNullException();

            this.Priority = priority;
            this.Rule = rule;
        }
    }

    public sealed class ExtensibleRule : Rule
    {
        Dictionary<string, ExtensibleRuleAlternativeInfo> _prioritizedRules = new Dictionary<string, ExtensibleRuleAlternativeInfo>();

        public ExtensibleRule(int tokenId, string name)
            : base(tokenId, name)
        {

        }

        public void Add(int priority, Rule rule)
        {
            if (rule == null)
                throw new ArgumentNullException();

            _prioritizedRules.Add(rule.Name, new ExtensibleRuleAlternativeInfo(priority, rule));
            base.Add(rule);
        }

        public ReadOnlyCollection<ExtensibleRuleAlternativeInfo> GetAlternatives()
        {
            return new ReadOnlyCollection<ExtensibleRuleAlternativeInfo>(_prioritizedRules.Values.ToArray());
        }

        protected override void OnFrozen()
        {
            foreach (var item in base.GetEntities().OfType<Rule>())
            {
                if (!_prioritizedRules.ContainsKey(item.Name))
                    _prioritizedRules.Add(item.Name, new ExtensibleRuleAlternativeInfo(0, item));
            }
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitExtensibleRule(this);
        }
    }
}
