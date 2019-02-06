using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{


    abstract class ParsingTreeNode : IParsingTreeNode
    {
        public ParserNode GrammarNode { get; private set; }
        public Location Location { get; private set; }

        public Rule Rule { get { return this.GrammarNode == null ? null : this.GrammarNode.Rule; } }
        public RuleExpression Expression { get { return this.GrammarNode.Expression; } }

        public ParsingTreeNode(ParserNode node, Location loc)
        {
            this.GrammarNode = node;
            this.Location = loc;
        }

        public void Visit(IParsingTreeNodeVisitor visitor)
        {
            this.VisitImpl(visitor);
        }

        protected abstract void VisitImpl(IParsingTreeNodeVisitor visitor);

        public static TemporaryGroup CreateRootGroup(ParserNode node, Location loc)
        {
            return new TemporaryGroup(node, loc, null, null);
        }

        public static TemporaryGroup CreateRootGroup(IParsingTreeNode oldNode, ParserNode omitPatternRoot, Location locFrom, Location locTo, out ParserNode prevGrammarNode, out bool insideOmittedFragment)
        {
            insideOmittedFragment = false;
            var oldGroup = oldNode as IParsingTreeGroup;
            if (oldGroup == null || oldGroup.Location > locFrom)
                throw new InvalidOperationException();

            prevGrammarNode = null;
            var newGroup = new TemporaryReparsingGroup(oldNode, (oldNode as ParsingTreeNode).GrammarNode, locFrom, locTo, null, null);
            var newGroupChild = CreateItemToReparse(oldGroup, newGroup, locFrom, locTo);
            int depth = 0;
            while (newGroupChild != null)
            {
                if (newGroupChild.GrammarNode == omitPatternRoot)
                    insideOmittedFragment = true;

                prevGrammarNode = newGroup.GrammarNode;
                oldGroup = newGroupChild.OldGroup;
                newGroup = newGroupChild;
                newGroupChild = CreateItemToReparse(oldGroup, newGroup, locFrom, locTo);
                depth++;
            }

            // TODO: [Portable.Parser.Impl.ParsingTreeNode.CreateRootGroup] to think about possible parsing directions when reparsing
            //var lastTakenChild = newGroup.Childs.LastOrDefault();
            //if (lastTakenChild != null)
            //    prevGrammarNode = ((ParsingTreeNode)lastTakenChild).GrammarNode;

            prevGrammarNode = newGroup.Parent.GrammarNode;
            return newGroup;
        }

        private static TemporaryReparsingGroup CreateItemToReparse(IParsingTreeGroup oldGroup, TemporaryReparsingGroup parent, Location fromLoc, Location toLoc)
        {
            IParsingTreeGroup prevGroup = null;

            foreach (var oldChild in oldGroup.Childs)
            {
                if (oldChild.Location <= fromLoc)
                {
                    var childTerm = oldChild as IParsingTreeTerminal;
                    var childGroup = oldChild as IParsingTreeGroup;

                    if (childTerm != null)
                    {
                        prevGroup = null;

                        if (childTerm.To > fromLoc)
                        {
                            break; // this term need to be reparsed and so forth
                        }
                        else
                        {
                            // skip
                        }
                    }
                    else if (childGroup != null)
                    {
                        prevGroup = childGroup;
                    }
                    else
                    {
                        throw new NotImplementedException("");
                    }
                }
                else
                {
                    break; // prev child item need to be processed
                }
            }

            return prevGroup == null ? null : new TemporaryReparsingGroup(
                prevGroup, (prevGroup as ParsingTreeNode).GrammarNode, fromLoc, toLoc, parent, null
            );
        }

        #region temporary impls

        public abstract class TemporaryNode : ParsingTreeNode
        {
            public TemporaryGroup Parent { get; private set; }

            public TemporaryNode(ParserNode node, Location loc, TemporaryGroup parent)
                : base(node, loc)
            {
                this.Parent = parent;
            }

            public TemporaryChildNode Recreate()
            {
                return this.RecreateImpl();
            }

            protected abstract TemporaryChildNode RecreateImpl();

            public TemporaryGroup ExitChild()
            {
                return this.Parent.UpdateChilds(this.Recreate());
            }

            public TemporaryGroup RemoveCurrent()
            {
                return this.Parent;
            }
        }

        public class TemporaryGroup : TemporaryNode, IParsingTreeGroup
        {
            public TemporaryChildNode Child { get; private set; }

            public virtual IEnumerable<IParsingTreeNode> Childs
            {
                get
                {
                    if (this.Child != null)
                    {
                        var stack = new Stack<TemporaryChildNode>();
                        for (var t = this.Child; t != null; t = t.Prev)
                            stack.Push(t);

                        while (stack.Count > 0)
                            yield return stack.Pop();
                    }
                }
            }

            public virtual int ChildsCount
            {
                get
                {
                    int i = 0;
                    for (var t = this.Child; t != null; t = t.Prev)
                        i++;

                    return i;
                }
            }

            public bool HasChilds { get { return this.Child != null; } }

            public TemporaryGroup(ParserNode node, Location loc, TemporaryGroup parent, TemporaryChildNode child)
                : base(node, loc, parent)
            {
                this.Child = child;
            }

            public TemporaryGroup UpdateChilds(TemporaryChildNode child)
            {
                return this.UpdateChildsImpl(child);
            }

            protected virtual TemporaryGroup UpdateChildsImpl(TemporaryChildNode child)
            {
                return new TemporaryGroup(
                    this.GrammarNode,
                    this.Location,
                    this.Parent,
                    child
                );
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitGroup(this);
            }

            protected override TemporaryChildNode RecreateImpl()
            {
                return new TemporaryGroupChildGroup(
                    this.GrammarNode, 
                    this.Location, 
                    this.Parent == null ? null : this.Parent.Child, 
                    this.RecreateChilds()
                );
            }

            private ParsedNode RecreateChilds()
            {
                ParsedNode node = null;

                for (var n = this.Child; n != null; n = n.Prev)
                    node = n.Recreate(node);

                return node;
            }

            public TemporaryGroup CreateChildGroup(ParserNode node, Location loc)
            {
                return new TemporaryGroup(node, loc, this, null);
            }

            public TemporaryTerminal CreateChildTerminal(ParserNode node, Location loc, Location from, Location to, string content)
            {
                return new TemporaryTerminal(node, loc, this, from, to, content);
            }

            public bool TryReconstruct(ParserNode nextNode, Location location, out TemporaryGroup reconstructedTree)
            {
                return this.TryReconstructImpl(nextNode, location, out reconstructedTree);
            }

            protected virtual bool TryReconstructImpl(ParserNode node, Location loc, out TemporaryGroup reconstructedRoot)
            {
                reconstructedRoot = null;
                return false;
            }

            public TemporaryGroup TakeOffCurrent()
            {
                if (this.ChildsCount != 1)
                    throw new InvalidOperationException();

                return this.Parent.UpdateChilds(this.Child.UpdatePrevLinks(this.Parent.Child));
            }
        }

        public class TemporaryTerminal : TemporaryNode, IParsingTreeTerminal
        {
            public Location From { get; private set; }
            public Location To { get; private set; }
            public string Content { get; private set; }

            public TemporaryTerminal(ParserNode node, Location loc, TemporaryGroup parent, Location from, Location to, string content)
                : base(node, loc, parent)
            {
                this.From = from;
                this.To = to;
                this.Content = content;
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitTerminal(this);
            }

            protected override TemporaryChildNode RecreateImpl()
            {
                return new TemporaryGroupChildTerminal(this.GrammarNode, this.Location, this.Parent.Child, this.From, this.To, this.Content);
            }

            public override string ToString()
            {
                return this.Content;
            }
        }

        #endregion

        #region temporary child impls

        public abstract class TemporaryChildNode : ParsingTreeNode
        {
            public TemporaryChildNode Prev { get; private set; }

            internal TemporaryChildNode(ParserNode grammarNode, Location loc, TemporaryChildNode prev)
                : base(grammarNode, loc)
            {
                this.Prev = prev;
            }

            public ParsedNode Recreate(ParsedNode next)
            {
                return this.RecreateImpl(next);
            }

            protected abstract ParsedNode RecreateImpl(ParsedNode next);

            public TemporaryChildNode UpdatePrevLinks(TemporaryChildNode prevNode)
            {
                return this.UpdatePrevLinksImpl(prevNode);
            }

            protected abstract TemporaryChildNode UpdatePrevLinksImpl(TemporaryChildNode prevNode);
        }

        public sealed class TemporaryGroupChildGroup : TemporaryChildNode, IParsingTreeGroup
        {
            public ParsedNode Child { get; private set; }

            public IEnumerable<IParsingTreeNode> Childs
            {
                get
                {
                    for (var t = this.Child; t != null; t = t.Next)
                        yield return t;
                }
            }

            public int ChildsCount
            {
                get
                {
                    int i = 0;
                    for (var t = this.Child; t != null; t = t.Next)
                        i++;

                    return i;
                }
            }

            public TemporaryGroupChildGroup(ParserNode grammarNode, Location loc, TemporaryChildNode prev, ParsedNode child)
                : base(grammarNode, loc, prev)
            {
                this.Child = child;
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitGroup(this);
            }

            protected override ParsedNode RecreateImpl(ParsedNode next)
            {
                return new Group(this.GrammarNode, this.Location, next, this.Child);
            }

            protected override TemporaryChildNode UpdatePrevLinksImpl(TemporaryChildNode prevNode)
            {
                return new TemporaryGroupChildGroup(
                    this.GrammarNode,
                    this.Location,
                    this.Prev == null ? prevNode : this.Prev.UpdatePrevLinks(prevNode),
                    this.Child
                );
            }
        }

        public sealed class TemporaryGroupChildTerminal : TemporaryChildNode, IParsingTreeTerminal
        {
            public Location From { get; private set; }
            public Location To { get; private set; }
            public string Content { get; private set; }

            public TemporaryGroupChildTerminal(ParserNode grammarNode, Location loc, TemporaryChildNode prev, Location from, Location to, string content)
                : base(grammarNode, loc, prev)
            {
                this.From = from;
                this.To = to;
                this.Content = content;
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitTerminal(this);
            }

            protected override ParsedNode RecreateImpl(ParsedNode next)
            {
                return new Terminal(this.GrammarNode, this.Location, next, this.From, this.To, this.Content);
            }

            protected override TemporaryChildNode UpdatePrevLinksImpl(TemporaryChildNode prevNode)
            {
                return new TemporaryGroupChildTerminal(
                    this.GrammarNode,
                    this.Location,
                    this.Prev == null ? prevNode : this.Prev.UpdatePrevLinks(prevNode),
                    this.From,
                    this.To,
                    this.Content
                );
            }

            public override string ToString()
            {
                return this.Content;
            }
        }

        #endregion

        #region finalized impls

        public abstract class ParsedNode : ParsingTreeNode
        {
            public ParsedNode Next { get; private set; }

            public ParsedNode(ParserNode node, Location loc, ParsedNode next)
                : base(node, loc)
            {
                this.Next = next;
            }

            public ParsedNode UpdateNext(ParsedNode next)
            {
                return this.UpdateNextImpl(next);
            }

            protected abstract ParsedNode UpdateNextImpl(ParsedNode next);
        }

        public class Group : ParsedNode, IParsingTreeGroup
        {
            public ParsedNode Child { get; private set; }

            public IEnumerable<IParsingTreeNode> Childs
            {
                get
                {
                    for (var t = this.Child; t != null; t = t.Next)
                        yield return t;
                }
            }

            public int ChildsCount
            {
                get
                {
                    int i = 0;
                    for (var t = this.Child; t != null; t = t.Next)
                        i++;

                    return i;
                }
            }

            public Group(ParserNode node, Location loc, ParsedNode next, ParsedNode child)
                : base(node, loc, next)
            {
                this.Child = child;
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitGroup(this);
            }

            protected override ParsedNode UpdateNextImpl(ParsedNode next)
            {
                return new Group(this.GrammarNode, this.Location, next, this.Child);
            }
        }

        public class Terminal : ParsedNode, IParsingTreeTerminal
        {
            public Location From { get; private set; }
            public Location To { get; private set; }
            public string Content { get; private set; }

            public Terminal(ParserNode node, Location loc, ParsedNode next, Location from, Location to, string content)
                : base(node, loc, next)
            {
                this.From = from;
                this.To = to;
                this.Content = content;
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitTerminal(this);
            }

            protected override ParsedNode UpdateNextImpl(ParsedNode next)
            {
                return new Terminal(this.GrammarNode, this.Location, next, this.From, this.To, this.Content);
            }

            public override string ToString()
            {
                return this.Content;
            }
        }

        #endregion

        #region temporary reparsing impls

        public class TemporaryReparsingGroup : TemporaryGroup, IParsingTreeGroup
        {
            //public override IEnumerable<IParsingTreeNode> Childs
            //{
            //    get
            //    {
            //        var reparsedChilds = base.Childs.ToArray();

            //        var loc = reparsedChilds.Length > 0 ? reparsedChilds.First().Location : this.ReparsingFrom;
            //        foreach (var item in this.OldGroup.Childs)
            //        {
            //            if (item.Location < loc && item.Location < this.ReparsingFrom)
            //                yield return item;
            //            else
            //                break;
            //        }

            //        foreach (var item in reparsedChilds)
            //            yield return item;
            //    }
            //}

            public override IEnumerable<IParsingTreeNode> Childs
            {
                get
                {
                    IParsingTreeNode node = null;

                    var parsedToLoc = this.Child == null ? this.ReparsingTo : this.Child.Location;
                    var oldChilds = this.OldGroup.Childs.ToArray();

                    var index = oldChilds.Length - 1;
                    for (; index >= 0; index--)
                    {
                        var item = oldChilds[index];
                        if (item.Location >= parsedToLoc && item.Location >= this.ReparsingTo)
                        {
                            node = item;
                            yield return item;
                        }
                        else
                            break;
                    }

                    if (node != null && this.Child != null && node.Location <= this.Child.Location)
                        throw new NotImplementedException("");

                    for (var n = this.Child; n != null; n = n.Prev)
                    {
                        node = n;
                        yield return node;
                    }

                    var parsedFromLoc = node == null ? this.ReparsingFrom : node.Location;

                    for (; index >= 0; index--)
                    {
                        var item = oldChilds[index];
                        if (item.Location < parsedFromLoc && item.Location < this.ReparsingFrom)
                            yield return item;
                    }
                }
            }

            public override int ChildsCount
            {
                get
                {
                    return this.Childs.Count();
                }
            }

            public IParsingTreeGroup OldGroup { get; private set; }
            public Location ReparsingFrom { get; private set; }
            public Location ReparsingTo { get; private set; }

            public TemporaryReparsingGroup(IParsingTreeNode oldNode, ParserNode node, Location fromLoc, Location toLoc, TemporaryGroup parent, TemporaryChildNode child)
                : base(node, oldNode.Location, parent, child)
            {
                this.OldGroup = oldNode as IParsingTreeGroup;
                this.ReparsingFrom = fromLoc;
                this.ReparsingTo = toLoc;

                if (this.OldGroup == null)
                    throw new ReparsingFailException();
            }

            protected override TemporaryGroup UpdateChildsImpl(TemporaryChildNode child)
            {
                return new TemporaryReparsingGroup(
                    this.OldGroup,
                    this.GrammarNode,
                    this.ReparsingFrom,
                    this.ReparsingTo,
                    this.Parent,
                    child
                );
            }

            protected override void VisitImpl(IParsingTreeNodeVisitor visitor)
            {
                visitor.VisitGroup(this);
            }

            protected override TemporaryChildNode RecreateImpl()
            {
                return new TemporaryGroupChildGroup(
                    this.GrammarNode, 
                    this.Location, 
                    this.Parent == null ? null : this.Parent.Child, 
                    this.RecreateChilds()
                );
            }

            //private ParsedNode OldRecreateChilds()
            //{
            //    ParsedNode node = null;

            //    for (var n = this.Child; n != null; n = n.Prev)
            //        node = n.Recreate(node);

            //    var loc = node.Location;
            //    var prepends = new List<ParsedNode>();
            //    foreach (ParsedNode item in this.OldGroup.Childs)
            //    {
            //        if (item.Location < loc && item.Location < this.ReparsingFrom)
            //            prepends.Add(item);
            //        else
            //            break;
            //    }

            //    for (int i = prepends.Count - 1; i >= 0; i--)
            //        node = prepends[i].UpdateNext(node);

            //    return node;
            //}

            protected override bool TryReconstructImpl(ParserNode node, Location loc, out TemporaryGroup reconstructedRoot)
            {
                if (loc >= this.ReparsingTo) // reuse right
                {
                    var it = this.OldGroup.Childs.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var oldChild = it.Current;
                        if (oldChild.Location == loc && (oldChild as ParsingTreeNode).GrammarNode == node)
                        {
                            // reconstruct all the rest of the tree
                            reconstructedRoot = this.ReconstructTree();
                            return true;
                        }
                    }
                }

                reconstructedRoot = null;
                return false;
            }

            TemporaryGroup ReconstructTree()
            {
                TemporaryGroup g = this;
                while (g.Parent != null)
                {
                    g = g.ExitChild();
                }

                return g;
            }

            private ParsedNode RecreateChilds()
            {
                ParsedNode node = null;

                var parsedToLoc = this.Child == null ? this.ReparsingTo : this.Child.Location;
                var oldChilds = this.OldGroup.Childs.ToArray();

                var index = oldChilds.Length - 1;
                for (; index >= 0; index--)
                {
                    var item = (ParsedNode)oldChilds[index];
                    if (item.Location >= parsedToLoc && item.Location >= this.ReparsingTo)
                        node = item;
                    else
                        break;
                }

                // TODO: [Portable.Parser.Impl.ParsingTreeNode.TemporaryReparsingGroup.RecreateChilds] wtf?
                //if (node != null && this.Child != null && node.Location <= this.Child.Location)
                //    throw new NotImplementedException("");

                for (var n = this.Child; n != null; n = n.Prev)
                    node = n.Recreate(node);

                var parsedFromLoc = node == null ? this.ReparsingFrom : node.Location;

                for (; index >= 0; index--)
                {
                    var item = (ParsedNode)oldChilds[index];
                    if (item.Location < parsedFromLoc && item.Location < this.ReparsingFrom)
                        node = item.UpdateNext(node);
                }

                return node;
            }
        }

        #endregion
    }

    [Serializable]
    public class ReparsingFailException : Exception
    {
        public ReparsingFailException() { }
        public ReparsingFailException(string message) : base(message) { }
        public ReparsingFailException(string message, Exception inner) : base(message, inner) { }
        protected ReparsingFailException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
