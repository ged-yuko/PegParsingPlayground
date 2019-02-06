using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ParserImpl.Xml.Model;

namespace ParserImpl.Xml
{
    public class XmlModelNavigator : XmlModelNavigatorBase
    {
        public XmlElement CurrElement { get { return this.CurrContext.CurrElement; } }
        public string CurrElementNamespace { get { return this.CurrContext.CurrElementNamespace; } }
        public int CurrElementChildsCount { get { return this.CurrContext.CurrElement.Childs.Count; } }

        XmlDocument _doc;

        Stack<NavContext> _state = new Stack<NavContext>();

        private NavContext CurrContext { get { return _state.Peek(); } }

        public XmlModelNavigator(XmlDocument doc, string rootNodeNamespaceName, params string[] rootNodeLocalNames)
        {
            if (doc == null || rootNodeLocalNames == null || rootNodeLocalNames.Length < 1 || string.IsNullOrWhiteSpace(rootNodeNamespaceName))
                throw new ArgumentNullException();

            var state = new NavContext(doc.RootElement);
            if (state.CurrElementNamespace != rootNodeNamespaceName ||
                rootNodeLocalNames.All(n => doc.RootElement.Name.LocalName != n))
                throw new InvalidOperationException("Invalid root element!");

            _doc = doc;
            _state.Push(state);
        }

        public bool TryEnterElement(int index, string localName)
        {
            return this.TryEnterElement(index, this.CurrElementNamespace, localName);
        }

        public bool TryEnterElement(int index, string nsName, string localName)
        {
            var st = _state.Peek();
            var cctx = st.ChildContexts[index];
            bool ok;

            if (cctx != null && cctx.CurrElement.Name.LocalName == localName && cctx.CurrElementNamespace == nsName)
            {
                _state.Push(cctx);
                ok = true;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        public void ExitElement()
        {
            if (_state.Count < 2)
                throw new InvalidOperationException();

            _state.Pop();
        }

        public XmlModelAttributesAnalyzer AnalyzeCurrentAttributes()
        {
            return new XmlModelAttributesAnalyzer(this.CurrContext);
        }

        public XmlModelElementAnalyzer AnalyzeCurrentElement()
        {
            return new XmlModelElementAnalyzer(new XmlModelItemAnalyzer(this.CurrContext));
        }

        public XmlModelItemAnalyzer AnalyzeCurrentItem()
        {
            return new XmlModelItemAnalyzer(this.CurrContext);
        }
    }
}
