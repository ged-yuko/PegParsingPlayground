using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;

namespace ParserImpl.Impl
{
    
    class Parser<TResult> : IParser<TResult>
        where TResult : class, IParsingResult
    {
        AnalyzerGraphInfo _analyzerInfo;
        readonly ParserInitialStateFabric<TResult> _initialStateFabric;

        public TimeSpan ParsingStatistics { get; private set; }
        public bool EnableLog { get; set; }
        public bool MaterializeOmittedFragments { get; set; }
        public bool UseDelayedStates { get; set; }
        public bool RestoreRewritedRecursion { get; set; }
        public RuleSet[] RuleSets { get { return _analyzerInfo.RuleSets; } }

        public Parser(AnalyzerGraphInfo analyzerInfo, ParserInitialStateFabric<TResult> initialStateFabric)
        {
            _analyzerInfo = analyzerInfo;
            _initialStateFabric = initialStateFabric;
            
            this.EnableLog = false;
            this.MaterializeOmittedFragments = false;
            this.UseDelayedStates = false;
        }

        public TResult Parse(ISourceTextReader source)
        {
            var ctx = new ParserContext<TResult>(this, _analyzerInfo, source, _initialStateFabric, null, source.TextEndLocation);
            
            var sw = new Stopwatch();
            sw.Start();
            
            var result = ctx.Parse();
            
            sw.Stop();
            this.ParsingStatistics = sw.Elapsed;

            return result;
        }

        public TResult ReParse(ISourceTextReader source, IParsingResult oldResult, Location limit)
        {
            if (!(oldResult is TResult realOldResult))
                throw new ArgumentException();

            var ctx = new ParserContext<TResult>(this, _analyzerInfo, source, _initialStateFabric, realOldResult, limit);

            var sw = new Stopwatch();
            sw.Start();

            var result = ctx.Parse();

            sw.Stop();
            this.ParsingStatistics = sw.Elapsed;

            return result;
        }
    }
}
