using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl;
using ParserImpl.Grammar;
using ParserImpl.Map;
using ParserImpl.Xml.Model;

namespace ParserImpl.Xml
{
    public class XmlParser
    {
        IParser<ITreeParsingResult> _xmlGrammar;
        Mapping<ISourceTextReader> _xmlMapping;

        public XmlParser()
        {
            var tr = DefinitionGrammar.Parse(XmlResources.OldXmlDocumentGrammar);
            var ruleSet = tr.Rules.OfType<RuleSet>().First(rs => rs.Name == "XmlDoc");
            _xmlGrammar = Parsers.CreateFabric(ruleSet).CreateTreeParser();

            //var map = new Mapping<ISourceTextReader>();
            //map.Set(ruleSet.Rules["%%"], (s, c) => c.Map<XmlDocument>(s.EnumerateRuleChilds().First(n => n.Rule.Name == "doc")));
            //map.Set(ruleSet.Rules["doc"], (s, c) => new XmlDocument(c.Map<XmlDeclaration>(s.EnumerateRuleChilds().First(n => n.Rule.Name == "decl")), c.Map<XmlElement>(s.EnumerateRuleChilds().First(n => n.Rule.Name == "element")), c.Map<XmlComment[]>(s.EnumerateRuleChilds().First(n => n.Rule.Name == "comments")), c.Map<XmlComment[]>(s.EnumerateRuleChilds().Where(n => n.Rule.Name == "comments").Skip(1).First())));
            //map.Set(ruleSet.Rules["comments"], (s, c) => s.EnumerateRuleChilds().Select(cs => c.Map<XmlComment>(cs)).ToArray());
            //map.Set(ruleSet.Rules["comment"], (s, c) => new XmlComment(s.EnumerateRuleChilds().First(n => n.Rule.Name == "ctext").GetContent(c.Context)));
            //map.Set(ruleSet.Rules["cdata"], (s, c) => new XmlCData(s.EnumerateRuleChilds().First(n => n.Rule.Name == "dtext").GetContent(c.Context)));
            //map.Set(ruleSet.Rules["text"], (s, c) => new XmlText(s.GetContent(c.Context)));
            //map.Set(ruleSet.Rules["decl"], (s, c) => new XmlDeclaration());
            //map.Set(ruleSet.Rules["name"], (s, c) => {
            //    var prefix = s[0].Content.Content.Length < s[0][0].Content.Content.Length ? null : s[0][0].Content.Content;
            //    return new XmlNodeName(prefix, s.EnumerateRuleChilds().First(n => n.Rule.Name == "identifier").GetContent(c.Context));
            //});
            //map.Set(ruleSet.Rules["element"], (s, c) => {
            //    if (s.ChildsCount > 2 && s["name"].Content.Content != s["name", 1].Content.Content)
            //        throw new InvalidOperationException(string.Format("Unmatched xml element close tag '{0}'!", s["name", 1].Content.Content));

            //    return s.ChildsCount < 3 ? new XmlElement(c.Map<XmlNodeName>(s["name"]), c.Map<XmlAttribute[]>(s["attrs"]))
            //                             : new XmlElement(c.Map<XmlNodeName>(s["name"]), c.Map<XmlAttribute[]>(s["attrs"]), c.Map<IXmlElementItem[]>(s["elContent"]));
            //});
            //map.Set(ruleSet.Rules["attrs"], (s, c) => s.Select(cs => c.Map<XmlAttribute>(cs)).ToArray());
            //map.Set(ruleSet.Rules["attr"], (s, c) => new XmlAttribute(c.Map<XmlNodeName>(s["name"]), s["avalue"].Content.Content));
            //map.Set(ruleSet.Rules["elContent"], (s, c) => s.Select(cs => c.Map<IXmlElementItem>(cs)).ToArray());

            //_xmlMapping = map;

            throw new NotImplementedException("");
        }

        public XmlDocument Parse(string docXml)
        {
            //var presult = _xmlGrammar.Parse(docXml);
            //var docTree = presult.ResultTree;

            //if (docTree == null)
            //{
            //    System.Diagnostics.Debug.Print(presult.Log);
            //    throw new InvalidOperationException("Parsing failed");
            //}

            //var tresult = _xmlMapping.Translate(docTree);
            //return (XmlDocument)tresult.Result;
            throw new NotImplementedException("");
        }
    }
}
