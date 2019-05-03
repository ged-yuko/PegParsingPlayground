using ParserImpl;
using ParserImpl.Grammar;
using ParserImpl.Impl;
using System;

namespace ParserTester
{
    public class ReplacedNode : IParsingTreeTerminal
    {
        private IParsingTreeTerminal _original;

        public ReplacedNode(IParsingTreeTerminal originalTerminal, string content, Location from, Location to)
        {
            _original = originalTerminal;
            From = from;
            To = to;
            Content = content;
        }

        public ParserNode GrammarNode => _original.GrammarNode;

        public Location Location => From;

        public Rule Rule => _original.Rule;

        public RuleExpression Expression => _original.Expression;

        public Location From { get; }

        public Location To { get; }

        public string Content { get; }

        public void Visit(IParsingTreeNodeVisitor visitor)
        {
            visitor.VisitTerminal(this);
        }
    }
}
