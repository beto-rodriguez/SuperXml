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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NCalc;

namespace Templator
{
    public class Compiler
    {
        public Compiler()
        {
            Scope = new Dictionary<string, dynamic>();
            RepeaterKey = "Tor.Repeat";
            IfKey = "Tor.If";
            TemplateKey = "Tor.Run";
            _isExpressionRegex = new Regex("(?<={{).*?(?=}})");
            _forEachRegex =
                new Regex(@"^\s*([a-zA-Z_]+[\w]*)\s+in\s+(([a-zA-Z][\w]*(\.[a-zA-Z][\w]*)*)|\[(.+)(,\s*.+)*\])\s*$",
                RegexOptions.Singleline);
        }

        public static string RepeaterKey { get; set; }
        public static string IfKey { get; set; }
        public static string TemplateKey  { get; set; }

        private static Regex _isExpressionRegex;
        private static Regex _forEachRegex;
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
            '_', '$', '1','2','3','4','5','6','7','8','9','0', '.'
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
            private dynamic GetValueFromScope(string propertyName)
            {
                try
                {
                    var keys = propertyName.Split('.');
                    var root = keys[0];

                    var scope = Scope;
                    var parent = this;
                    while (!scope.ContainsKey(root))
                    {
                        parent = parent.Parent;
                        scope = parent.Scope;
                    }

                    var obj = scope[root];
                    if (keys.Length == 1) return obj;

                    var level = 1;
                    do
                    {
                        obj = obj.GetType().GetProperty(keys[level]).GetValue(obj, null);
                        level++;
                    } while (level < keys.Length);

                    return obj;
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

                if (!_forEachRegex.IsMatch(expression))
                    throw new FormatException(
                        "Compilation Error: ForEach was expecting an expression like " +
                        "'varName in [value1, value2, value3..., valueN]'");

                var match = _forEachRegex.Match(expression);
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
                        ["$even"] = even
                    };
                }
            }

            private bool If()
            {
                var at = Attributes.FirstOrDefault(x => x.Name == IfKey);
                var expression = at?.Value;
                if (string.IsNullOrEmpty(expression)) return true;
                var e = Evaluate(expression);
                bool res;
                var couldConvert = bool.TryParse(e, out res);
                return couldConvert && res;
            }

            private List<string> _varCache;

            private string Inject(string expression)
            {
                foreach (var v in _isExpressionRegex.Matches(expression).Cast<Match>()
                            .GroupBy(x => x.Value).Select(varGroup => varGroup.First().Value))
                {
                    expression = expression.Replace("{{" + v + "}}", Evaluate(v));
                }
                return expression;
            }

            private string Evaluate(string expression)
            {
                var originalExpression = expression;
                var p = 0;
                var parameters = new Dictionary<string, object>();

                foreach (var varName in GetVarNames(expression))
                {
                    dynamic value = GetValueFromScope(varName);
                    expression = expression.Replace(varName, "[p" + p + "]");
                    parameters.Add("p" + p , value);
                    p++;
                }

                if (string.IsNullOrWhiteSpace(expression)) return "";
                var e = new Expression(expression.Replace("&gt;", ">").Replace("&lt;", "<"), EvaluateOptions.NoCache);
                foreach (var parameter in parameters) e.Parameters[parameter.Key] = parameter.Value ?? false;

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

            private IEnumerable<string> GetVarNames(string expression)
            {
                if (_varCache != null)
                {
                    //return it from caché!
                    foreach (var cache in _varCache)
                    {
                        yield return cache;
                    }
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(expression)) { yield return ""; yield break;}
                expression += " "; 
                var isString = false;
                var isReading = false;
                var read = new List<char>();
                _varCache = new List<string>();
                foreach (var c in expression)
                {
                    if (isReading && !isString)
                    {
                        if (ValidContentName.Contains(c))
                        {
                            read.Add(c);
                        }
                        else
                        {
                            isReading = false;
                            var s = new string(read.ToArray());
                            if (!_keyWords.Contains(s))
                            {
                                _varCache.Add(s);
                                yield return s;
                            }
                        }
                    }
                    else
                    {
                        if (ValidStartName.Contains(c) && !isString)
                        {
                            isReading = true;
                            read = new List<char> {c};
                        }
                        if (c == '\'') isString = !isString;
                    }
                }
            }

            public void Run(XmlWriter writer)
            {
                switch (Type)
                {
                    case BufferCommands.NewElement:
                        foreach (var scope in Scopes())
                        {
                            Scope = scope ?? Scope;
                            if (!If()) continue;
                            var isTemplate = Name == TemplateKey;
                            var ns = Attributes.FirstOrDefault(x => x.Name == "xmlns");
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
        }

        public class XmlAttribute
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public enum BufferCommands
        {
            NewElement,
            StringContent,
            NewDocument
        }
    }
}
