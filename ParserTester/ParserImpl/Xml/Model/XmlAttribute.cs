using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace ParserImpl.Xml.Model
{
    public class XmlAttribute
    {
        public XmlNodeName Name { get; private set; }
        public string Value { get; private set; }

        public XmlAttribute(XmlNodeName name, string value)
        {
            if (!SecurityElement.IsValidAttributeValue(value))
                throw new ArgumentException();

            this.Name = name;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("XmlAttribute[{0} = \"{1}\"]", this.Name, this.Value);
        }
    }
}