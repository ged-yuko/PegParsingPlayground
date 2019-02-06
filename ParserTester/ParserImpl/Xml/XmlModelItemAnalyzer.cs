using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Xml.Info;
using ParserImpl.Xml.Model;

namespace ParserImpl.Xml
{

    public class XmlModelItemAnalyzer : XmlModelNavigatorBase, IEnumerator<XmlModelItemAnalyzer>, IEnumerable<XmlModelItemAnalyzer>
    {
        NavContext _context;
        int _childElementIndex;
        bool _disposed = false;

        internal NavContext Context { get { return _context; } }

        public string ElementLocalName { get { return _context.CurrElement.Name.LocalName; } }
        public string ElementNamespace { get { return _context.CurrElementNamespace; } }
        public XmlName ElementName { get { return new XmlName(this.ElementNamespace, this.ElementLocalName); } }

        public XmlModelItemAnalyzer Current { get; private set; }
        object System.Collections.IEnumerator.Current { get { return this.Current; } }

        public bool IsElementAnalyzer { get { return _context != null; } }
        public string Text { get; private set; }

        internal XmlModelItemAnalyzer(NavContext context)
        {
            if (context == null)
                throw new ArgumentNullException();

            _context = context;
            _childElementIndex = 0;

            this.Text = null;
        }

        internal XmlModelItemAnalyzer(string text)
        {
            _context = null;
            this.Text = text;
        }

        public XmlModelAttributesAnalyzer AnalyzeCurrentAttributes()
        {
            return new XmlModelAttributesAnalyzer(_context);
        }

        public bool MoveNext()
        {
            if (_disposed)
                throw new ObjectDisposedException("XmlModelItemAnalyzer");

            bool ok;

            if (_childElementIndex < _context.CurrElement.Childs.Count)
            {
                var item = _context.CurrElement.Childs[_childElementIndex];

                while (item is XmlComment)
                {
                    _childElementIndex++;
                    item = _context.CurrElement.Childs[_childElementIndex];
                }

                if (item is XmlElement)
                {
                    this.Current = new XmlModelItemAnalyzer(_context.ChildContexts[_childElementIndex]);
                }
                else
                {
                    var text = item is XmlText ? (item as XmlText).Text :
                               item is XmlCData ? (item as XmlCData).Content :
                               null;

                    if (text == null)
                        throw new NotImplementedException(string.Format("Unknown xml item [{0}]", item));

                    this.Current = new XmlModelItemAnalyzer(text);
                }

                _childElementIndex++;
                ok = true;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        public void Reset()
        {
            if (_disposed)
                throw new ObjectDisposedException("XmlModelItemAnalyzer");

            _childElementIndex = 0;
        }

        public void Dispose()
        {
            _disposed = true;
            this.Current = null;
        }

        bool _selfEnumerated = false;

        public IEnumerator<XmlModelItemAnalyzer> GetEnumerator()
        {
            if (!_selfEnumerated)
            {
                _selfEnumerated = true;
                return this;
            }
            else
            {
                return new XmlModelItemAnalyzer(_context);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}
