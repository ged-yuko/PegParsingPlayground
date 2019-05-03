using ParserImpl;
using ParserImpl.Grammar;
using ParserImpl.Impl;
using System.Collections.Generic;
using System.Linq;

namespace ParserTester
{
    public class ReplacedGroupNode : IParsingTreeGroup
    {
        private IParsingTreeGroup _original;

        public ReplacedGroupNode(IParsingTreeGroup original, IEnumerable<IParsingTreeNode> childs)
        {
            _original = original;
            Childs = childs;
        }

        public IEnumerable<IParsingTreeNode> Childs { get; }

        public int ChildsCount => Childs.Count();

        public ParserNode GrammarNode => _original.GrammarNode;

        public Location Location => _original.Location;

        public Rule Rule => _original.Rule;

        public RuleExpression Expression => _original.Expression;

        public void Visit(IParsingTreeNodeVisitor visitor)
        {
            visitor.VisitGroup(this);
        }
    }
}
