using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Impl
{
    class ParserGraphPrinter : IParserNodeVisitor
    {
        IndentedWriter _w = null;
        List<string> _indents = new List<string>();

        const string _initial = "──";
        const string _middleIndent = "├─";
        const string _finishIndent = "└─";
        const string _space = "┈┈";
        const string _skipIndent = "│┈";

        public ParserGraphPrinter()
        {
        }

        public string CollectTree(ParserNode node)
        {
            _w = new IndentedWriter();

            this.Push(_finishIndent);
            node.Visit(this);

            var result = _w.GetContentAsString();
            _w = null;

            return result;
        }

        public static string Collect(ParserNode node)
        {
            return new ParserGraphPrinter().CollectTree(node);
        }

        private void PrintIndent()
        {
            foreach (var item in _indents)
            {
                _w.Write(item);
            }
        }

        private void Push(string indent)
        {
            _indents.Add(indent);
        }

        private string Pop()
        {
            var ret = _indents[_indents.Count - 1];
            _indents.RemoveAt(_indents.Count - 1);
            return ret;
        }

        private void PrintNode(ParserNode.SingleChildNode node)
        {
            this.PrintIndent();
            _w.WriteLine(node.ToString());

            var t = this.Pop();
            this.Push(t == _finishIndent ? _space : _skipIndent);

            this.Push(_finishIndent);
            node.Child.Visit(this);
            this.Pop();

            this.Pop();
            this.Push(t);
        }

        private void PrintNode(ParserNode.GroupParserNode node)
        {
            this.PrintIndent();
            _w.WriteLine(node.ToString());

            var t = this.Pop();
            this.Push(t == _finishIndent ? _space : _skipIndent);

            this.Push(_middleIndent);
            int i;
            for (i = 0; i < node.Childs.Count - 1; i++)
            {
                node.Childs[i].Visit(this);
            }
            this.Pop();

            this.Push(_finishIndent);
            node.Childs[i].Visit(this);
            this.Pop();

            this.Pop();
            this.Push(t);
        }

        private void PrintNode(ParserNode node)
        {
            this.PrintIndent();
            _w.WriteLine(node.ToString());
        }

        void IParserNodeVisitor.VisitFsm(ParserNode.FsmParserNode fsmParserNode)
        {
            this.PrintNode(fsmParserNode);
        }

        void IParserNodeVisitor.VisitRecursive(ParserNode.RecursiveParserNode recursiveParserNode)
        {
            this.PrintNode(recursiveParserNode);
        }

        void IParserNodeVisitor.VisitChek(ParserNode.Check check)
        {
            this.PrintNode(check);
        }

        void IParserNodeVisitor.VisitCheckNot(ParserNode.CheckNot checkNot)
        {
            this.PrintNode(checkNot);
        }

        void IParserNodeVisitor.VisitRuleCall(ParserNode.RuleCall ruleCall)
        {
            this.PrintNode(ruleCall);
        }

        void IParserNodeVisitor.VisitNumber(ParserNode.Number number)
        {
            this.PrintNode(number);
        }

        void IParserNodeVisitor.VisitSequence(ParserNode.Sequence sequence)
        {
            this.PrintNode(sequence);
        }

        void IParserNodeVisitor.VisitAlternatives(ParserNode.Alternatives alternatives)
        {
            this.PrintNode(alternatives);
        }
    }
}
