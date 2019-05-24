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
        public IEnumerable<IParsingTreeTerminal> FindAffectedNode(IParsingTreeNode node, Location location)
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
                            yield return terminal;
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
        }

        public IParsingTreeNode Parse(IParsingTreeNode tree, ISourceTextReader textReader, RuleSet[] currentRules, Location location, bool isDeleting)
        {
            // move to previos character of text
            textReader.MoveTo(location);
            textReader.MovePrev();

            IEnumerable<IParsingTreeTerminal> affectedNodes = FindAffectedNode(tree, textReader.Location);

            foreach (var affectedNode in affectedNodes)
            {
                ISourceTextReader oldContentReader = new StringSourceTextReader(affectedNode.Content);
                string renewedContent;
                Location newToLocation;

                if (isDeleting)
                {
                    renewedContent = TryRemoveChar(textReader, location, affectedNode, oldContentReader, out newToLocation);

                    if (renewedContent is null)
                    {
                        continue;
                    }
                }
                else
                {
                    renewedContent = TryAddChar(textReader, location, affectedNode, oldContentReader, out newToLocation);
                }

                if (IsUpdatedContentMatсhOldNode(affectedNode, renewedContent))
                {
                    var replacement = new ReplacedNode(affectedNode, renewedContent, affectedNode.From, newToLocation);
                    return NodeWithReplacedNode(tree, affectedNode, replacement, location, newToLocation - affectedNode.To);
                }
            }

            return RebuildTreeFromLocation(tree, textReader, currentRules, location);
        }

        private IParsingTreeNode RebuildTreeFromLocation(IParsingTreeNode tree, ISourceTextReader textReader, RuleSet[] currentRules, Location location)
        {
            throw new NotImplementedException();
        }

        private string TryRemoveChar(ISourceTextReader textReader, Location location, IParsingTreeTerminal affectedNode, ISourceTextReader oldNodeContentReader, out Location newToLocation)
        {
            newToLocation = new Location(affectedNode.To.Line, affectedNode.To.Column - 1);

            oldNodeContentReader.MoveTo(location - affectedNode.Location);

            try
            {
                return affectedNode.Content.Remove(oldNodeContentReader.GetPosition(), 1);
            }
            catch
            {
                return null;
            }
        }

        private static string TryAddChar(ISourceTextReader textReader, Location location, IParsingTreeTerminal affectedNode, ISourceTextReader oldNodeContentReader, out Location newToLocation)
        {
            textReader.MoveTo(location);
            textReader.MovePrev();
            char addedChar = textReader.Character;

            // TODO: Newlines
            oldNodeContentReader.MoveTo(location - affectedNode.Location - new Location(0, 1));
            if (oldNodeContentReader.GetPosition() > 0)
            {
                oldNodeContentReader.MovePrev();
            }
            
            newToLocation = new Location(affectedNode.To.Line, affectedNode.To.Column + 1);

            return affectedNode.Content.Insert(oldNodeContentReader.GetPosition(), addedChar.ToString());
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

        private bool IsUpdatedContentMatсhOldNode(IParsingTreeNode affectedNode, string renewedContent)
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
