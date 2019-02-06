using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    abstract class ParserStep : IParserStep
    {
        public IParserStep Prev { get { return this.PrevStep; } }
        public ParserStep PrevStep { get; private set; }
        public ParserNode Node { get; private set; }
        public Location Location { get; private set; }

        public Rule Rule { get { return this.Node.Rule; } }
        public RuleExpression Expression { get { return this.Node.Expression; } }

        internal ParserStep(ParserStep prev, ParserNode Node, Location location)
        {
            this.PrevStep = prev;
            this.Node = Node;
            this.Location = location;
        }

        public void Visit(IParserStepVisitor visitor)
        {
            this.VisitImpl(visitor);
        }

        protected abstract void VisitImpl(IParserStepVisitor visitor);

        public ParserStep CreateTerminal(ParserNode node, Location from, Location to, string text)
        {
            return new Terminal(this, node, from, to, text);
        }

        public ParserStep CreateEnterNode(ParserNode node, Location location)
        {
            return new EnterNode(this, node, location);
        }

        public ParserStep CreateExitNode(ParserNode node, Location location)
        {
            return new ExitNode(this, node, location);
        }

        public override string ToString()
        {
            return this.Node.ToString();
        }

        #region implementations

        public sealed class Terminal : ParserStep, ITerminalStep
        {
            public Location From { get; private set; }
            public Location To { get; private set; }
            public string Text { get; private set; }

            public Terminal(ParserStep prev, ParserNode node, Location from, Location to, string text)
                : base(prev, node, from)
            {
                this.From = from;
                this.To = to;
                this.Text = text;
            }

            protected override void VisitImpl(IParserStepVisitor visitor)
            {
                visitor.VisitTerminal(this);
            }
        }

        public sealed class EnterNode : ParserStep, IEnterStep
        {
            public EnterNode(ParserStep prev, ParserNode node, Location location)
                : base(prev, node, location) { }

            protected override void VisitImpl(IParserStepVisitor visitor)
            {
                visitor.VisitEnterNode(this);
            }
        }

        public sealed class ExitNode : ParserStep, IExitStep
        {
            public ExitNode(ParserStep prev, ParserNode node, Location location)
                : base(prev, node, location) { }

            protected override void VisitImpl(IParserStepVisitor visitor)
            {
                visitor.VisitExitNode(this);
            }
        }

        #endregion
    }

    public class ParserStepsPrinter : IParserStepVisitor
    {
        IndentedWriter _w = null;

        private ParserStepsPrinter()
        {

        }


        string CollectImpl(IParserStep node)
        {
            var stack = new Stack<IParserStep>();
            for (var step = node; step != null; step = step.Prev)
                stack.Push(step);

            _w = new IndentedWriter("  ");
            _w.Push().Push();

            while (stack.Count > 0)
            {
                stack.Pop().Visit(this);
            }

            var result = _w.GetContentAsString();
            _w = null;

            return result;
        }

        public static string Collect(IParserStep node)
        {
            return new ParserStepsPrinter().CollectImpl(node);
        }

        void IParserStepVisitor.VisitTerminal(ITerminalStep terminal)
        {
            _w.WriteLine(terminal.ToString());
        }

        void IParserStepVisitor.VisitEnterNode(IEnterStep enterNode)
        {
            _w.Push();
            _w.WriteLine(enterNode.ToString());
        }

        void IParserStepVisitor.VisitExitNode(IExitStep exitNode)
        {
            _w.Pop();
        }
    }
}
