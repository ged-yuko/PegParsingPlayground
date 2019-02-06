using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Impl
{
    abstract class ParserInitialStateFabric<TResult>
        where TResult : class, IParsingResult
    {
        internal ParserInitialStateFabric()
        {
        }

        internal ParserState<TResult> CreateInitialState(ParserNode grammarRoot, ParserNode omitRoot, ISourceTextReader source, TResult oldResult, Location limit, bool enableLog)
        {
            return this.CreateInitialStateImpl(grammarRoot, omitRoot, source, oldResult, limit, enableLog);
        }

        protected abstract ParserState<TResult> CreateInitialStateImpl(ParserNode grammarRoot, ParserNode omitRoot, ISourceTextReader source, TResult oldResult, Location limit, bool enableLog);
    }

    sealed class LinearParserInitialStateFabric : ParserInitialStateFabric<LinearParserState.Result>
    {
        public LinearParserInitialStateFabric()
        {
        }

        protected override ParserState<LinearParserState.Result> CreateInitialStateImpl(ParserNode grammarRoot, ParserNode omitRoot, ISourceTextReader source, LinearParserState.Result oldResult, Location limit, bool enableLog)
        {
            if (oldResult != null)
                throw new NotImplementedException("");

            return LinearParserState.ForStart(grammarRoot, source, enableLog);
        }
    }

    sealed class TreeParserInitialStateFabric : ParserInitialStateFabric<ParserTreeState.Result>
    {
        public TreeParserInitialStateFabric()
        {
        }

        protected override ParserState<ParserTreeState.Result> CreateInitialStateImpl(ParserNode grammarRoot, ParserNode omitRoot,  ISourceTextReader source, ParserTreeState.Result oldResult, Location limit, bool enableLog)
        {
            return oldResult == null
                ? ParserTreeState.ForStart(grammarRoot, omitRoot, source, null, limit, enableLog)
                : ParserTreeState.ForStart(grammarRoot, omitRoot, source, oldResult.Tree, limit, enableLog);
        }
    }
}
