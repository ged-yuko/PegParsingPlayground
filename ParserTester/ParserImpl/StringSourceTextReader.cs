using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl
{
    // TODO: [Portable.Parser.StringSourceTextReader] debug it
    public class StringSourceTextReader : ISourceTextReader, ISourceTextReaderHolder
    {
        public Location Location { get { return new Location(_line, _column); } }
        public Location TextEndLocation { get { return new Location(_maxLine, _text.Length - _lineBreaks[_lineBreaks.Length - 1]); } }
        public char Character { get { return _pos == _text.Length ? char.MaxValue : _text[_pos]; } }


        readonly string _text;
        readonly char _newLine;
        readonly int _maxLine;
        readonly int[] _lineBreaks;

        int _pos, _line, _column;

        ISourceTextReaderHolder _holder = null;

        public ITreeParsingResult ParsingResult { get; private set; }

        public StringSourceTextReader(string text)
        {
            if (text == null)
                throw new ArgumentNullException();

            _pos = 0;
            _text = text;
            _line = 0;
            _column = 0;
            _newLine = '\n';
            _maxLine = _text.Count(c => c == _newLine);

            var positions = new List<int>();
            positions.Add(0);

            var i = text.IndexOf(_newLine);
            while (i >= 0)
            {
                positions.Add(i + 1);
                i = text.IndexOf(_newLine, i + 1);
            }
            _lineBreaks = positions.ToArray();
        }

        public StringSourceTextReader(StringSourceTextReader r)
        {
            _pos = r._pos;
            _text = r._text;
            _line = r._line;
            _column = r._column;
            _newLine = r._newLine;
            _maxLine = r._maxLine;
            _lineBreaks = r._lineBreaks;
        }

        public bool MoveNext()
        {
            return this.Move(1);
        }

        public bool MovePrev()
        {
            return this.Move(-1);
        }

        public int GetPosition(Location location)
        {
            if (!this.TryGetPosition(location, out int ret))
                throw new ArgumentOutOfRangeException();

            return ret;
        }

        bool TryGetPosition(Location location, out int pos)
        {
            if (location.Line > _maxLine)
            {
                pos = -1;
                return false;
            }

            pos = _lineBreaks[location.Line] + location.Column;
            if (pos < 0 || pos > _text.Length)
                return false;

            return true;
        }

        public bool MoveTo(Location location)
        {
            int pos;
            if (this.TryGetPosition(location, out pos))
            {
                _pos = pos;
                _line = location.Line;
                _column = location.Column;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Move(int offset)
        {
            if (offset == 0)
                return true;

            int i, j;
            int step = Math.Sign(offset);
            int count = Math.Abs(offset);

            var line = _line;
            var column = _column;

            for (i = _pos, j = 0; (i < _text.Length || step < 0) && i >= 0 && j < count; i += step, j++, column += step)
            {
                if (i < _text.Length && _text[i] == _newLine)
                {
                    line += step;
                    column = step > 0 ? -step : this.FindLineStart(i - 1);
                }
            }

            if (column < 0)
                return false;

            bool ok;
            if (i <= _text.Length && i >= 0)
            {
                ok = true;
                _holder = null;
                _pos = i;
                _line = line;
                _column = column;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        private int FindLineStart(int pos)
        {
            var i = pos;
            var cnt = 0;

            while (i >= 0)
            {
                if (_text[i] == _newLine)
                    return cnt;

                i--;
                cnt++;
            }

            return cnt;
        }


        public string GetText()
        {
            return _text;
        }

        public int GetPosition()
        {
            return _pos;
        }

        public ISourceTextReaderHolder Clone()
        {
            if (_holder == null)
            {
                _holder = new StringSourceTextReader(this);
            }

            return _holder;
        }

        ISourceTextReader ISourceTextReaderHolder.GetReader()
        {
            return new StringSourceTextReader(this);
        }

        public string GetText(Location from, Location to)
        {
            var p1 = this.GetPosition(from);
            var p2 = this.GetPosition(to);
            return _text.Substring(p1, p2 - p1);
        }
    }
}
