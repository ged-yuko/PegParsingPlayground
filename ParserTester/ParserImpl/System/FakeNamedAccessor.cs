using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public interface IFakeNamedGetAccessor<TArg, TValue>
    {
        TValue this[TArg arg] { get; }
    }

    public interface IFakeNamedSetAccessor<TArg, TValue>
    {
        TValue this[TArg arg] { set; }
    }

    public sealed class FakeNamedAccessor<TArg, TValue> : IFakeNamedGetAccessor<TArg, TValue>
    {
        public TValue this[TArg arg]
        {
            get
            {
                if (_getter == null)
                    throw new NotSupportedException();

                return _getter(arg);
            }
            set
            {
                if (_getter == null)
                    throw new NotSupportedException();

                _setter(arg, value);
            }
        }

        Func<TArg, TValue> _getter;
        Action<TArg, TValue> _setter;

        public FakeNamedAccessor(Func<TArg, TValue> getter) : this(getter, null) { }
        public FakeNamedAccessor(Action<TArg, TValue> setter) : this(null, setter) { }

        public FakeNamedAccessor(Func<TArg, TValue> getter, Action<TArg,TValue> setter)
        {
            _getter = getter;
            _setter = setter;
        }
    }
}
