using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserImpl.Map;

namespace ParserImpl.Grammar
{
    public static class DefinitionGrammar
    {
        static bool _initialized = false;
        static RuleSet _definitionRuleSet;
        static IParserFabric _defParserFabric;
        static Mapping<TranslationContext> _map;

        public static IParserFabric ParserFabric { get { Initialize(); return _defParserFabric; } }

        private static void Initialize()
        {
            if (!_initialized)
            {
                InitializeImpl();
                _initialized = true;
            }
        }

        private static void InitializeImpl()
        {
            const string GrammarDefinition = "GrammarDefinition";

            // const string commentsAndSpaces = "commentsAndSpaces";

            const string name = "name";
            const string number = "number";
            const string hex = "hex";
            const string complexName = "complexName";

            const string attributes = "attributes";
            const string attributeUsageArgList = "attributeUsageArgList";
            const string attributeUsage = "attributeUsage";
            const string attributesCollection = "attributesCollection";

            const string expr = "expr";
            const string simple = "simple";
            const string @string = "string";
            const string chars = "chars";
            const string anyChar = "anyChar";
            const string charCode = "charCode";
            const string group = "group";
            const string check = "check";
            const string checkNot = "checkNot";
            const string usage = "usage";
            const string flag = "flag";
            const string args = "args";
            const string complex = "complex";
            const string sequence = "sequence";
            const string item = "item";

            const string alternatives = "alternatives";
            const string repeat = "repeat";
            const string quantor = "quantor";
            const string qnumbers = "qnumbers";
            const string full = "full";
            const string max = "max";
            const string min = "min";
            const string exact = "exact";

            const string ruleDef = "ruleDef";
            const string ruleDefArg = "ruleDefArg";
            const string ruleDefArgs = "ruleDefArgs";
            const string ruleBody = "ruleBody";
            const string extendable = "extendable";
            const string alternative = "alternative";
            const string priority = "priority";
            const string @explicit = "explicit";
            const string subRules = "subRules";

            const string ruleSet = "ruleSet";
            const string body = "body";
            const string ruleSetImport = "ruleSetImport";
            const string alias = "alias";

            int tokenId = 0;

            var rs = new RuleSet(GrammarDefinition) {
                // new EntityAttribute("OmitPattern", new RuleExpression.RuleUsage(commentsAndSpaces)),
                new EntityAttribute("OmitPattern", new RuleExpression.Regex(@"([\s]*)(/\*(?>(?:(?>[^*]+)|\*(?!/))*)\*/[\s]*)*")),
                new EntityAttribute("RootRule", new RuleExpression.RuleUsage("ruleSet.body")),

                // new ExplicitRule(tokenId++, commentsAndSpaces, new RuleExpression.Regex(@"([\s]*)(/\*(?>(?:(?>[^*]+)|\*(?!/))*)\*/[\s]*)*")),

                new ExplicitRule(tokenId++, name, new RuleExpression.Regex("[a-zA-Z_][a-zA-Z_0-9]*")),
                new ExplicitRule(tokenId++, number, new RuleExpression.Regex("[0-9]+")),
                new ExplicitRule(tokenId++, hex, new RuleExpression.Regex("0x[a-fA-F0-9]+")),
                new ExplicitRule(tokenId++, complexName, new RuleExpression.Sequence(
                    new RuleExpression.RuleUsage(name),
                    new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.Sequence(
                        new RuleExpression.CharCode('.'),
                        new RuleExpression.RuleUsage(name)
                    ))
                )),

                new ExplicitRule(tokenId++, attributes, new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.RuleUsage(attributesCollection))) {
                    new ExplicitRule(tokenId++, attributeUsageArgList, new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                        new RuleExpression.CharCode('('),
                        new RuleExpression.RuleUsage(expr),
                        new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.Sequence(
                            new RuleExpression.CharCode(','),
                            new RuleExpression.RuleUsage(expr)
                        )),
                        new RuleExpression.CharCode(')')
                    ))),
                    new ExplicitRule(tokenId++, attributeUsage, new RuleExpression.Sequence(
                        new RuleExpression.RuleUsage(complexName),
                        new RuleExpression.RuleUsage(attributeUsageArgList)
                    )),
                    new ExplicitRule(tokenId++, attributesCollection, new RuleExpression.Sequence(
                        new RuleExpression.CharCode('['),
                        new RuleExpression.RuleUsage(attributeUsage),
                        new RuleExpression.MatchNumber(0, int.MaxValue,new RuleExpression.Sequence(
                            new RuleExpression.CharCode(','),   
                            new RuleExpression.RuleUsage(attributeUsage)
                        )),
                        new RuleExpression.CharCode(']')
                    ))
                },

                new ExtensibleRule(tokenId++, expr) {
                    { 0, new ExtensibleRule(tokenId++, complex) {
                        { 0, new ExplicitRule(tokenId++, sequence, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(item),
                            new RuleExpression.MatchNumber(1, int.MaxValue, new RuleExpression.RuleUsage(item))
                        )) {
                            new ExplicitRule(tokenId++, item, new RuleExpression.Or(
                                new RuleExpression.RuleUsage(alternatives),
                                new RuleExpression.RuleUsage(repeat),
                                new RuleExpression.RuleUsage(simple)
                            ))
                        } },
                        { 1, new ExplicitRule(tokenId++, alternatives, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(item),
                            new RuleExpression.MatchNumber(1, int.MaxValue, new RuleExpression.Sequence(
                                new RuleExpression.CharCode('|'),
                                new RuleExpression.RuleUsage(item)
                            ))
                        )) {
                            new ExplicitRule(tokenId++, item, new RuleExpression.Or(
                                new RuleExpression.RuleUsage(repeat),
                                new RuleExpression.RuleUsage(simple)
                            ))
                        } },
                        { 2, new ExplicitRule(tokenId++, repeat, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(simple),
                            new RuleExpression.RuleUsage(quantor)
                        )) {
                            new ExplicitRule(tokenId++, quantor, new RuleExpression.Or(
                                new RuleExpression.CharCode('*'),
                                new RuleExpression.CharCode('+'),
                                new RuleExpression.CharCode('?'),
                                new RuleExpression.Sequence(
                                    new RuleExpression.CharCode('{'),
                                    new RuleExpression.RuleUsage(qnumbers),
                                    new RuleExpression.CharCode('}')
                                )
                            )),
                            new ExtensibleRule(tokenId++, qnumbers) {
                                { 0, new ExplicitRule(tokenId++, full, new RuleExpression.Sequence(
                                    new RuleExpression.RuleUsage(number),
                                    new RuleExpression.CharCode(','),
                                    new RuleExpression.RuleUsage(number)
                                )) },
                                { 1, new ExplicitRule(tokenId++, max, new RuleExpression.Sequence(
                                    new RuleExpression.CharCode(','),
                                    new RuleExpression.RuleUsage(number)
                                )) },
                                { 2, new ExplicitRule(tokenId++, min, new RuleExpression.Sequence(
                                    new RuleExpression.RuleUsage(number),
                                    new RuleExpression.CharCode(',')
                                )) },
                                { 3, new ExplicitRule(tokenId++, exact, new RuleExpression.RuleUsage(number)) }
                            }
                        } }
                    } },
                    { 0, new ExtensibleRule(tokenId++, simple) {
                        //{ 0, new ExplicitRule(tokenId++, @string, new RuleExpression.Regex("\"[^\"\\]*(?:\\.[^\"\\]*)*\"")) },
                        { 0, new ExplicitRule(tokenId++, @string, new RuleExpression.Regex("\\\"[^\\\"\\\\]*(?:\\\\.[^\\\"\\\\]*)*\\\"")) },
                        { 1, new ExplicitRule(tokenId++, chars , new RuleExpression.Regex("'[^']*'")) },
                        { 2, new ExplicitRule(tokenId++, anyChar, new RuleExpression.CharCode('.')) },
                        { 3, new ExplicitRule(tokenId++, charCode, new RuleExpression.RuleUsage(hex)) },
                        { 4, new ExplicitRule(tokenId++, group, new RuleExpression.Sequence(
                            new RuleExpression.CharCode('('),
                            new RuleExpression.RuleUsage(expr),
                            new RuleExpression.CharCode(')')
                        )) },
                        { 5, new ExplicitRule(tokenId++, check, new RuleExpression.Sequence(
                            new RuleExpression.CharCode('&'),
                            new RuleExpression.RuleUsage(simple)
                        )) },
                        { 6, new ExplicitRule(tokenId++, checkNot, new RuleExpression.Sequence(
                            new RuleExpression.CharCode('!'),
                            new RuleExpression.RuleUsage(simple)
                        )) },
                        { 7, new ExplicitRule(tokenId++, usage, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(flag),
                            new RuleExpression.RuleUsage(complexName),
                            new RuleExpression.RuleUsage(args)
                        )) {
                            new ExplicitRule(tokenId++, flag, new RuleExpression.MatchNumber(0, 1, new RuleExpression.Or(
                                new RuleExpression.CharCode('%'),
                                new RuleExpression.CharCode('#')
                            ))),
                            new ExplicitRule(tokenId++, args, new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                                new RuleExpression.CharCode('<'),
                                new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                                    new RuleExpression.RuleUsage(expr),
                                    new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.Sequence(
                                        new RuleExpression.CharCode(','),
                                        new RuleExpression.RuleUsage(expr)
                                    ))
                                )),
                                new RuleExpression.CharCode('>')
                            )))
                        } }
                    } }
                },

                new ExplicitRule(tokenId++, ruleDef, new RuleExpression.Sequence(
                    new RuleExpression.RuleUsage(attributes),
                    new RuleExpression.RuleUsage(flag),
                    new RuleExpression.RuleUsage(name),
                    new RuleExpression.RuleUsage(ruleDefArgs),
                    new RuleExpression.CharCode(':'),
                    new RuleExpression.RuleUsage(ruleBody),
                    new RuleExpression.CharCode(';')
                )) {
                    new ExplicitRule(tokenId++, flag, new RuleExpression.MatchNumber(0, 1, new RuleExpression.CharCode('#'))),
                    new ExplicitRule(tokenId++, ruleDefArg, new RuleExpression.Sequence(
                        new RuleExpression.RuleUsage(flag),
                        new RuleExpression.RuleUsage(name)
                    )),
                    new ExplicitRule(tokenId++, ruleDefArgs, new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                        new RuleExpression.CharCode('<'),
                        new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(ruleDefArg),
                            new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.Sequence(
                                new RuleExpression.CharCode(','),
                                new RuleExpression.RuleUsage(ruleDefArg)
                            ))
                        )),
                        new RuleExpression.CharCode('>')
                    ))),
                    new ExtensibleRule(tokenId++, ruleBody) {
                        { 0, new ExplicitRule(tokenId++, extendable, new RuleExpression.Sequence(
                            new RuleExpression.CharCode('{'),
                            new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.RuleUsage(alternative)),
                            new RuleExpression.CharCode('}')
                        )) {
                            new ExplicitRule(tokenId++, alternative, new RuleExpression.Sequence(
                                new RuleExpression.RuleUsage(priority),
                                new RuleExpression.CharCode('|'),
                                new RuleExpression.RuleUsage(ruleDef)
                            )),
                            new ExplicitRule(tokenId++, priority, new RuleExpression.MatchNumber(0,1,new RuleExpression.RuleUsage(number)))
                        } },
                        { 1, new ExplicitRule(tokenId++, @explicit, new RuleExpression.Sequence(
                            new RuleExpression.RuleUsage(expr),
                            new RuleExpression.RuleUsage(subRules)
                        )) {
                            new ExplicitRule(tokenId++, subRules, new RuleExpression.MatchNumber(0, 1, new RuleExpression.Sequence(
                                new RuleExpression.CharCode('{'),
                                new RuleExpression.MatchNumber(0,int.MaxValue, new RuleExpression.RuleUsage(ruleDef)),
                                new RuleExpression.CharCode('}')
                            )))
                        } }
                    }
                },
                
                new ExplicitRule(tokenId++, ruleSet, new RuleExpression.Sequence(
                    new RuleExpression.RuleUsage(attributes),
                    new RuleExpression.RuleUsage(complexName),
                    new RuleExpression.CharCode('{'),
                    new RuleExpression.RuleUsage(body),
                    new RuleExpression.CharCode('}')
                )) {
                    new ExplicitRule(tokenId++, body, new RuleExpression.MatchNumber(0, int.MaxValue, new RuleExpression.RuleUsage(item))),
                    new ExplicitRule(tokenId++, item, new RuleExpression.Sequence(
                        new RuleExpression.Or(
                            new RuleExpression.RuleUsage(ruleDef),
                            new RuleExpression.RuleUsage(ruleSet),
                            new RuleExpression.RuleUsage(ruleSetImport)
                        )
                    )),
                    new ExplicitRule(tokenId++, ruleSetImport, new RuleExpression.Sequence(
                        new RuleExpression.RuleUsage(attributes),
                        new RuleExpression.RuleUsage(alias),
                        new RuleExpression.RuleUsage(complexName),
                        new RuleExpression.CharCode(';')
                    )) {
                        new ExplicitRule(tokenId++, alias, new RuleExpression.MatchNumber(0, 1, 
                            new RuleExpression.Sequence(
                                new RuleExpression.RuleUsage(name),
                                new RuleExpression.CharCode('=')
                            )
                        ))
                    }
                }
            };
            _definitionRuleSet = rs;
            _defParserFabric = Parsers.CreateFabric(rs);


            var m = new Mapping<TranslationContext>();
            m.Set(rs.Rules[name], (n, c) => n.GetContent(c.Context.Reader));
            m.Set(rs.Rules[complexName], (n, c) => string.Join(".", n.EnumerateRuleChilds().Select(cn => c.Map<string>(cn))));
            m.Set(rs.Rules[number], (n, c) => int.Parse(n.GetContent(c.Context.Reader)));
            m.Set(rs.Rules[hex], (n, c) => Convert.ToInt32(n.GetContent(c.Context.Reader).Substring(2), 16));

            #region attributes mapping

            m.Set(rs.Rules[attributes], (n, c) =>
                n.EnumerateRuleChilds().SelectMany(cn => c.Map<EntityAttribute[]>(cn)).ToArray()
            );
            m.Set(rs.Rules[attributes].Rules[attributesCollection], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<EntityAttribute>(0);

                for (int i = 1; i < arr.Length - 1; i += 2)
                    result.Add(c.Map<EntityAttribute>(arr[i]));

                return result.ToArray();
            });
            m.Set(rs.Rules[attributes].Rules[attributeUsage], (n, c) => {
                var cn = n.GetRuleChildsArray();
                return new EntityAttribute(c.Map<string>(cn[0]), c.Map<RuleExpression[]>(cn[1]));
            });
            m.Set(rs.Rules[attributes].Rules[attributeUsageArgList], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<RuleExpression>(0);

                for (int i = 1; i < arr.Length - 1; i += 2)
                    result.Add(c.Map<RuleExpression>(arr[i]));

                return result.ToArray();
            });

            #endregion

            #region expr mapping

            m.Set(rs.Rules[expr], (n, c) => c.Map<RuleExpression>(n.EnumerateRuleChilds().First()));
            m.Set(rs.Rules[expr].Rules[complex], (n, c) => c.Map<RuleExpression>(n.EnumerateRuleChilds().First()));
            m.Set(rs.Rules[expr].Rules[complex].Rules[sequence], (n, c) => (RuleExpression)new RuleExpression.Sequence(
                n.EnumerateRuleChilds().Select(cn => c.Map<RuleExpression>(cn)).ToArray()
            ));
            m.Set(rs.Rules[expr].Rules[complex].Rules[sequence].Rules[item], (n, c) =>
                c.Map<RuleExpression>(n.EnumerateRuleChilds().First())
            );
            m.Set(rs.Rules[expr].Rules[complex].Rules[alternatives], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<RuleExpression>(0);

                for (int i = 0; i < arr.Length; i += 2)
                    result.Add(c.Map<RuleExpression>(arr[i]));

                return (RuleExpression)new RuleExpression.Or(result.ToArray());
            });
            m.Set(rs.Rules[expr].Rules[complex].Rules[alternatives].Rules[item], (n, c) =>
                c.Map<RuleExpression>(n.EnumerateRuleChilds().First())
            );
            m.Set(rs.Rules[expr].Rules[complex].Rules[repeat], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var exprValue = c.Map<RuleExpression>(arr[0]);
                var quantity = c.Map<Tuple<int, int>>(arr[1]);
                return (RuleExpression)new RuleExpression.MatchNumber(quantity.Item1, quantity.Item2, exprValue);
            });
            m.Set(rs.Rules[expr].Rules[complex].Rules[repeat].Rules[quantor], (n, c) => {
                var specStr = n.GetContent(c.Context.Reader);
                int minNum, maxNum;

                switch (specStr)
                {
                    case "?": minNum = 0; maxNum = 1; break;
                    case "*": minNum = 0; maxNum = int.MaxValue; break;
                    case "+": minNum = 1; maxNum = int.MaxValue; break;
                    default:
                        {
                            var specParts = specStr.TrimStart('{').TrimEnd('}').Split(new[] { ',' }, StringSplitOptions.None).Select(p => p.Trim()).ToArray();

                            if (specParts.Length == 1)
                            {
                                minNum = maxNum = int.Parse(specParts[0]);
                            }
                            else if (specParts[0].Length == 0)
                            {
                                minNum = 0;
                                maxNum = int.Parse(specParts[1]);
                            }
                            else if (specParts[1].Length == 0)
                            {
                                minNum = int.Parse(specParts[0]);
                                maxNum = int.MaxValue;
                            }
                            else
                            {
                                minNum = int.Parse(specParts[0]);
                                maxNum = int.Parse(specParts[1]);
                            }
                        } break;
                }

                return Tuple.Create(minNum, maxNum);
            });

            m.Set(rs.Rules[expr].Rules[simple], (n, c) => c.Map<RuleExpression>(n.EnumerateRuleChilds().First()));
            m.Set(rs.Rules[expr].Rules[simple].Rules[@string], (n, c) => {
                var content = n.GetContent(c.Context.Reader);
                return (RuleExpression)new RuleExpression.Regex(content.Substring(1, content.Length - 2));
            });
            m.Set(rs.Rules[expr].Rules[simple].Rules[chars], (n, c) => {
                var content = n.GetContent(c.Context.Reader);
                return (RuleExpression)new RuleExpression.Chars(content.Substring(1, content.Length - 2));
            });
            m.Set(rs.Rules[expr].Rules[simple].Rules[anyChar], (n, c) =>
                (RuleExpression)new RuleExpression.AnyChar()
            );
            m.Set(rs.Rules[expr].Rules[simple].Rules[charCode], (n, c) =>
                (RuleExpression)new RuleExpression.CharCode((char)c.Map<int>(n.EnumerateRuleChilds().First()))
            );
            m.Set(rs.Rules[expr].Rules[simple].Rules[group], (n, c) =>
                (RuleExpression)c.Map<RuleExpression>(n.EnumerateRuleChilds().Skip(1).First())
            );
            m.Set(rs.Rules[expr].Rules[simple].Rules[check], (n, c) =>
                (RuleExpression)new RuleExpression.Check(c.Map<RuleExpression>(n.EnumerateRuleChilds().Skip(1).First()))
            );
            m.Set(rs.Rules[expr].Rules[simple].Rules[checkNot], (n, c) =>
                (RuleExpression)new RuleExpression.CheckNot(c.Map<RuleExpression>(n.EnumerateRuleChilds().Skip(1).First()))
            );
            m.Set(rs.Rules[expr].Rules[simple].Rules[usage], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var flagStr = arr[0].GetContent(c.Context.Reader).Trim();
                var expandSubnodes = flagStr == "#";
                var ruleName = arr[1].GetContent(c.Context.Reader).Trim();
                if (flagStr == "%")
                    ruleName = "%" + ruleName;
                var argsValue = c.Map<RuleExpression[]>(arr[2]);

                return (RuleExpression)new RuleExpression.RuleUsage(expandSubnodes, ruleName, argsValue);
            });
            m.Set(rs.Rules[expr].Rules[simple].Rules[usage].Rules[args], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<RuleExpression>(0);

                for (int i = 1; i < arr.Length - 1; i += 2)
                    result.Add(c.Map<RuleExpression>(arr[i]));

                return result.ToArray();
            });

            #endregion

            #region rule def mapping

            m.Set(rs.Rules[ruleDef], (n, c) => {
                var cn = n.GetRuleChildsArray();
                var attrs = c.Map<EntityAttribute[]>(cn[0]);
                var flagValue = c.Map<bool>(cn[1]);
                var nameValue = c.Map<string>(cn[2]);
                var argsValue = c.Map<RuleParameter[]>(cn[3]);
                var bodyInfo = c.Map<RuleBodyInfo>(cn[5]);

                Rule result;
                if (bodyInfo.Expression == null)
                {
                    var extResult = new ExtensibleRule(c.Context.GetNextTokenId(), nameValue);
                    foreach (var alt in bodyInfo.Alternatives)
                        extResult.Add(alt.Priority, alt.Rule);

                    result = extResult;
                }
                else
                {
                    try
                    {
                        result = new ExplicitRule(c.Context.GetNextTokenId(), nameValue, bodyInfo.Expression);
                        foreach (var subrule in bodyInfo.Subrules)
                            result.Add(subrule);
                    }
                    catch
                    {
                        result = null;
                        Console.WriteLine();
                    }
                }

                foreach (var attr in attrs)
                    result.Add(attr);

                foreach (var arg in argsValue)
                    result.Add(arg);

                result.IsExpandable = flagValue;

                return (RuleSetBase)result;
            });
            m.Set(rs.Rules[ruleDef].Rules[flag], (n, c) => n.GetContent(c.Context.Reader).Length > 0);
            m.Set(rs.Rules[ruleDef].Rules[ruleDefArg], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var flagValue = c.Map<bool>(arr[0]);
                var nameValue = c.Map<string>(arr[1]);
                return new RuleParameter(nameValue) {
                    IsExpandable = flagValue
                };
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleDefArgs], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<RuleParameter>(0);

                for (int i = 1; i < arr.Length - 1; i += 2)
                    result.Add(c.Map<RuleParameter>(arr[i]));

                return result.ToArray();
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleBody], (n, c) =>
                c.Map<RuleBodyInfo>(n.EnumerateRuleChilds().First())
            );
            m.Set(rs.Rules[ruleDef].Rules[ruleBody].Rules[extendable], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<ExtensibleRuleAlternativeInfo>(0);

                for (int i = 1; i < arr.Length - 1; i++)
                    result.Add(c.Map<ExtensibleRuleAlternativeInfo>(arr[i]));

                return new RuleBodyInfo(result.ToArray());
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleBody].Rules[extendable].Rules[alternative], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var priorityValue = c.Map<int>(arr[0]);
                var def = c.Map<RuleSetBase>(arr[2]);
                return new ExtensibleRuleAlternativeInfo(priorityValue, (Rule)def);
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleBody].Rules[extendable].Rules[priority], (n, c) => {
                var child = n.EnumerateRuleChilds().FirstOrDefault();
                return child == null ? 0 : c.Map<int>(child);
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleBody].Rules[@explicit], (n, c) => {
                var arr = n.GetRuleChildsArray();
                return new RuleBodyInfo(
                    c.Map<RuleExpression>(arr[0]),
                    c.Map<RuleSetBase[]>(arr[1])
                );
            });
            m.Set(rs.Rules[ruleDef].Rules[ruleBody].Rules[@explicit].Rules[subRules], (n, c) => {
                var arr = n.GetRuleChildsArray();
                var result = new List<RuleSetBase>(0);

                for (int i = 1; i < arr.Length - 1; i++)
                    result.Add(c.Map<RuleSetBase>(arr[i]));

                return result.ToArray();
            });

            #endregion

            #region rule set mapping

            m.Set(rs.Rules[ruleSet], (n, c) => {
                var cn = n.GetRuleChildsArray();
                var nameValue = c.Map<string>(cn[1]);
                var result = new RuleSet(nameValue);

                foreach (var r in c.Map<RuleSetBase[]>(cn[3]))
                    result.Add(r);
                foreach (var a in c.Map<EntityAttribute[]>(cn[0]))
                    result.Add(a);

                return (RuleSetBase)result;
            });
            m.Set(rs.Rules[ruleSet].Rules[body], (n, c) =>
                n.EnumerateRuleChilds().Select(cn => c.Map<RuleSetBase>(cn)).ToArray()
            );
            m.Set(rs.Rules[ruleSet].Rules[item], (n, c) =>
                c.Map<RuleSetBase>(((IParsingTreeGroup)n).Childs.First())
            );
            m.Set(rs.Rules[ruleSet].Rules[ruleSetImport], (n, c) => {
                var cn = n.GetRuleChildsArray();
                var aliasValue = c.Map<string>(cn[1]);
                var nameValue = c.Map<string>(cn[2]);

                var result = new RuleSetImport(string.IsNullOrWhiteSpace(alias) ? nameValue : aliasValue, nameValue);
                foreach (var a in c.Map<EntityAttribute[]>(cn[0]))
                    result.Add(a);

                throw new NotImplementedException("");
                return (NamedEntityBase)result;
            });
            m.Set(rs.Rules[ruleSet].Rules[ruleSetImport].Rules[alias], (n, c) =>
                c.Map<string>(n.EnumerateRuleChilds().First())
            );

            #endregion

            _map = m;
        }

        class RuleBodyInfo
        {
            public ExtensibleRuleAlternativeInfo[] Alternatives { get; private set; }
            public RuleExpression Expression { get; private set; }
            public RuleSetBase[] Subrules { get; private set; }

            public RuleBodyInfo(ExtensibleRuleAlternativeInfo[] alternatives)
            {
                this.Alternatives = alternatives;
            }

            public RuleBodyInfo(RuleExpression expr, RuleSetBase[] subrules)
            {
                this.Expression = expr;
                this.Subrules = subrules;
            }
        }

        public static RuleSetParsingResult Parse(string text, bool withLog = false)
        {
            Initialize();

            var parser = _defParserFabric.CreateTreeParser();
            parser.EnableLog = withLog;

            var reader = new StringSourceTextReader(text);
            var parsingResult = parser.Parse(reader);

            if (!parsingResult.Successed)
                throw new InvalidOperationException("Invalid grammar definition!");

            // return new RuleSetParsingResult(null, parsingResult, parser.ParsingStatistics);

            var tree = ((IParsingTreeGroup)((IParsingTreeGroup)parsingResult.Tree).Childs.First()).Childs.First();

            var ctx = _map.Translate(tree, new TranslationContext(reader));
            var rules = ctx.Result as RuleSetBase[];
            if (rules == null)
                throw new InvalidOperationException("RuleSet mapping failed unexpectedly!");

            return new RuleSetParsingResult(rules, parsingResult, parser.ParsingStatistics);
        }
    }

    public class TranslationContext
    {
        public StringSourceTextReader Reader { get; private set; }

        private int _id;

        public TranslationContext(StringSourceTextReader reader)
        {
            _id = 0;
            this.Reader = reader;
        }

        public int GetNextTokenId()
        {
            return _id++;
        }
    }

    public sealed class RuleSetParsingResult
    {
        public RuleSetBase[] Rules { get; private set; }
        public ITreeParsingResult ParsingResult { get; private set; }
        public TimeSpan Statistics { get; private set; }

        internal RuleSetParsingResult(RuleSetBase[] rules, ITreeParsingResult parsingResult, TimeSpan statistics)
        {
            this.Rules = rules;
            this.ParsingResult = parsingResult;
            this.Statistics = statistics;
        }
    }
}
