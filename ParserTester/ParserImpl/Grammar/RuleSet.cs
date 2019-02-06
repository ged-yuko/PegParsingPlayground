using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ParserImpl.Grammar
{
    public abstract class RuleSetBase : NamedEntityBase, IEnumerable<NamedEntityBase>
    {
        protected Dictionary<string, NamedEntityBase> _entities = new Dictionary<string, NamedEntityBase>();

        #region named indexed

        IFakeNamedGetAccessor<string, Rule> _rulesAccessor = null;
        public IFakeNamedGetAccessor<string, Rule> Rules
        {
            get
            {
                return _rulesAccessor ?? (_rulesAccessor = new FakeNamedAccessor<string, Rule>(key => {
                    var result = _entities[key] as Rule;
                    if (result == null)
                        throw new InvalidOperationException();

                    return result;
                }));
            }
        }

        IFakeNamedGetAccessor<string, RuleSet> _ruleSetsAccessor = null;
        public IFakeNamedGetAccessor<string, RuleSet> RuleSets
        {
            get
            {
                return _ruleSetsAccessor ?? (_ruleSetsAccessor = new FakeNamedAccessor<string, RuleSet>(key => {
                    var result = _entities[key] as RuleSet;
                    if (result == null)
                        throw new InvalidOperationException();

                    return result;
                }));
            }
        }

        IFakeNamedGetAccessor<string, RuleSetImport> _ruleSetImportsAccessor = null;
        public IFakeNamedGetAccessor<string, RuleSetImport> RuleSetImports
        {
            get
            {
                return _ruleSetImportsAccessor ?? (_ruleSetImportsAccessor = new FakeNamedAccessor<string, RuleSetImport>(key => {
                    var result = _entities[key] as RuleSetImport;
                    if (result == null)
                        throw new InvalidOperationException();

                    return result;
                }));
            }
        }

        #endregion

        internal RuleSetBase(string name)
            : base(name)
        {
        }

        public void Add(RuleSetBase entity)
        {
            if (entity == null)
                throw new ArgumentNullException();

            this.ValidateNotFrozen();
            _entities.Add(entity.Name, entity);
        }

        public ReadOnlyCollection<NamedEntityBase> GetEntities()
        {
            return new ReadOnlyCollection<NamedEntityBase>(_entities.Values.ToArray());
        }

        public IEnumerator<NamedEntityBase> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public sealed class RuleSet : RuleSetBase
    {
        public RuleSet(string name)
            : base(name)
        {
        }

        public void Add(RuleSetImport import)
        {
            _entities.Add(import.Name, import);
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitRuleSet(this);
        }
    }

    public sealed class RuleSetImport : NamedEntityBase
    {
        public string RuleSetName { get; private set; }

        public RuleSetImport(string name, string ruleSetName)
            : base(name)
        {
            if (string.IsNullOrEmpty(ruleSetName))
                throw new ArgumentNullException();

            this.RuleSetName = ruleSetName;
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitRuleSetImport(this);
        }
    }
}
