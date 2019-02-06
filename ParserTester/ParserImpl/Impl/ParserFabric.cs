using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    class ParserFabric : IParserFabric
    {
        public string RootRuleSetName { get; private set; }
        public ReadOnlyCollection<RuleSet> RuleSets { get; private set; }

        AnalyzerGraphInfo _analyzerGraph;

        public ParserFabric(string rootRuleSetName, params RuleSet[] ruleSets)
        {
            this.RootRuleSetName = rootRuleSetName;
            this.RuleSets = new ReadOnlyCollection<RuleSet>(ruleSets.EmptyCollectionIfNull());

            _analyzerGraph = AnalyzerGraphBuilder.BuildGraph(rootRuleSetName, ruleSets);
        }

        public string GetDebugInfo()
        {
            string result = string.Empty;

            if (_analyzerGraph.OmitGraph != null)
            {
                result += string.Format("Omit: {0}{1}", Environment.NewLine, ParserGraphPrinter.Collect(_analyzerGraph.OmitGraph));
            }

            result += Environment.NewLine;
            result += string.Format("Analyzer: {0}{1}", Environment.NewLine, ParserGraphPrinter.Collect(_analyzerGraph.AnalyzerGraph));

            return result;
        }

        public IParser<ILinearParsingResult> CreateLinearParser()
        {
            return new Parser<LinearParserState.Result>(_analyzerGraph, new LinearParserInitialStateFabric());
        }

        public IParser<ITreeParsingResult> CreateTreeParser()
        {
            return new Parser<ParserTreeState.Result>(_analyzerGraph, new TreeParserInitialStateFabric());
        }
    }
}
