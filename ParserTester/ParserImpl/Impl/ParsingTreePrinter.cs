using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Impl
{

    public class FullParsingTreePrinter : IParsingTreeNodeVisitor
    {
        StringBuilder _sb;
        string _s;
        int _i;

        private FullParsingTreePrinter()
        {
            _sb = new StringBuilder();
            _s = "  ";
            _i = 0;
        }

        public static string CollectTree(IParsingTreeNode node)
        {
            var v = new FullParsingTreePrinter();
            node.Visit(v);
            return v._sb.ToString();
        }

        private void PrintNode(IParsingTreeNode node)
        {
            for (int i = 0; i < _i; i++)
            {
                _sb.Append(_s);
            }

            _sb.Append(node.GetType().Name);
            _sb.AppendFormat("  @{0} {1} ", (node as ParsingTreeNode).Location, node.Rule);
            _sb.AppendLine(((ParsingTreeNode)node).GrammarNode.ToString());
        }

        void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
        {
            PrintNode(group);

            _i++;
            foreach (var item in group.Childs)
                item.Visit(this);

            _i--;
        }

        void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
        {
            PrintNode(terminal);
        }
    }

    public class RulesParsingTreePrinter : IParsingTreeNodeVisitor
    {
        StringBuilder _sb;
        string _s;
        int _i;

        private RulesParsingTreePrinter()
        {
            _sb = new StringBuilder();
            _s = "  ";
            _i = 0;
        }

        public static string CollectTree(IParsingTreeNode node)
        {
            var v = new RulesParsingTreePrinter();
            node.Visit(v);
            return v._sb.ToString();
        }

        private void PrintNode(IParsingTreeNode node)
        {
            for (int i = 0; i < _i; i++)
            {
                _sb.Append(_s);
            }

            _sb.Append(node.GetType().Name);
            _sb.AppendFormat("  @{0} {1} ", (node as ParsingTreeNode).Location, node.Rule);
            _sb.AppendLine(((ParsingTreeNode)node).GrammarNode.ToString());
        }

        void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
        {
            PrintNode(group);

            _i++;
            foreach (var item in group.GetRuleChilds())
                item.Visit(this);

            _i--;
        }

        void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
        {
            PrintNode(terminal);
        }
    }
}
