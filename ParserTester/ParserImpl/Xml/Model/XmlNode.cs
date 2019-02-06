using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Security;

namespace ParserImpl.Xml.Model
{
    public abstract class XmlNode
    {
        internal XmlNode()
        {

        }

        public string MakeXmlMarkup()
        {
            var sb = new StringBuilder();
            var w = new XmlModelWriter(sb);
            this.WriteTo(w);
            return sb.ToString();
        }

        public abstract void WriteTo(XmlModelWriter writer);
    }

    public abstract class XmlNamedNode : XmlNode
    {
        public XmlNodeName Name { get; private set; }

        internal XmlNamedNode(XmlNodeName name)
        {
            this.Name = name;
        }
    }

    public interface IXmlElementItem
    {
        void WriteTo(XmlModelWriter writer);
    }

    public class XmlElement : XmlNamedNode, IXmlElementItem //, IEnumerable<IXmlElementItem>
    {
        public ReadOnlyCollection<XmlAttribute> Attributes { get; private set; }
        public ReadOnlyCollection<IXmlElementItem> Childs { get; private set; }

        public string this[XmlNodeName attrName]
        {
            get
            {
                var attr = this.Attributes.FirstOrDefault(a => a.Name == attrName);
                return attr == null ? null : attr.Value;
            }
        }

        public XmlElement(XmlNodeName name, params XmlAttribute[] attrs)
            : this(name, attrs, new IXmlElementItem[0]) { }

        public XmlElement(XmlNodeName name, XmlAttribute[] attrs = null, params IXmlElementItem[] childs)
            : base(name)
        {
            if (attrs.GroupBy(a => a.Name).Any(ag => ag.Count() > 1))
                throw new ArgumentException();

            this.Attributes = new ReadOnlyCollection<XmlAttribute>(attrs == null ? new XmlAttribute[0] : attrs);
            this.Childs = new ReadOnlyCollection<IXmlElementItem>(childs == null ? new IXmlElementItem[0] : childs);
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            writer.WriteOpenElement(this.Name);

            foreach (var item in this.Attributes)
                writer.WriteAttribute(item.Name, item.Value);

            foreach (var item in this.Childs)
                item.WriteTo(writer);

            writer.WriteCloseElement();
        }

        //#region IEnumerable<IXmlElementItem>

        //public IEnumerator<IXmlElementItem> GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}

        //System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //{
        //    return this.GetEnumerator();
        //}

        //#endregion

        public override string ToString()
        {
            return string.Format("XmlElement[{0}]", this.Name);
        }
    }

    public class XmlComment : XmlNode, IXmlElementItem
    {
        public string Text { get; private set; }

        public XmlComment(string text)
        {
            if (text.Contains("-->"))
                throw new ArgumentException();

            this.Text = text;
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            writer.WriteComment(this.Text);
        }

        public override string ToString()
        {
            return string.Format("XmlComment[{0}]", this.Text);
        }
    }

    public class XmlCData : XmlNode, IXmlElementItem
    {
        public string Content { get; private set; }

        public XmlCData(string content)
        {
            if (content.Contains("]]>"))
                throw new ArgumentException();

            this.Content = content;
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            writer.WriteCData(this.Content);
        }

        public override string ToString()
        {
            return string.Format("XmlCData[{0}]", this.Content);
        }
    }

    public class XmlText : XmlNode, IXmlElementItem
    {
        public string Text { get; private set; }

        public XmlText(string text)
        {
            this.Text = text;
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            writer.WriteText(this.Text);
        }
 
        public override string ToString()
        {
            return string.Format("XmlText[{0}]", this.Text);
        }
    }
}
