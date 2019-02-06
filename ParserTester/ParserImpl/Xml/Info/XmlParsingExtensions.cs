using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserImpl.Xml.Info
{
    public static class XmlParsingExtensions
    {
        public static bool TryParseElementEntry<T>(this XmlModelElementAnalyzer elementAnalyzer, string elementName, XmlDocParsingContext context, out T result)
             where T : XmlElementModelItem, new()
        {
            bool ok;
            XmlModelElementAnalyzer el;

            if (elementAnalyzer.TryGetElement(elementName, out el))
            {
                result = new T();
                result.Parse(el, context);
                ok = true;
            }
            else
            {
                result = null;
                ok = false;
            }

            return ok;
        }

        //public static bool TryParseElementEntries<T>(this XmlModelElementAnalyzer elementAnalyzer, string elementName, int minOccurs, int maxOccurs, XmlDocParsingContext context, out T[] result)
        //     where T : XmlElementModelItem, new()
        //{
        //    List<T> lst = new List<T>();
        //    T item;
        //    Int32 count = 0;

        //    for (; count < minOccurs; count++)
        //    {
        //        if (elementAnalyzer.TryParseElementEntry(elementName, context, out item))
        //        {
        //            lst.Add(item);
        //        }
        //        else
        //        {
        //            result = null;
        //            return false;
        //        }
        //    }

        //    for (; count < maxOccurs; count++)
        //    {
        //        if (elementAnalyzer.TryParseElementEntry(elementName, context, out item))
        //        {
        //            lst.Add(item);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    result = lst.ToArray();
        //    return true;
        //}

        public delegate bool TryEntryHandlerDelegate<T>(XmlModelElementAnalyzer elementAnalyzer, XmlDocParsingContext context, out T result);

        public static bool TryParseEntries<T>(this XmlModelElementAnalyzer elementAnalyzer, int minOccurs, int maxOccurs, XmlDocParsingContext context, TryEntryHandlerDelegate<T> handler, out T[] result)
        {
            List<T> lst = new List<T>();
            T item;
            Int32 count = 0;

            for (; count < minOccurs; count++)
            {
                if (handler(elementAnalyzer, context, out item))
                {
                    lst.Add(item);
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            for (; count < maxOccurs; count++)
            {
                if (handler(elementAnalyzer, context, out item))
                {
                    lst.Add(item);
                }
                else
                {
                    break;
                }
            }

            result = lst.ToArray();
            return true;
        }

        class TryParseElementEntriesHandlerContext
        {
            readonly string _elementName;

            public TryParseElementEntriesHandlerContext(string elementName)
            {
                _elementName = elementName;
            }

            public bool Proc<T>(XmlModelElementAnalyzer elementAnalyzer, XmlDocParsingContext context, out T result)
                where T : XmlElementModelItem, new()
            {
                return elementAnalyzer.TryParseElementEntry(_elementName, context, out result);
            }
        }

        public static bool TryParseElementEntries<T>(this XmlModelElementAnalyzer elementAnalyzer, string elementName, int minOccurs, int maxOccurs, XmlDocParsingContext context, out T[] result)
             where T : XmlElementModelItem, new()
        {
            return elementAnalyzer.TryParseEntries(minOccurs, maxOccurs, context, new TryParseElementEntriesHandlerContext(elementName).Proc<T>, out result);
        }


    }
}
