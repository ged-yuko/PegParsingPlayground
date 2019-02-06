using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ParserImpl.Grammar
{
    class GrammarNavigator
    {
        struct RuleParameterInfo
        {
            public readonly RuleParameter paramInfo;
            public readonly RuleExpression expression;
            public readonly NavContext paramValueContext;

            public RuleParameterInfo(RuleParameter paramInfo, RuleExpression expression, NavContext paramValueContext)
            {
                this.paramInfo = paramInfo;
                this.expression = expression;
                this.paramValueContext = paramValueContext;
            }
        }

        public class RuleInfo
        {
            public bool IsExplicitRule { get { return this.Rule != null; } }

            public ExplicitRule Rule { get; private set; }
            public ReadOnlyCollection<ExtensibleRule> ExtRule { get; private set; }

            public string NamePath { get; private set; }

            public RuleInfo(string namePath, ExplicitRule rule)
            {
                this.NamePath = namePath;
                this.Rule = rule;
                this.ExtRule = null;
            }

            public RuleInfo(string namePath, ExtensibleRule[] rules)
            {
                this.NamePath = namePath;
                this.Rule = null;
                this.ExtRule = new ReadOnlyCollection<ExtensibleRule>(rules);
            }
        }

        class NavContext
        {
            public RuleInfo Info { get; private set; }
            public string Name { get; private set; }

            public string FullName
            {
                get
                {
                    var hasPrevName = _parentContext != null && !string.IsNullOrWhiteSpace(_parentContext.Name);
                    return hasPrevName ? string.Format("{0}.{1}", _parentContext.FullName, this.Name) : this.Name;
                }
            }

            public string RuleFullName
            {
                get
                {
                    var hasPrevName = _parentContext != null && !string.IsNullOrWhiteSpace(_parentContext.RuleFullName) && _parentContext.Info != null;
                    return hasPrevName ? string.Format("{0}.{1}", _parentContext.RuleFullName, this.Name) : this.Name;
                }
            }

            ReadOnlyCollection<RuleSetBase> _entities;
            ReadOnlyCollection<EntityAttribute> _attributes = null;
            ReadOnlyCollection<RuleSetBase> _visibleEntities = null;

            Dictionary<string, RuleParameterInfo> _ruleParamExpressions = null;

            readonly NavContext _parentContext;

            public NavContext ParentContext { get { return _parentContext; } }
            public ReadOnlyCollection<RuleSetBase> Entities { get { return _entities; } }

            public NavContext(params RuleSet[] ruleSets)
            {
                _parentContext = null;
                _entities = null;
                _attributes = new ReadOnlyCollection<EntityAttribute>(CollectionsUtils<EntityAttribute>.EmptyCollection);
                _visibleEntities = new ReadOnlyCollection<RuleSetBase>(ruleSets.Cast<RuleSetBase>().ToArray());
            }

            private NavContext(NavContext parent, string entityName)
            {
                _parentContext = parent;

                _entities = new ReadOnlyCollection<RuleSetBase>(parent._visibleEntities.Where(e => e.Name == entityName).ToArray());
                _attributes = new ReadOnlyCollection<EntityAttribute>(_entities.SelectMany(e => e.GetAttributes()).ToArray());

                var visibleEntities = _entities.SelectMany(e => e.GetEntities()).OfType<RuleSetBase>();

                foreach (var item in _entities.SelectMany(e => e.GetEntities()).OfType<RuleSetImport>())
                {
                    RuleSetBase[] importedEntities;

                    if (item.RuleSetName.Contains('.'))
                        throw new NotImplementedException();

                    if (!parent.TryResolveEntities(item.RuleSetName, out importedEntities))
                    {
                        importedEntities = _entities.SelectMany(e => e.GetEntities())
                                                    .Where(ie => ie.Name == item.RuleSetName)
                                                    .OfType<RuleSetBase>()
                                                    .ToArray();
                    }

                    if (importedEntities.Length > 0)
                    {
                        var importedContent = importedEntities.SelectMany(ie => ie.GetEntities())
                                                              .OfType<RuleSetBase>();

                        visibleEntities = visibleEntities.Concat(importedContent);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Failed to import rule set [{0}]!", item.RuleSetName));
                    }
                }

                _visibleEntities = new ReadOnlyCollection<RuleSetBase>(visibleEntities.ToArray());

                RuleInfo info;
                this.Name = entityName;
                this.Info = TryMakeRuleInfo(_entities, out info) ? info : null;
            }

            public bool TryGetAttribute(string attrName, out EntityAttribute attr)
            {
                attr = _attributes.FirstOrDefault(a => a.Name == attrName);
                return attr != null;
            }

            public bool TryEnter(string entityName, out NavContext result)
            {
                foreach (var item in this.GetContexts())
                    if (item.TryEnterImpl(entityName, out result))
                        return true;

                result = null;
                return false;
            }

            private bool TryEnterImpl(string entityName, out NavContext result)
            {
                if (_visibleEntities.Any(e => e.Name == entityName))
                    result = new NavContext(this, entityName);
                else
                    result = null;

                return result != null;
            }

            public ReadOnlyCollection<EntityAttribute> GetAttributes()
            {
                return _attributes;
            }

            public bool TryResolveEntities(string entityName, out RuleSetBase[] entities)
            {
                foreach (var item in this.GetContexts())
                    if (item.TryResolveEntitiesImpl(entityName, out entities))
                        return true;

                entities = null;
                return false;
            }

            private bool TryResolveEntitiesImpl(string entityName, out RuleSetBase[] entities)
            {
                entities = _visibleEntities.Where(e => e.Name == entityName).ToArray();
                if (entities.Length == 0)
                    entities = null;

                return entities != null;
            }

            public bool TryResolveRule(string ruleName, out RuleInfo rule)
            {
                foreach (var item in this.GetContexts())
                    if (item.TryResolveRuleImpl(ruleName, out rule))
                        return true;

                rule = null;
                return false;
            }

            private bool TryResolveRuleImpl(string ruleName, out RuleInfo rule)
            {
                var childs = _visibleEntities.Where(e => e.Name == ruleName).OfType<Rule>().ToArray();

                if (childs.Length > 0)
                {
                    if (!TryMakeRuleInfo(childs, out rule))
                        throw new InvalidOperationException(string.Format("Entity named [{0}] is not a rule!", ruleName));
                }
                else
                {
                    rule = null;
                }

                return rule != null;
            }

            private IEnumerable<NavContext> GetContexts()
            {
                var ctx = this;
                do
                {
                    yield return ctx;
                    ctx = ctx._parentContext;
                } while (ctx != null);
            }

            private bool TryMakeRuleInfo(IList<RuleSetBase> entities, out RuleInfo info)
            {
                if (entities.Count == 0)
                    throw new InvalidOperationException();

                var ruleEntities = entities.OfType<Rule>().ToArray();
                if (ruleEntities.Length == 0)
                {
                    info = null;
                    return false;
                }

                if (entities.Count != ruleEntities.Length)
                    throw new InvalidOperationException();

                var name = entities[0].Name;
                if (!entities.All(e => e.Name == name))
                    throw new InvalidOperationException();

                if (ruleEntities.Length > 0)
                {
                    if (ruleEntities[0] is ExplicitRule && ruleEntities.Length == 1)
                    {
                        info = new RuleInfo(this.FullName, ruleEntities[0] as ExplicitRule);
                    }
                    else if (ruleEntities.All(r => r is ExtensibleRule))
                    {
                        info = new RuleInfo(this.FullName, ruleEntities.Cast<ExtensibleRule>().ToArray());
                    }
                    else
                    {
                        throw new NotImplementedException("");
                    }
                }
                else
                {
                    info = null;
                }

                return info != null;
            }

            public void SetParamExpression(RuleParameter paramInfo, RuleExpression expr, NavContext paramValueContext)
            {
                if (_ruleParamExpressions == null)
                    _ruleParamExpressions = new Dictionary<string, RuleParameterInfo>();

                _ruleParamExpressions.Add(paramInfo.Name, new RuleParameterInfo(paramInfo, expr, paramValueContext));
            }

            public bool TryResolveParamExpression(string name, out RuleParameterInfo expr)
            {
                bool result;

                if (_ruleParamExpressions == null)
                {
                    expr = default(RuleParameterInfo);
                    result = false;
                }
                else
                {
                    result = _ruleParamExpressions.TryGetValue(name, out expr);
                }

                return result;
            }
        }

        Stack<NavContext> _stack = new Stack<NavContext>();
        // IndentedWriter _log = new IndentedWriter("  ");

        public RuleInfo CurrRuleInfo { get { return _stack.Peek().Info; } }
        public string CurrPath { get { return _stack.Peek().FullName; } }
        public ReadOnlyCollection<RuleSetBase> ChildEntities { get { return _stack.Peek().Entities; } }

        public string RuleParentScopeName
        {
            get
            {
                var rule = _stack.Peek();
                if (rule.Info == null)
                    throw new InvalidOperationException();

                return rule.ParentContext.FullName;
            }
        }

        public string RuleFullName
        {
            get
            {
                var rule = _stack.Peek();
                if (rule.Info == null)
                    throw new InvalidOperationException();

                return rule.RuleFullName;
            }
        }

        public GrammarNavigator(params RuleSet[] ruleSets)
        {
            Array.ForEach(ruleSets, rs => rs.Freeze());
            _stack.Push(new NavContext(ruleSets));
        }

        public bool TryGetAttribute(string attrName, out EntityAttribute attr)
        {
            return _stack.Peek().TryGetAttribute(attrName, out attr);
        }

        public ReadOnlyCollection<EntityAttribute> GetAttributes()
        {
            return _stack.Peek().GetAttributes();
        }

        //public bool TryResolveRule(string ruleName, out RuleInfo rule)
        //{
        //    return _stack.Peek().TryResolveRule(ruleName, out rule);
        //}

        public void Enter(string entityName)
        {
            if (!this.TryEnter(entityName))
                throw new InvalidOperationException();
        }

        //public bool TryEnter(string entityName)
        //{
        //    NavContext ctx;

        //    if (_stack.Peek().TryEnter(entityName, out ctx))
        //    {
        //        _stack.Push(ctx);
        //    }
        //    else
        //    {
        //        ctx = null;
        //    }

        //    return ctx != null;
        //}

        public bool TryEnter(string complexName)
        {
            NavContext ctx;
            return this.TryEnterInternal(complexName.Split('.'), out ctx);
        }

        public bool TryEnterRule(string complexName, out RuleInfo info)
        {
            NavContext ctx;
            if (this.TryEnterInternal(complexName.Split('.'), out ctx))
            {
                info = ctx.Info;
                if (info == null)
                    this.Exit();
            }
            else
            {
                info = null;
            }

            return info != null;
        }

        private bool TryEnterInternal(string[] entityNameParts, out NavContext resultContext)
        {
            // _log.Write("entering from [{0}] to [{1}]", _stack.Peek().FullName, string.Join(".", entityNameParts));
            var ctx = _stack.Peek();

            foreach (var namePart in entityNameParts)
            {
                if (!ctx.TryEnter(namePart, out ctx))
                {
                    // _log.WriteLine(" ...fail");
                    resultContext = null;
                    return false;
                }
            }

            // _log.WriteLine(" ...ok").Push();
            _stack.Push(ctx);
            resultContext = ctx;
            return true;
        }

        public void Exit()
        {
            if (!this.TryExit())
                throw new InvalidOperationException();
        }

        public bool TryExit()
        {
            bool ok;

            if (_stack.Count > 1)
            {
                // var oldCtxName = _stack.Peek().FullName;
                _stack.Pop();
                // _log.WriteLine("exiting from [{0}] to [{1}]", oldCtxName, _stack.Peek().FullName).Pop();
                ok = true;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        public bool TryEnterParameterContext(string paramName, out RuleExpression expr)
        {
            bool ok;
            RuleParameterInfo paramInfo;
            if (_stack.Peek().TryResolveParamExpression(paramName, out paramInfo))
            {
                expr = paramInfo.expression;
                ok = true;
                _stack.Push(paramInfo.paramValueContext);
            }
            else
            {
                expr = null;
                ok = false;
            }

            return ok;
        }

        public void SetParamExpression(RuleParameter paramInfo, RuleExpression expr)
        {
            _stack.Peek().SetParamExpression(paramInfo, expr, _stack.Skip(1).First());
        }
    }
}
