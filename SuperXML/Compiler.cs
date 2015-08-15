//The MIT License(MIT)

//Copyright(c) 2015 Alberto Rodriguez

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;
using NCalc;

namespace Templator
{
    public class Compiler
    {
        public Compiler()
        {
            Scope = new Dictionary<string, dynamic>();
        }

        static Compiler()
        {
            RepeaterKey = "Tor.Repeat";
            IfKey = "Tor.If";
            TemplateKey = "Tor.Run";
            IsExpressionRegex = new Regex("(?<={{).*?(?=}})");
            ForEachRegex =
                new Regex(@"^\s*([a-zA-Z_]+[\w]*)\s+in\s+(([a-zA-Z][\w]*(\.[a-zA-Z][\w]*)*)|\[(.+)(,\s*.+)*\])\s*$",
                RegexOptions.Singleline);
            Filters = new Dictionary<string, Func<object, string>>
            {
                ["currency"] = x =>
                {
                    //this is simple and dirty to support all numeric types.
                    var s = x.ToString();
                    double d;
                    double.TryParse(s, out d);
                    return d.ToString("C");
                }
            };
        }

        public static string RepeaterKey { get; set; }
        public static string IfKey { get; set; }
        public static string TemplateKey  { get; set; }
        public static Dictionary<string, Func<object, string>> Filters { get; }

        private static readonly Regex IsExpressionRegex;
        private static readonly Regex ForEachRegex;
        private static readonly char[] ValidStartName =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '_', '$'
        };
        private static readonly char[] ValidContentName =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '_', '$', '1','2','3','4','5','6','7','8','9','0', '.', '[', ']' , '(' , ')'
        };

        private static string[] _keyWords = {"if"};

        public XmlWriterSettings XmlWriterSettings { get; set; }

        public Dictionary<string, dynamic> Scope { get; set; }

        public Compiler SetScope(Dictionary<string, dynamic> scope)
        {
            Scope = scope;
            return this;
        }

        public Compiler AddElementToScope(string key, dynamic value)
        {
            if (Scope.ContainsKey(key))
            {
                Scope[key] = value;
            }
            else
            {
                Scope.Add(key, value);
            }
            return this;
        }

        /// <summary>
        /// Compiles a string template
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string CompileString(string input)
        {
            var template = "<Tor.string>"+input+"</Tor.string>";
            using (var reader = XmlReader.Create(new StringReader(template)))
            {
                var output = new StringBuilder();
                var ws = XmlWriterSettings ?? new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true };
                using (var writer = XmlWriter.Create(output, ws))
                {
                    var compiled = _readXml(reader);
                    compiled.Run(writer);
                }
                return output.ToString().Replace("<Tor.string>", "").Replace("</Tor.string>", "");
            }
        }

        /// <summary>
        /// Compiles a Xml template with specified URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="root">
        ///     Set the root to compile, to improve performance. 
        ///     example: x => x.Children.First(y => x.Name == "MyElement")
        /// </param>
        /// <returns></returns>
        public string CompileXml(string uri, Func<XmlElement, XmlElement> root = null)
        {
            using (var reader = XmlReader.Create(uri))
            {
                var output = new StringBuilder();
                var ws = XmlWriterSettings ?? new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true };
                using (var writer = XmlWriter.Create(output, ws))
                {
                    var compiled = _readXml(reader);
                    if (root != null)
                    {
                        compiled = root(compiled);
                    }
                    compiled.Run(writer);
                }
                return output.ToString();
            }
        }

        /// <summary>
        /// Compiles a Xml template using the specified stream with default settings. 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="root">
        ///     Set the root to compile, to improve performance. 
        ///     example: x => x.Children.First(y => x.Name == "MyElement")
        /// </param>
        /// <returns></returns>
        public string CompileXml(Stream stream, Func<XmlElement, XmlElement> root = null)
        {
            using (var reader = XmlReader.Create(stream))
            {
                var output = new StringBuilder();
                var ws = XmlWriterSettings ?? new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true };
                using (var writer = XmlWriter.Create(output, ws))
                {
                    var compiled = _readXml(reader);
                    if (root != null)
                    {
                        compiled = root(compiled);
                    }
                    compiled.Run(writer);
                }
                return output.ToString();
            }
        }

        /// <summary>
        /// Compiles a Xml template by using the specified text reader. 
        /// </summary>
        /// <param name="textReader"></param>
        /// <param name="root">
        ///     Set the root to compile, to improve performance. 
        ///     example: x => x.Children.First(y => x.Name == "MyElement")
        /// </param>
        /// <returns></returns>
        public string CompileXml(TextReader textReader, Func<XmlElement, XmlElement> root = null)
        {
            using (var reader = XmlReader.Create(textReader))
            {
                var output = new StringBuilder();
                var ws = XmlWriterSettings ?? new XmlWriterSettings {Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true};
                using (var writer = XmlWriter.Create(output, ws))
                {
                    var compiled = _readXml(reader);
                    if (root != null)
                    {
                        compiled = root(compiled);
                    }
                    compiled.Run(writer);
                }
                return output.ToString();
            }
        }

        /// <summary>
        /// Compiles a Xml template with a specified XmlReader
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="root">
        ///     Set the root to compile, to improve performance. 
        ///     example: x => x.Children.First(y => x.Name == "MyElement")
        /// </param>
        /// <returns></returns>
        public string CompileXml(XmlReader xmlReader, Func<XmlElement, XmlElement> root = null)
        {
            var output = new StringBuilder();
            var ws = XmlWriterSettings ?? new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true };
            using (var writer = XmlWriter.Create(output, ws))
            {
                var compiled = _readXml(xmlReader);
                if (root != null)
                {
                    compiled = root(compiled);
                }
                compiled.Run(writer);
            }
            return output.ToString();
        }

        private XmlElement _readXml(XmlReader reader)
        {
            var element = new XmlElement(BufferCommands.NewDocument) {Scope = Scope};
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        element = new XmlElement(BufferCommands.NewElement)
                        {
                            Name = reader.Name,
                            Parent = element
                        };
                        for (var i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            element.Attributes.Add(new XmlAttribute
                            {
                                Name = reader.Name,
                                Value = reader.Value
                            });
                        }
                        if (reader.AttributeCount > 0) reader.MoveToElement();
                        if (reader.IsEmptyElement) goto case XmlNodeType.EndElement;
                        break;
                    case XmlNodeType.Text:
                        new XmlElement(BufferCommands.StringContent)
                        {
                            Value = reader.Value,
                            Parent = element
                        };
                        break;
                    case XmlNodeType.EndElement:
                        element = element.Parent;
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                        //ignored
                        break;
                }
            }
            var root = element.Children.First();
            return root;
        }

        public class XmlElement
        {
            private XmlElement _parent;
            public XmlElement(BufferCommands type)
            {
                Attributes = new List<XmlAttribute>();
                Children = new List<XmlElement>();
                Type = type;
                Scope = new Dictionary<string, dynamic>();
            }

            private BufferCommands Type { get; }
            /// <summary>
            /// Name of the Element
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Content of the Element
            /// </summary>
            public string Value { get; set; }
            /// <summary>
            /// Sets Name space to xml Element
            /// </summary>
            /// <summary>
            /// Attributes in the Element
            /// </summary>
            public List<XmlAttribute> Attributes { get;}
            public XmlElement Parent
            {
                get { return _parent; }
                set
                {
                    _parent = value;
                    _parent?.Children.Add(this);
                }
            }
            /// <summary>
            /// Gets the children of this element
            /// </summary>
            public List<XmlElement> Children { get;}
            /// <summary>
            /// Scope of current Element.
            /// </summary>
            public Dictionary<string, dynamic> Scope { get; set; }
            public dynamic GetValueFromScope(string propertyName)
            {
                try
                {
                    var keys = propertyName.Split('.');
                    var property = new PropertyAccess(keys[0]);

                    var scope = Scope;
                    var parent = this;
                    while (!scope.ContainsKey(property.Name))
                    {
                        parent = parent.Parent;
                        scope = parent.Scope;
                    }

                    var obj = scope[property.Name];
                    var level = 1;

                    while (level < keys.Length)
                    {
                        obj = property.GetValue(obj);
                        property = new PropertyAccess(keys[level]);
                        var t = obj.GetType();
                        obj = t == typeof(Dictionary<string, dynamic>) || t.IsArray 
                            ? obj[property.Name]
                            : t.GetProperty(property.Name).GetValue(obj, null);
                        level++;
                    }

                    return property.GetValue(obj);
                }
                catch (Exception)
                {
                    Trace.WriteLine(propertyName + " not found. default value returned = false");
                    return false;
                }
            }
            private IEnumerable<Dictionary<string, dynamic>> Scopes()
            {
                var repeaterAttribute = Attributes.FirstOrDefault(x => x.Name == RepeaterKey);
                if (repeaterAttribute == null)
                {
                    yield return null;
                    yield break;
                }

                var expression = repeaterAttribute.Value;

                if (!ForEachRegex.IsMatch(expression))
                    throw new FormatException(
                        "Compilation Error: ForEach was expecting an expression like " +
                        "'varName in [value1, value2, value3..., valueN]'");

                var match = ForEachRegex.Match(expression);
                var repeater = match.Groups[1].ToString();
                var scopeName = match.Groups[3].ToString();
                var items = GetValueFromScope(scopeName);

                var i = 0;

                foreach (var item in items)
                {
                    var even = i%2 == 0;
                    yield return new Dictionary<string, dynamic>
                    {
                        [repeater] = item,
                        ["$index"] = i++,
                        ["$odd"] = !even,
                        ["$even"] = even,
                        ["$parent"] = Parent.Scope
                    };
                }
            }

            private Dictionary<string, CExpression> _cache;
            private CExpression _ifCache; 

            private bool If()
            {
                var at = Attributes.FirstOrDefault(x => x.Name == IfKey);
                var expression = at?.Value;
                if (string.IsNullOrEmpty(expression)) return true;
                if (_ifCache == null) _ifCache = new CExpression(expression, this);
                var e = _ifCache.Evaluate();
                bool res;
                var couldConvert = bool.TryParse(e, out res);
                return couldConvert && res;
            }

            private string Inject(string expression)
            {
                var buildingCache = false;
                if (_cache == null)
                {
                    buildingCache = true;
                    _cache = new Dictionary<string, CExpression>();
                }
                foreach (var v in IsExpressionRegex.Matches(expression).Cast<Match>()
                            .GroupBy(x => x.Value).Select(varGroup => varGroup.First().Value))
                {
                    if (buildingCache) _cache.Add(v, new CExpression(v, this));
                    expression = expression.Replace("{{" + v + "}}", _cache[v].Evaluate());
                }
                return expression;
            }

            private string Evaluate(string expression, IEnumerable<string> cache)
            {
                var originalExpression = expression;
                var expAndFilt = expression.Split('|');
                expression = expAndFilt[0];
                var p = 0;
                var parameters = new Dictionary<string, object>();

                foreach (var varName in cache)
                {
                    dynamic value = GetValueFromScope(varName);
                    expression = Regex.Replace(expression, (varName.StartsWith("$") ? "[\\$]" : "\\b") + varName.Replace("$", "") + "\\b", "[p" + p + "]");
                    parameters.Add("p" + p , value);
                    p++;
                }

                if (string.IsNullOrWhiteSpace(expression)) return "";
                var e = new Expression(expression.Replace("&gt;", ">").Replace("&lt;", "<"), EvaluateOptions.NoCache);
                foreach (var parameter in parameters) e.Parameters[parameter.Key] = parameter.Value ?? false;

                try
                {
                    var result = e.Evaluate();
                    return expAndFilt.Length > 1
                        ? Filters[expAndFilt[1].Trim()](result)
                        : result.ToString();
                }
                catch
                {
                    return "{{ " + originalExpression + " }}";
                }
            }

            private IEnumerable<string> GetVarNames(string expression)
            {
                var result = new List<string>();
                if (string.IsNullOrWhiteSpace(expression)) return result;

                expression += " ";
                var isString = false;
                var isReading = false;
                var isParameter = false;
                var read = new List<char>();
                foreach (var c in expression)
                {
                    if (isReading && !isString)
                    {
                        if (ValidContentName.Contains(c) || isParameter)
                        {
                            if (c == '(') isParameter = true;
                            if (c == ')') isParameter = false;
                            read.Add(c);
                        }
                        else
                        {
                            isReading = false;
                            var s = new string(read.ToArray());
                            if (!_keyWords.Contains(s))
                            {
                                result.Add(s);
                            }
                        }
                    }
                    else
                    {
                        if (ValidStartName.Contains(c) && !isString)
                        {
                            isReading = true;
                            read = new List<char> { c };
                        }
                        if (c == '\'') isString = !isString;
                    }
                }
                return result;
            }

            public void Run(XmlWriter writer)
            {
                switch (Type)
                {
                    case BufferCommands.NewElement:
                        var isTemplate = Name == TemplateKey;
                        var ns = Attributes.FirstOrDefault(x => x.Name == "xmlns");
                        foreach (var scope in Scopes())
                        {
                            Scope = scope ?? Scope;

                            if (!If()) continue;
                            
                            if (!isTemplate)
                                if (ns != null) writer.WriteStartElement(Name, ns.Value);
                                else writer.WriteStartElement(Name);

                            foreach (var attribute in Attributes.Where(attribute => attribute.Name != RepeaterKey
                                                                                    && attribute.Name != IfKey))
                            {
                                writer.WriteAttributeString(attribute.Name, Inject(attribute.Value));
                            }
                            foreach (var child in Children)
                            {
                                child.Run(writer);
                            }
                            if (!isTemplate) writer.WriteEndElement();
                        }
                        break;
                    case BufferCommands.StringContent:
                        writer.WriteString(Inject(Value));
                        break;
                }
            }

            private class CExpression
            {
                public CExpression(string expression, XmlElement parent)
                {
                    Items = new List<CExpressionItem>();
                    Parent = parent;
                    OriginalExpression = expression;

                    var read = new List<char>();
                    var type = LectureType.Unknow;

                    foreach (var c in expression)
                    {
                        switch (type)
                        {
                            case LectureType.Variable:
                                if (!ValidContentName.Contains(c))
                                {
                                    Items.Add(new CExpressionItem
                                    {
                                        FromScope = true,
                                        Value = new string(read.ToArray())
                                    });
                                    read.Clear();
                                    type = LectureType.Unknow;
                                }
                                break;
                            case LectureType.String:
                                if (c == '\'')
                                {
                                    Items.Add(new CExpressionItem
                                    {
                                        FromScope = false,
                                        Value = new string(read.ToArray())
                                    });
                                    read.Clear();
                                    type = LectureType.Unknow;
                                }
                                break;
                            case LectureType.Constant:
                                if (ValidStartName.Contains(c) || c == '\'' || c == '|')
                                {
                                    Items.Add(new CExpressionItem
                                    {
                                        FromScope = false,
                                        Value = new string(read.ToArray())
                                    });
                                    read.Clear();
                                    type = LectureType.Unknow;
                                }
                                break;
                        }
                        if (type == LectureType.Unknow)
                        {
                            if (ValidStartName.Contains(c))
                            {
                                type = LectureType.Variable;
                            }
                            else
                                switch (c)
                                {
                                    case '\'':
                                        type = LectureType.String;
                                        break;
                                    case '|':
                                        type = LectureType.Filter;
                                        break;
                                    default:
                                        type = LectureType.Constant;
                                        break;
                                }
                        }
                        read.Add(c);
                    }
                    if (type != LectureType.Filter)
                    {
                        Items.Add(new CExpressionItem
                        {
                            FromScope = type == LectureType.Variable,
                            Value = new string(read.ToArray())
                        });
                    }
                    else
                    {
                        Filter = new string(read.ToArray()).Replace("|", "").Trim();
                    }
                }

                private List<CExpressionItem> Items { get; }
                private XmlElement Parent { get; }
                private string OriginalExpression { get; }
                private string Filter { get; set; }

                public string Evaluate()
                {
                    var sb = new StringBuilder();
                    var p = 0;
                    var parameters = new Dictionary<string, object>();
                    foreach (var i in Items)
                    {
                        if (i.FromScope)
                        {
                            sb.Append("[p");
                            sb.Append(p);
                            sb.Append("]");
                            parameters.Add("p" + p, Parent.GetValueFromScope(i.Value) ?? false);   
                            p++;
                        }
                        else
                        {
                            sb.Append(i.Value);
                        }
                    }

                    var s = sb.ToString();
                    if (string.IsNullOrWhiteSpace(s)) return "";
                    var e = new Expression(s.Replace("&gt;", ">").Replace("&lt;", "<"), EvaluateOptions.NoCache)
                    {
                        Parameters = parameters
                    };

                    try
                    {
                        var result = e.Evaluate();
                        return Filter != null 
                            ? Filters[Filter](result)
                            : result.ToString();
                    }
                    catch
                    {
                        Trace.WriteLine("Error Evaluating expression '" + OriginalExpression + "'");
                        return "{{ " + OriginalExpression + " }}";
                    }
                }
            }

            private class CExpressionItem
            {
                public bool FromScope { get; set; }
                public string Value { get; set; }
            }
        }

        public class XmlAttribute
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class PropertyAccess
        {
            public PropertyAccess(string propertyName)
            {
                var ar = propertyName.Split('[', ']');
                Name = ar[0];
                Children = new List<string>();
                for (var i = 1; i < ar.Length -1; i++)
                {
                    Children.Add(ar[i]);
                }
            }

            public string Name { get; set; }
            public List<string> Children { get; set; }

            public dynamic GetValue(dynamic obj)
            {
                dynamic r = obj;
                foreach (var child in Children)
                {
                    if (r.GetType().IsArray)
                    {
                        int index;
                        int.TryParse(child, out index);
                        r = r[index];
                    }
                    else
                    {
                        r = r[child];
                    }
                }
                return r;
            }
        }

        public enum BufferCommands
        {
            NewElement,
            StringContent,
            NewDocument
        }

        private enum LectureType
        {
            Variable, String, Filter, Unknow, Constant
        }
    }
}
