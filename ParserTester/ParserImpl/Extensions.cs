using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;
using ParserImpl.Impl;

namespace ParserImpl
{
    public static class Extensions
    {
        public static IEnumerable<IParsingTreeNode> GetRuleChilds(this IParsingTreeGroup node, bool disableExpanding = false, bool skipOmitFragments = true)
        {
            return node.GetRuleChildsImpl(disableExpanding, skipOmitFragments, 0);
        }

        static IEnumerable<IParsingTreeNode> GetRuleChildsImpl(this IParsingTreeGroup node, bool disableExpanding, bool skipOmitFragments, int depth)
        {
            foreach (var item in node.Childs)
            {
                //if (item is IParsingTreeTerminal || (
                //    node.Expression is RuleExpression.RuleUsage && (
                //        item.Rule != node.Rule && !item.Rule.IsExpandable
                //    )
                //))

                //if (skipOmitFragments && item.Rule == null && depth > 1)
                //{
                //    continue;
                //}
                if ((item as IParsingTreeNode).GrammarNode is ParserNode.RecursiveParserNode)
                {
                    foreach (var subitem in (item as IParsingTreeGroup).GetRuleChildsImpl(disableExpanding, skipOmitFragments, depth + 1))
                        yield return subitem;
                }
                else if (!disableExpanding && node.Expression is RuleExpression.RuleUsage && item.Rule != node.Rule && item.Rule != null && !item.Rule.IsExpandable)
                {
                    yield return item;
                }
                else if (disableExpanding && node.Expression is RuleExpression.RuleUsage && item.Rule != node.Rule && item.Rule != null)
                {
                    yield return item;
                }
                else if (!disableExpanding && (item is IParsingTreeTerminal))
                {
                    if (item.Rule != null && !item.Rule.IsExpandable)
                        yield return item;
                }
                else if (disableExpanding && (item is IParsingTreeTerminal))
                {
                    yield return item;
                }
                else if (item is IParsingTreeGroup)
                {
                    foreach (var subitem in (item as IParsingTreeGroup).GetRuleChildsImpl(disableExpanding, skipOmitFragments, depth + 1))
                        yield return subitem;
                }
                else
                {
                    throw new NotImplementedException("");
                }
            }
        }

        public static IEnumerable<IParsingTreeNode> EnumerateRuleChilds(this IParsingTreeNode node, bool disableExpanding = false, bool skipOmitFragments = true)
        {
            if (node is IParsingTreeTerminal)
                throw new InvalidOperationException();

            var g = node as IParsingTreeGroup;
            if (g == null)
                throw new InvalidOperationException();

            return g.GetRuleChilds(disableExpanding, skipOmitFragments);
        }

        internal static IParsingTreeNode[] GetRuleChildsArray(this IParsingTreeNode node)
        {
            if (node is IParsingTreeTerminal)
                throw new InvalidOperationException();

            var g = node as IParsingTreeGroup;
            if (g == null)
                throw new InvalidOperationException();

            return g.GetRuleChilds().ToArray();
        }

        public static string GetContent(this IParsingTreeNode node, ISourceTextReader reader)
        {
            var from = node.GetFromLocation();
            if (from.HasValue)
            {
                var to = node.GetToLocation();
                return reader.GetText(from.Value, to.Value);
            }
            else
            {
                return string.Empty;
            }
        }

        static Location? GetFromLocation(this IParsingTreeNode node)
        {
            var t = FirstTerminalSearcher.FindFirstTerminal(node);
            return t == null ? null : (Location?)t.From;
        }

        static Location? GetToLocation(this IParsingTreeNode node)
        {
            var t = LastTerminalSearcher.FindLastTerminal(node);
            return t == null ? null : (Location?)t.To;
        }

        class FirstTerminalSearcher : IParsingTreeNodeVisitor
        {
            public IParsingTreeTerminal Result { get; private set; }

            int _depth = 0;

            private FirstTerminalSearcher() { }

            public static IParsingTreeTerminal FindFirstTerminal(IParsingTreeNode node)
            {
                var visitor = new FirstTerminalSearcher();
                node.Visit(visitor);
                return visitor.Result;
            }

            void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
            {
                _depth++;

                foreach (var item in group.Childs)
                    if (this.Result == null && group.Rule != null && _depth > 0)
                        item.Visit(this);

                _depth--;
            }

            void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
            {
                this.Result = terminal;
            }
        }

        class LastTerminalSearcher : IParsingTreeNodeVisitor
        {
            public IParsingTreeTerminal Result { get; private set; }

            int _depth = 0;

            private LastTerminalSearcher() { }

            public static IParsingTreeTerminal FindLastTerminal(IParsingTreeNode node)
            {
                var visitor = new LastTerminalSearcher();
                node.Visit(visitor);
                return visitor.Result;
            }

            void IParsingTreeNodeVisitor.VisitGroup(IParsingTreeGroup group)
            {
                _depth++;

                foreach (var item in group.Childs.Reverse())
                    if (this.Result == null && group.Rule != null && _depth > 0)
                        item.Visit(this);
                
                _depth--;
            }

            void IParsingTreeNodeVisitor.VisitTerminal(IParsingTreeTerminal terminal)
            {
                this.Result = terminal;
            }
        }

        private static string FormatTypeNameInternal(this string name, bool capitalizeFirstCharacter)
        {
            var chars = new char[name.Length];

            var prevCharDowngraded = false;
            var prevCharNewWord = false;

            chars[0] = capitalizeFirstCharacter ? char.ToUpper(name[0]) : char.ToLower(name[0]);
            for (int i = 1; i < name.Length; i++)
            {
                var c = name[i];

                if (prevCharDowngraded)
                {
                    prevCharDowngraded = !char.IsUpper(c);
                    c = char.ToLower(c);

                    prevCharNewWord = false;
                }
                else if (prevCharNewWord)
                {
                    prevCharDowngraded = char.IsUpper(c);
                    c = char.ToLower(c);

                    prevCharNewWord = false;
                }
                else if (char.IsUpper(c))
                {
                    prevCharDowngraded = false;
                    prevCharNewWord = true;
                }
                else
                {
                    prevCharDowngraded = false;
                    prevCharNewWord = false;
                }

                chars[i] = c;
            }

            return new string(chars);
        }

        internal static string FormatTypeName(this string namePart)
        {
            return FormatName(namePart) + "Type";
        }

        internal static string FormatTypeName(this IEnumerable<string> nameParts)
        {
            return FormatName(nameParts) + "Type";
        }

        internal static string FormatName(this string namePart)
        {
            return namePart.Split('.').FormatName();
        }

        internal static string FormatName(this IEnumerable<string> nameParts)
        {
            return string.Join(string.Empty, nameParts.Select(s => s.FormatTypeNameInternal(true))).FormatTypeNameInternal(false);
        }

        internal static RuleExpression GetRuleSetAttributeArgument(this GrammarNavigator nav, string attributeName, bool required = true)
        {
            var attr = nav.GetAttributes().FirstOrDefault(a => a.Name == attributeName);
            if (attr == null)
                if (required)
                    throw new InvalidOperationException(string.Format("Attribute [{0}] not found on rule set [{1}]!", attributeName, nav.CurrPath));
                else
                    return null;

            //if (attr.Arguments.Count != 1 || !(attr.Arguments.First() is RuleExpression.RuleUsage))
            if (attr.Arguments.Count != 1)
                throw new InvalidOperationException(string.Format("Attribute [{0}] arguments is invalid on rule set [{1}]!", attributeName, nav.CurrPath));

            var expression = attr.Arguments.First();
            return expression;
        }

    }
}
