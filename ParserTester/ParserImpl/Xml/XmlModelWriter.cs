using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;

namespace ParserImpl.Xml
{
    public class XmlModelWriter
    {
        bool _needCloseElementStart = false;
        Action<string> _write;
        Stack<string> _elements = new Stack<string>();

        public XmlModelWriter(StringBuilder stringBuilder)
        {
            _write = s => stringBuilder.Append(s);
        }

        public XmlModelWriter(StreamWriter writer)
        {
            _write = s => writer.Write(s);
        }

        private void Write(string str)
        {
            _write(str);
        }

        private void Write(params string[] strs)
        {
            for (int i = 0; i < strs.Length; i++)
            {
                this.Write(strs[i]);
            }
        }

        private void WriteFormat(string format, params object[] objs)
        {
            this.Write(string.Format(format, objs));
        }

        public void WriteAttribute(XmlNodeName name, string value)
        {
            if (!SecurityElement.IsValidAttributeValue(value))
                throw new ArgumentException();

            if (!_needCloseElementStart)
                throw new InvalidOperationException();

            this.Write(" ", name.ToString(), "=\"", value, "\"");
        }

        public void WriteOpenElement(XmlNodeName name)
        {
            if (_needCloseElementStart)
                this.Write(">");

            var sname = name.ToString();
            this.Write("<", sname);
            _elements.Push(sname);

            _needCloseElementStart = true;
        }

        public void WriteCloseElement()
        {
            var elName = _elements.Pop();

            if (_needCloseElementStart)
            {
                this.Write("/>");
            }
            else
            {
                this.Write("</", elName, ">");
            }

            _needCloseElementStart = false;
        }

        public void WriteComment(string text)
        {
            if (text.Contains("-->"))
                throw new ArgumentException();

            if (text.Contains("-->"))
                throw new ArgumentException();

            if (_needCloseElementStart)
                this.Write(">");

            this.Write("<!--", text, "-->");

            _needCloseElementStart = false;
        }

        public void WriteText(string text)
        {
            if (!SecurityElement.IsValidText(text))
                throw new ArgumentException();

            if (_needCloseElementStart)
                this.Write(">");

            this.Write(SecurityElement.Escape(text));

            _needCloseElementStart = false;
        }

        public void WriteCData(string text)
        {
            if (text.Contains("]]>"))
                throw new ArgumentException();

            if (_needCloseElementStart)
                this.Write(">");

            this.Write("<![CDATA[", text, "]]>");

            _needCloseElementStart = false;
        }

        public void WriteDeclaration()
        {
            this.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }
    }
}
