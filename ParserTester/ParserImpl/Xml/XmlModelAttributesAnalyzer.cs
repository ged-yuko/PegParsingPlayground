using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Xml.Info;
using ParserImpl.Xml.Model;

namespace ParserImpl.Xml
{
    public class XmlModelAttributesAnalyzer : XmlModelNavigatorBase
    {
        NavContext _context;
        Dictionary<string, List<XmlAttribute>> _attrsByNamespace;

        public XmlElement Element { get { return _context.CurrElement; } }
        public string ElementNamespace { get { return _context.CurrElementNamespace; } }

        public string this[string localName] { get { return this[_context.CurrElementNamespace, localName]; } }

        public string this[string nsName, string localName]
        {
            get
            {
                string value;
                if (!this.TryGetAttribute(nsName, localName, out value))
                    throw new InvalidOperationException(string.Format("Missing attribute [{0}, {1}]", nsName, localName));

                return localName;
            }
        }

        internal XmlModelAttributesAnalyzer(NavContext context)
        {
            if (context == null)
                throw new ArgumentNullException();

            _context = context;
            _attrsByNamespace = context.CurrElement.Attributes.GroupBy(a => context.ResolveNsPrefix(a.Name.Prefix))
                                       .ToDictionary(ag => ag.Key, ag => ag.ToList());
        }

        public int CountRestAttributes()
        {
            return _attrsByNamespace.Values.Sum(l => l.Count);
        }

        public bool TryGetAttribute(string localName, out string attrValue, bool removeAttr = true)
        {
            return this.TryGetAttribute(_context.CurrElementNamespace, localName, out attrValue, removeAttr);
        }

        public bool TryGetAttribute(string nsName, string localName, out string attrValue, bool removeAttr = true)
        {
            bool ok;

            List<XmlAttribute> attrs;
            if (_attrsByNamespace.TryGetValue(nsName, out attrs))
            {
                var index = attrs.IndexOf(a => a.Name.LocalName == localName);

                if (index >= 0)
                {
                    attrValue = attrs[index].Value;

                    if (removeAttr)
                        attrs.RemoveAt(index);

                    ok = true;
                }
                else
                {
                    attrValue = null;
                    ok = false;
                }
            }
            else
            {
                attrValue = null;
                ok = false;
            }

            return ok;
        }

        public XmlAnyAttributeInfo[] GetRestAttributesInfo()
        {
            return _attrsByNamespace.SelectMany(kv => kv.Value.Select(a => new XmlAnyAttributeInfo(new XmlName(kv.Key, a.Name.LocalName), a.Value))).ToArray();
        }
    }
}
