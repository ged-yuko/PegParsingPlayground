using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Map
{
	public class Mapping<TContext>
	{
		class MapInfo
		{
			public readonly Delegate action;
			public readonly Rule rule;

			public MapInfo(Rule rule, Delegate action)
			{
				if (rule == null)
					throw new ArgumentNullException();
				if (action == null)
					throw new ArgumentNullException();

				this.rule = rule;
				this.action = action;
			}
		}

		List<MapInfo> _maps = new List<MapInfo>();

		public Mapping()
		{
		}

		private void Set(MapInfo info)
		{
			var id = info.rule.TokenId;

			while (id >= _maps.Count)
				_maps.Add(null);

			_maps[id] = info;
		}

        public Mapping<TContext> Set<T>(Rule rule, Func<IParsingTreeNode, MappingContext<TContext>, T> action)
		{
			var info = new MapInfo(rule, action);

			this.Set(info);

			return this;
		}

        public Func<IParsingTreeNode, MappingContext<TContext>, T> GetMap<T>(Rule rule)
		{
            return (Func<IParsingTreeNode, MappingContext<TContext>, T>)_maps[rule.TokenId].action;
		}

        public MappingContext<TContext> Translate(IParsingTreeNode node, TContext contextObj)
		{
            var context = new MappingContext<TContext>(this);
            context.Context = contextObj;
			context.Result = _maps[node.Rule.TokenId].action.DynamicInvoke(node, context);
			return context;
		}
	}
}
