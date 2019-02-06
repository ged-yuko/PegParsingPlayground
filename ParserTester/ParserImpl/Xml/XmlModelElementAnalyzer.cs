using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Xml
{
    public class XmlModelElementAnalyzer : XmlModelNavigatorBase
    {
        XmlModelItemAnalyzer _itemAnalyzer;
        XmlModelItemAnalyzer _childItem;

        public bool IsEndReached { get { return _childItem == null; } }

        public string ElementLocalName { get { return _itemAnalyzer.ElementLocalName; } }

        internal XmlModelElementAnalyzer(XmlModelItemAnalyzer itemAnalyzer)
        {
            _itemAnalyzer = itemAnalyzer;
            this.MoveNext();
        }

        public XmlModelAttributesAnalyzer AnalyzeAttributes()
        {
            return new XmlModelAttributesAnalyzer(_itemAnalyzer.Context);
        }

        private void MoveNext()
        {
            _childItem = _itemAnalyzer.MoveNext() ? _itemAnalyzer.Current : null;
        }

        public bool TryGetElement(out XmlModelElementAnalyzer el)
        {
            bool ok;

            if (_childItem.IsElementAnalyzer)
            {
                el = new XmlModelElementAnalyzer(_childItem);
                this.MoveNext();
                ok = true;
            }
            else
            {
                el = null;
                ok = false;
            }

            return ok;
        }

        public bool TryGetElement(string localName, out XmlModelElementAnalyzer el)
        {
            return this.TryGetElement(_itemAnalyzer.ElementNamespace, localName, out el);
        }

        public bool TryGetElement(string nsName, string localName, out XmlModelElementAnalyzer el)
        {
            bool ok;

            if (_childItem.IsElementAnalyzer && _childItem.ElementNamespace == nsName && _childItem.ElementLocalName == localName)
            {
                el = new XmlModelElementAnalyzer(_childItem);
                this.MoveNext();
                ok = true;
            }
            else
            {
                el = null;
                ok = false;
            }

            return ok;
        }

        public bool TryGetText(out string text)
        {
            bool ok;

            if (!_childItem.IsElementAnalyzer && _childItem.Text != null)
            {
                text = _childItem.Text;
                ok = true;
            }
            else
            {
                text = null;
                ok = false;
            }

            return ok;
        }
    }
}
