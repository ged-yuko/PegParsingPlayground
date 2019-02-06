using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl
{
    public static class Parsers
    {
        public static IParserFabric CreateFabric(RuleSet ruleSet)
        {
            return CreateFabric(ruleSet.Name, ruleSet);
        }

        public static IParserFabric CreateFabric(string rootRuleSetName, params RuleSet[] ruleSets)
        {
            return new Impl.ParserFabric(rootRuleSetName, ruleSets);
        }
    }
}
