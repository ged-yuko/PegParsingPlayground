using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Impl;

namespace ParserImpl
{
    public interface IParsingTreeWalkerHandler
    {
        void EnterGroup(IParsingTreeGroup group);
        void Terminal(IParsingTreeTerminal term);
        void ExitGroup();
    }

    public interface IParsingTreeStructureReader
    {
        string GetNodeName(int upToLevel);
    }

    public class ParsingTreeWalker
    {
        Stack<ParsingTreeNode.ParsedNode> _stack = new Stack<ParsingTreeNode.ParsedNode>();

        public ParsingTreeWalker()
        {
        }

        public void TraverseFragment(IParsingTreeNode root, IParsingTreeWalkerHandler handler, Location locFrom, Location locTo)
        {
            if (root == null)
                throw new ArgumentNullException("Argument 'root' cannot be null!");

            _stack.Clear();
            _stack.Push((ParsingTreeNode.Group)root);

            while (_stack.Count > 0)
            {
                var n = _stack.Count;
                var currNode = _stack.Pop();
                
                var g = currNode as ParsingTreeNode.Group;
                var t = currNode as ParsingTreeNode.Terminal;

                if (g != null)
                {
                    if (g.Location > locTo)
                        continue;

                    _stack.Push(null);
                    handler.EnterGroup(g);

                    foreach (ParsingTreeNode.ParsedNode item in g.GetRuleChilds(true))
                        _stack.Push(item);
                }
                else if (t != null)
                {
                    if (t.From > locTo)
                        continue;

                    handler.Terminal(t);

                    if (t.To < locFrom)
                        return;
                }
                else if (currNode == null)
                {
                    handler.ExitGroup();
                }
                else
                {
                    throw new NotImplementedException("");
                }
            }
        }
    }
}
