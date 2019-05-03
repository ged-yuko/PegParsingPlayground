using ParserImpl;
using ParserImpl.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParserTester
{
    public class EvolutionalParser : IEvolutionalParser
    {
        public IParsingTreeTerminal FindAffectedNode(IParsingTreeNode node, Location location)
        {
            Stack<IParsingTreeNode> nodesStack = new Stack<IParsingTreeNode>();
            nodesStack.Push(node);

            while (nodesStack.Count > 0)
            {
                switch (nodesStack.Pop())
                {
                    case IParsingTreeTerminal terminal:
                        if (terminal.Location <= location && terminal.To >= location)
                        {
                            return terminal;
                        }
                        break;

                    case IParsingTreeGroup group:
                        foreach (IParsingTreeNode child in group.Childs)
                        {
                            nodesStack.Push(child);
                        }

                        break;
                }
            }

            return null;
        }

        public IParsingTreeNode Parse(IParsingTreeNode tree, ISourceTextReader textReader, RuleSet[] currentRules, Location location, bool isDeleting)
        {
            // move to previos character of text
            textReader.MoveTo(location);
            textReader.MovePrev();

            IParsingTreeTerminal affectedNode = FindAffectedNode(tree, textReader.Location);
            ISourceTextReader oldContentReader = new StringSourceTextReader(affectedNode.Content);
            string renewedContent;
            char addedChar;
            Location newToLocation;

            if (isDeleting)
            {
                throw new NotImplementedException();
            }
            else
            {
                // inserting new character
                textReader.MoveTo(location);
                textReader.MovePrev();
                addedChar = textReader.Character;

                // TODO: Newlines
                oldContentReader.MoveTo(location - affectedNode.Location - new Location(0, 1));
                if (oldContentReader.GetPosition() > 0)
                {
                    oldContentReader.MovePrev();
                }

                renewedContent = affectedNode.Content.Insert(oldContentReader.GetPosition(), addedChar.ToString());
                newToLocation = new Location(affectedNode.To.Line, affectedNode.To.Column + 1);
            }

            if (IsUpdatedContentMathOldNode(affectedNode, renewedContent))
            {
                var replacement = new ReplacedNode(affectedNode, renewedContent, affectedNode.From, newToLocation);
                return NodeWithReplacedNode(tree, affectedNode, replacement, location, newToLocation - affectedNode.To);
            }

            return tree;
        }

        private IParsingTreeNode NodeWithReplacedNode(IParsingTreeNode node, IParsingTreeNode affectedNode, IParsingTreeNode replacedNode, Location changedAt, Location differ)
        {
            if (node == affectedNode)
            {
                return replacedNode;
            }

            switch (node)
            {
                case IParsingTreeTerminal terminal:
                    if (terminal.From <= replacedNode.Location)
                    {
                        return terminal;
                    }
                    else
                    {
                        return new ReplacedNode(
                            terminal,
                            terminal.Content,
                            terminal.From,
                            terminal.To + (terminal.To.Line == changedAt.Line ? new Location(0, differ.Column) : new Location(differ.Line, 0))
                        );
                    }

                case IParsingTreeGroup group:
                    return new ReplacedGroupNode(
                        group,
                        group.Childs.Select(c => NodeWithReplacedNode(c, affectedNode, replacedNode, changedAt, differ)).Where(c => c != null)
                    );

                default:
                    return null;
            }
        }

        private bool IsUpdatedContentMathOldNode(IParsingTreeNode affectedNode, string renewedContent)
        {
            switch (affectedNode.Expression)
            {
                case RuleExpression.Regex regexRule:
                    Regex regex = new Regex(regexRule.Pattern, RegexOptions.Compiled);
                    Match match = regex.Match(renewedContent);
                    return match.Success && match.Index == 0 && match.Length == renewedContent.Length;
            }

            return false;
        }
    }
}
