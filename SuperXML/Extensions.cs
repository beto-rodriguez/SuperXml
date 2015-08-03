using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NCalc;
using Newtonsoft.Json;

namespace SuperXml
{
    public static class Extensions
    {
        static Extensions()
        {
            IsExpressionRegex = new Regex("(?<={{).*?(?=}})");
            ForEachRegex = 
                new Regex(@"^\s*([a-zA-Z_]+[\w]*)\s+in\s+(([a-zA-Z][\w]*(\.[a-zA-Z][\w]*)*)|\[(.+)(,\s*.+)*\])\s*$");
            IsXmlTagClosedRegex = new Regex(@"^<.*\/\s*>.*$", RegexOptions.Singleline);
        }

        private static readonly Regex IsExpressionRegex;
        private static readonly Regex ForEachRegex;
        private static readonly Regex IsXmlTagClosedRegex;

        public static XmlNode Compile(this XmlNode node, Dictionary<string, dynamic> s)
        {
            var content = node.CompileString(s);
            var doc = new XmlDocument();
            doc.LoadXml(content);
            return doc.DocumentElement;
        }

        private static string CompileString(this XmlNode node, Dictionary<string, dynamic> s)
        {
            if (!node.If(s)) return "";

            var xml = "";

            if (node.NodeType == XmlNodeType.Element)
            {
                foreach (var expanded in node.ForEach(s))
                {
                    var isExpression = node.Name == "Expression";
                    xml += !isExpression
                        ? node.OuterXml.Substring(0, node.OuterXml.IndexOf(">", StringComparison.Ordinal) + 1)
                        : "";
                    var nodeMarkUp = xml;

                    foreach (Match e in IsExpressionRegex.Matches(xml))
                    {
                        xml = xml.Replace("{{" + e.Value + "}}",
                            EvaluateExpression(e.Value, expanded.Scope));
                    }

                    foreach (XmlNode child in node.ChildNodes)
                    {
                        xml += child.CompileString(expanded.Scope);
                    }

                    xml += (!IsXmlTagClosedRegex.IsMatch(nodeMarkUp) && !isExpression
                        ? "</" + node.Name + ">"
                        : "");
                }
                return xml;
            }
            foreach (Match e in IsExpressionRegex.Matches(node.InnerText))
            {
                xml = node.InnerText.Replace("{{" + e.Value + "}}", EvaluateExpression(e.Value, s));
            }
            return xml;
        }

        private static IEnumerable<ExpandedNode> ForEach(this XmlNode node, Dictionary<string, dynamic> s)
        {
            if (node == null) yield break;
            var exp = node.ReadAttrAsString("ForEach");
            if (exp == null)
            {
                yield return new ExpandedNode { Node = node, Scope = s };
                yield break;
            }

            exp = exp.Replace(Environment.NewLine, "");
                
            if (!ForEachRegex.IsMatch(exp))
                throw new Exception(
                    "Compilation Error: ForEach was expecting an expression like " +
                    "'varName in [value1, value2, value3..., valueN]'");

            var match = ForEachRegex.Match(exp);
            var repeater = match.Groups[1].ToString();
            var scopeName = match.Groups[3].ToString();
            var scopeRoot = scopeName.Split('.')[0];
            var items = scopeName.Length > 0
                ? (s.ContainsKey(scopeRoot) ? NavigateTo(s[scopeRoot], scopeName) : new int[] { })
                : JsonConvert.DeserializeObject<dynamic>(match.Groups[2].ToString());

            var cs = s.ToDictionary(x => x.Key, x => x.Value);

            Action<string, dynamic> addToScope = (key, value) =>
            {
                if (cs.ContainsKey(key)) cs[key] = value;
                else cs.Add(key, value);
            };

            var i = 0;
            foreach (var item in items)
            {
                addToScope(repeater, item);
                addToScope("$index", i++);
                yield return new ExpandedNode { Node = node, Scope = cs };
            }
        }

        private static bool If(this XmlNode node, Dictionary<string, dynamic> s)
        {
            if (node == null) return false;
            var exp = node.ReadAttr("If");
            if (exp == null) return true;

            var eval = EvaluateExpression(exp, s);

            bool res;
            var couldConvert = bool.TryParse(eval, out res);

            return couldConvert && res;
        }

        public static string EvaluateExpression(string expression, Dictionary<string, dynamic> scope)
        {
            var originalExpression = expression;
            var p = 0;
            var parameters = new Dictionary<string, object>();
            var regex = new Regex(@"[\s|&=!<>+\-*/%^(]([A-Za-z_$]\w*(\.[A-Za-z_][\w()]*)*)");

            foreach (Match match in regex.Matches(" " + expression))
            {
                var g = match.Groups[1].Value;
                var varName = g.Split('.')[0].Trim();
                dynamic varValue = scope.ContainsKey(varName)
                    ? NavigateTo(scope[varName], g)
                    : "";
                expression = expression.Replace(g, "[p" + p + "]");
                parameters.Add("p" + p, varValue);
                p++;
            }
            if (string.IsNullOrWhiteSpace(expression)) return "";
            var e = new Expression(expression.Replace("&gt;", ">").Replace("&lt;", "<"));
            foreach (var parameter in parameters) e.Parameters[parameter.Key] = parameter.Value;

            try
            {
                var result = e.Evaluate().ToString();
                return result;
            }
            catch
            {
                return originalExpression;
            }
        }

        private static dynamic NavigateTo(dynamic obj, string propertyName)
        {
            var name = propertyName.Split('.').ToList();
            if (name.Count == 1) return obj;

            var level = 1;
            do
            {
                obj = obj.GetType().GetProperty(name[level]).GetValue(obj, null);
                level++;
            } while (level < name.Count);

            return obj;
        }

        private class ExpandedNode
        {
            public XmlNode Node { get; set; }
            public Dictionary<string, dynamic> Scope { get; set; }
        }

        private static string ReadAttr(this XmlNode node, string attribute, string defaultValue = null)
        {
            if (attribute == null || node.Attributes == null)
                return null;

            return node.Attributes[attribute] == null ? defaultValue : node.Attributes[attribute].Value;
        }

        public static string ReadAttrAsString(this XmlNode node, string attribute, string defaultValue = null)
        {
            if (attribute == null || node.Attributes == null)
                return null;

            return node.Attributes[attribute] == null ? defaultValue : node.Attributes[attribute].Value;
        }

    }
}
