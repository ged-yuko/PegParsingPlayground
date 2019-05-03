using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Grammar;
using ParserImpl.Impl;

namespace ParserImpl
{
    #region parser infrastructure

    public interface IParserFabric
    {
        IParser<ILinearParsingResult> CreateLinearParser();
        IParser<ITreeParsingResult> CreateTreeParser();

        string GetDebugInfo();
    }

    public interface IParser<out TResult>
        where TResult : class, IParsingResult
    {
        TimeSpan ParsingStatistics { get; }
        bool EnableLog { get; set; }
        bool MaterializeOmittedFragments { get; set; }
        bool UseDelayedStates { get; set; }
        bool RestoreRewritedRecursion { get; set; }

        // parse source text from scratch
        TResult Parse(ISourceTextReader source);

        // incrementally parse source text from current location with information reusage
        TResult ReParse(ISourceTextReader source, IParsingResult oldResult, Location limit);
    }

    public interface IParsingResult
    {
        bool AllTextParsed { get; }
        bool Successed { get; }
        // IParsingTreeNode Tree { get; }
        // IParsingTreeNode[] Trees { get; }

        string GetDebugInfo();
    }

    public interface ISourceTextReaderHolder
    {
        ISourceTextReader GetReader();
    }

    public interface ISourceTextReader
    {
        Location Location { get; }
        char Character { get; }

        bool MoveNext();
        bool MovePrev();
        bool Move(int offset);
        bool MoveTo(Location location);

        string GetText();
        int GetPosition();

        ISourceTextReaderHolder Clone();

        Location TextEndLocation { get; }

        string GetText(Location from, Location to);
    }
        
    #endregion

    #region linear info

    public interface ILinearParsingResult : IParsingResult
    {
        IParserStep[] Steps { get; }
    }

    public interface IParserStepVisitor
    {
        void VisitTerminal(ITerminalStep terminal);

        void VisitEnterNode(IEnterStep enterNode);

        void VisitExitNode(IExitStep exitNode);
    }

    public interface IParserStep
    {
        IParserStep Prev { get; }

        Rule Rule { get; }
        RuleExpression Expression { get; }

        void Visit(IParserStepVisitor visitor);
    }

    public interface ITerminalStep : IParserStep
    {
        Location From { get; }
        Location To { get; }
    }

    public interface IEnterStep : IParserStep
    {
    }

    public interface IExitStep : IParserStep
    {
    }

    #endregion

    #region tree info

    public interface ITreeParsingResult : IParsingResult
    {
        IParsingTreeNode Tree { get; }
        IParsingTreeNode[] Trees { get; }
    }

    public interface IParsingTreeNodeVisitor
    {
        void VisitGroup(IParsingTreeGroup group);

        void VisitTerminal(IParsingTreeTerminal terminal);
    }

    public interface IParsingTreeNode
    {
        ParserNode GrammarNode { get; }
        Location Location { get; }

        Rule Rule { get; }
        RuleExpression Expression { get; }

        void Visit(IParsingTreeNodeVisitor visitor);
    }

    public interface IParsingTreeTerminal : IParsingTreeNode
    {
        Location From { get; }
        Location To { get; }
        string Content { get; }
    }

    public interface IParsingTreeGroup : IParsingTreeNode
    {
        IEnumerable<IParsingTreeNode> Childs { get; }
        int ChildsCount { get; }
    }

    #endregion

    public struct ParserResultId : IComparable<ParserResultId>
    {
        uint _value;

        internal ParserResultId(uint value)
        {
            _value = value;
        }

        public int CompareTo(ParserResultId other)
        {
            return this._value.CompareTo(other._value);
        }
    }
}
