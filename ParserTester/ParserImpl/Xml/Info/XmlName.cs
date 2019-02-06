using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace ParserImpl.Xml.Info
{
    public struct XmlName
    {
        private readonly string _namespace;
        private readonly string _localName;

        public string Namespace { get { return _namespace; } }
        public string LocalName { get { return _localName; } }

        public XmlName(string nsName, string localName)
        {
            if (nsName == null)
                throw new ArgumentException();
            if (!SecurityElement.IsValidAttributeName(localName))
                throw new ArgumentException();

            _namespace = nsName;
            _localName = localName;
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}", _namespace, _localName);
        }
    }
}
