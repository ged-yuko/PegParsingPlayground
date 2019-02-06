using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace ParserImpl.Xml
{

    public struct XmlNodeName
    {
        private readonly string _prefix;
        private readonly string _localName;

        public string Prefix { get { return _prefix; } }
        public string LocalName { get { return _localName; } }

        public XmlNodeName(string localName)
            : this(null, localName) { }

        public XmlNodeName(string prefix, string localName)
        {
            if (prefix != null && !SecurityElement.IsValidAttributeName(prefix))
                throw new ArgumentException();
            if (!SecurityElement.IsValidAttributeName(localName))
                throw new ArgumentException();

            _prefix = prefix;
            _localName = localName;
        }

        public override string ToString()
        {
            return (_prefix == null) ? _localName : (_prefix + ":" + _localName);
        }

        public static bool operator ==(XmlNodeName a, XmlNodeName b)
        {
            return a._prefix == b._prefix && a._localName == b._prefix;
        }

        public static bool operator !=(XmlNodeName a, XmlNodeName b)
        {
            return a._prefix != b._prefix || a._localName != b._prefix;
        }


    }
}
