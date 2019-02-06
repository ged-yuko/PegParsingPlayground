using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ParserImpl.Xml.Model;

namespace ParserImpl.Xml
{
    public abstract class XmlModelNavigatorBase
    {
        internal class NavContext
        {
            private static readonly string CurrNsPrefix = string.Empty;

            public NavContext PrevContext { get; private set; }

            public XmlElement CurrElement { get; private set; }
            public string CurrElementNamespace { get; private set; }
            public string CurrNamespace { get; private set; }

            Dictionary<string, string> _availableNamespaces;

            ReadOnlyCollection<NavContext> _childs = null;
            public ReadOnlyCollection<NavContext> ChildContexts
            {
                get
                {
                    return _childs ?? (_childs = new ReadOnlyCollection<NavContext>(
                        this.CurrElement.Childs.Select(itm => itm as XmlElement)
                                               .Select(itm => itm == null ? null : new NavContext(this, itm))
                                               .ToList()
                    ));
                }
            }

            public NavContext(XmlElement currElement)
            {
                _availableNamespaces = this.GetNamespaces(currElement);

                string ns;
                if (!_availableNamespaces.TryGetValue(CurrNsPrefix, out ns))
                    ns = null;

                this.CurrElement = currElement;
                this.CurrNamespace = ns;
                this.PrevContext = null;
                this.CurrElementNamespace = this.ResolveNsPrefix(currElement.Name.Prefix);
            }

            public NavContext(NavContext prevContext, XmlElement currElement)
            {
                _availableNamespaces = this.GetNamespaces(currElement);

                string ns;
                if (!_availableNamespaces.TryGetValue(CurrNsPrefix, out ns))
                    ns = prevContext.CurrNamespace;

                this.CurrElement = currElement;
                this.CurrNamespace = ns;
                this.PrevContext = prevContext;
                this.CurrElementNamespace = this.ResolveNsPrefix(currElement.Name.Prefix);
            }

            private Dictionary<string, string> GetNamespaces(XmlElement el)
            {
                return el.Attributes.Where(a => (a.Name.LocalName == "xmlns" && string.IsNullOrWhiteSpace(a.Name.Prefix)) || (a.Name.Prefix == "xmlns"))
                         .ToDictionary(a => string.IsNullOrWhiteSpace(a.Name.Prefix) ? CurrNsPrefix : a.Name.LocalName, a => a.Value);
            }

            public string ResolveNsPrefix(string prefix)
            {
                string ns;

                if (string.IsNullOrWhiteSpace(prefix))
                    prefix = CurrNsPrefix;

                if (!_availableNamespaces.TryGetValue(prefix, out ns))
                {
                    if (this.PrevContext == null)
                        throw new InvalidOperationException(string.Format("Unknown Xml namespace prefix '{0}'", prefix));

                    ns = this.PrevContext.ResolveNsPrefix(prefix);
                }

                return ns;
            }
        }

        internal XmlModelNavigatorBase()
        {
        }
    }
}
