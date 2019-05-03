using ParserImpl;
using ParserImpl.Grammar;

namespace ParserTester
{
    interface IEvolutionalParser
    {
        IParsingTreeNode Parse(IParsingTreeNode node, ISourceTextReader textReader, RuleSet[] currentRules, Location location, bool isDeleting);
    }
}
