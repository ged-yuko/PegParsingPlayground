using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ParserImpl.Xml.Model
{
    public class XmlDeclaration : XmlNode
    {
        public XmlDeclaration()
        {
            // TODO: 
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            writer.WriteDeclaration();
        }
    }

    public class XmlDocument : XmlNode
    {
        static readonly XmlComment[] _noComments = new XmlComment[0];

        public XmlDeclaration Declaration { get; private set; }
        public XmlElement RootElement { get; private set; }

        public ReadOnlyCollection<IXmlElementItem> Items { get; private set; }

        public XmlDocument(XmlDeclaration declaration, XmlElement rootElement, IList<XmlComment> leadingComments = null, IList<XmlComment> closingComments = null)
        {
            if (declaration == null)
                throw new ArgumentNullException("Argument 'declaration' cannot be null!");
            if (rootElement == null)
                throw new ArgumentNullException("Argument 'rootElement' cannot be null!");

            this.Declaration = declaration;
            this.RootElement = rootElement;

            this.Items = new ReadOnlyCollection<IXmlElementItem>(
                (leadingComments ?? _noComments).Cast<IXmlElementItem>()
                                                .Concat(new[] { rootElement })
                                                .Concat(closingComments ?? _noComments)
                                                .ToList()
            );
        }

        public override void WriteTo(XmlModelWriter writer)
        {
            this.Declaration.WriteTo(writer);

            foreach (var item in this.Items)
                item.WriteTo(writer);
        }
    }
}