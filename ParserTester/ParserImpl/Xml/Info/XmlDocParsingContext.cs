using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Xml.Info
{
    public interface IXmlElementModelItem
    {
        void Parse(XmlModelElementAnalyzer a, XmlDocParsingContext c);
    }

    public abstract class XmlElementModelItem : IXmlElementModelItem
    {
        public void Parse(XmlModelElementAnalyzer a, XmlDocParsingContext c)
        {
            this.ParseAttributes(a.AnalyzeAttributes(), c);
            this.ParseContent(a, c);
        }

        protected abstract void ParseContent(XmlModelElementAnalyzer a, XmlDocParsingContext c);
        protected abstract void ParseAttributes(XmlModelAttributesAnalyzer a, XmlDocParsingContext c);
    }

    public class XmlAnyAttributeInfo
    {
        public XmlName Name { get; private set; }
        public string Value { get; private set; }

        public XmlAnyAttributeInfo(XmlName name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    public class XmlAnyAttributeValue
    {
        public XmlName Name { get; private set; }
        public object Value { get; private set; }

        public XmlAnyAttributeValue(XmlName name, object value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    public class XmlAnyElementValue
    {
        public XmlName Name { get; private set; }
        public object ModelValue { get; private set; }

        public XmlAnyElementValue(XmlName name, object modelValue)
        {
            this.Name = name;
            this.ModelValue = modelValue;
        }
    }

    public class XmlDocParsingContext
    {
        public XmlDocParsingContext()
        {

        }

        public XmlAnyElementValue ParseElement(XmlModelItemAnalyzer el)
        {
            throw new NotImplementedException("");
        }

        public XmlAnyAttributeValue ParseAttribute(XmlAnyAttributeInfo attr)
        {
            throw new NotImplementedException("");
        }
    }

}
