using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Grammar
{
    class GrammarBuilder
    {
        int _idCounter = 0;

        public GrammarBuilder()
        {

        }

        public ExtensibleRule CreateExtensibleRule(string name)
        {
            return new ExtensibleRule(_idCounter++, name);
        }

        public ExplicitRule CreateExplicitRule(string name, RuleExpression expr)
        {
            return new ExplicitRule(_idCounter++, name, expr);
        }
    }
}
