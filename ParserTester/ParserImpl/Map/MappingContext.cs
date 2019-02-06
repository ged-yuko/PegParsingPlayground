using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Map
{
    public class MappingContext<TContext>
	{
        Mapping<TContext> _mapping;

		public TContext Context { get; set; }
		public object Result { get; internal set; }

        internal MappingContext(Mapping<TContext> mapping)
		{
			_mapping = mapping;
		}

		public T Map<T>(IParsingTreeNode node)
		{
			return _mapping.GetMap<T>(node.Rule)(node, this);
		}
	}
}
