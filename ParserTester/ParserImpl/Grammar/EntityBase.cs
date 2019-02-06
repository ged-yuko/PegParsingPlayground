using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ParserImpl.Grammar
{
    public abstract class EntityBase : FreezableBase
    {
        internal EntityBase()
        {
        }

        public void Visit(IRuleSetVisitor visitor)
        {
            this.VisitImpl(visitor);
        }

        protected abstract void VisitImpl(IRuleSetVisitor visitor);
    }

    public abstract class NamedEntityBase : EntityBase
    {
        private List<EntityAttribute> _attributes = new List<EntityAttribute>();

        public string Name { get; private set; }

        internal NamedEntityBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException();

            this.Name = name;
        }

        public void Add(EntityAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentNullException();

            this.ValidateNotFrozen();
            _attributes.Add(attribute);
        }

        public ReadOnlyCollection<EntityAttribute> GetAttributes()
        {
            return new ReadOnlyCollection<EntityAttribute>(_attributes.ToArray());
        }

        public override string ToString()
        {
            return this.GetType().Name + " [" + this.Name + "]";
        }
    }

    public sealed class EntityAttribute : NamedEntityBase
    {
        public ReadOnlyCollection<RuleExpression> Arguments { get; private set; }

        public EntityAttribute(string name, params RuleExpression[] args)
            : base(name)
        {
            this.Arguments = new ReadOnlyCollection<RuleExpression>(args.EmptyCollectionIfNull());
            this.Freeze();
        }

        protected override void VisitImpl(IRuleSetVisitor visitor)
        {
            visitor.VisitAttribute(this);
        }
    }
}
