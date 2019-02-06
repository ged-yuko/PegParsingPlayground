using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl
{
    public interface IFreezable
    {
        bool IsFrozen { get; }
        void Freeze();
    }

    public abstract class FreezableBase : IFreezable
    {
        public bool IsFrozen { get; private set; }

        public FreezableBase()
        {
            this.IsFrozen = false;
        }

        public void Freeze()
        {
            this.IsFrozen = true;
            this.OnFrozen();
        }

        protected virtual void OnFrozen() { }

        protected void ValidateNotFrozen()
        {
            if (this.IsFrozen)
                throw new InvalidOperationException();
        }
    }
}
