using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserImpl.Impl
{
    abstract class FSM
    {
        public FSM()
        {
        }

        public bool TryRun(ISourceTextReader reader, out string text)
        {
            return this.TryRunImpl(reader, out text);
        }

        protected abstract bool TryRunImpl(ISourceTextReader reader, out string text);

        public abstract override string ToString();

        #region custom impls

        class AnyCharFSM : FSM
        {
            public AnyCharFSM()
            {
            }

            protected override bool TryRunImpl(ISourceTextReader reader, out string text)
            {
                text = reader.Character.ToString();
                return reader.Move(1);
            }

            public override string ToString()
            {
                return "FSM [.]";
            }
        }

        class CharsSeqFSM : FSM
        {
            public string Characters { get; private set; }

            public CharsSeqFSM(string characters)
            {
                this.Characters = characters;
            }

            protected override bool TryRunImpl(ISourceTextReader reader, out string text)
            {
                int index = 0;

                for (; index < this.Characters.Length; index++)
                {
                    if (reader.Character == this.Characters[index])
                        if (reader.Move(1))
                            continue;

                    break;
                }

                bool result;
                if (index == this.Characters.Length)
                {
                    text = this.Characters;
                    result = true;
                }
                else
                {
                    if (index > 0)
                        if (!reader.Move(-index))
                            throw new InvalidOperationException();

                    text = null;
                    result = false;
                }

                return result;
            }

            public override string ToString()
            {
                return string.Format("FSM ['{0}']", this.Characters);
            }
        }

        class RegexHack : FSM
        {
            public string Pattern { get; private set; }

            Regex _regex;

            public RegexHack(string pattern)
            {
                this.Pattern = pattern;
                _regex = new Regex(pattern);
            }

            protected override bool TryRunImpl(ISourceTextReader reader, out string content)
            {
                bool result;
                var text = reader.GetText();
                var pos = reader.GetPosition();

                var match = _regex.Match(text, pos);
                if (match.Success && match.Index == pos)
                {
                    content = match.Value;
                    result = reader.Move(match.Length);
                }
                else
                {
                    content = null;
                    result = false;
                }

                return result;
            }

            public override string ToString()
            {
                return string.Format("FSM [\"{0}\"]", this.Pattern);
            }
        }

        #endregion

        public static FSM AnyChar()
        {
            // return new RegexHack(".");
            return new AnyCharFSM();
        }

        public static FSM FromPattern(string pattern)
        {
            return new RegexHack(pattern);
        }

        public static FSM FromCharsSequence(string characters)
        {
            // return new RegexHack(Regex.Escape(characters));
            return new CharsSeqFSM(characters);
        }
    }
}
