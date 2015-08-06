using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NCalc;
using Newtonsoft.Json;

namespace SuperXml
{
    public class Compiler
    {
        public Compiler()
        {
            Scope = new Dictionary<string, dynamic>();
            RepeaterKey = "ForEach";
            IfKey = "If";
            TemplateKey = "TemplateBlock";
            _isExpressionRegex = new Regex("(?<={{).*?(?=}})");
            _forEachRegex =
                new Regex(@"^\s*([a-zA-Z_]+[\w]*)\s+in\s+(([a-zA-Z][\w]*(\.[a-zA-Z][\w]*)*)|\[(.+)(,\s*.+)*\])\s*$",
                RegexOptions.Singleline);
            _varNameRegex = new Regex(@"[\s|&=!<>+\-*/%^(]([A-Za-z_$]\w*(\.[A-Za-z_][\w()]*)*)");
        }

        public static string RepeaterKey { get; set; }
        public static string IfKey { get; set; }
        public static string TemplateKey  { get; set; }

        private static Regex _isExpressionRegex;
        private static Regex _forEachRegex;
        private static Regex _varNameRegex;

        public Dictionary<string, dynamic> Scope { get; set; }

        public Compiler SetScope(Dictionary<string, dynamic> scope)
        {
            Scope = scope;
            return this;
        }

        public Compiler AddElementToScope(string key, dynamic value)
        {
            Scope.Add(key, value);
            return this;
        }

        /// <summary>
        /// Compiles t XElement with specified URI.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public XElement Compile(string uri)
        {
            using (var r = XmlReader.Create(uri))
            {
                return _compile(r);
            }
        }

        /// <summary>
        /// Compiles to a XElement using the specified stream with default settings. 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public XElement Compile(Stream stream)
        {
            using (var r = XmlReader.Create(stream))
            {
                return _compile(r);
            }
        }

        /// <summary>
        /// Compiles to XElement by using the specified text reader. 
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns></returns>
        public string Compile(TextReader textReader)
        {
            using (var reader = XmlReader.Create(textReader))
            {
                var output = new StringBuilder();
                var ws = new XmlWriterSettings {Indent = true};
                using (var writer = XmlWriter.Create(output, ws))
                {
                    var compiled = _compileV3(reader);
                    compiled.Run(writer);
                }
                return output.ToString();
            }
        }

        /// <summary>
        /// Compiles to XElement with a specified XmlReader
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <returns></returns>
        public XElement Compile(XmlReader xmlReader)
        {
            return _compile(xmlReader);
        }

        private XElement _compile(XmlReader reader)
        {
#if (DEBUG)
            Trace.WriteLine("<<New Compilation Job Started>>");
            var startJob = DateTime.Now;
#endif
            if (!reader.Read()) throw new FileNotFoundException("Root document was not found.");
            var root = XNode.ReadFrom(reader) as XElement;
            //var compiled = root(Scope, true);
#if (DEBUG)
            Trace.WriteLine("<<Compilation Job Finished in " + (DateTime.Now - startJob).TotalMilliseconds + "ms  >>");
#endif
            return null;
        }

        private CompilationElement _compileV3(XmlReader reader)
        {
            var element = new CompilationElement(BufferCommands.NewDocument) {Scope = Scope};
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        element = new CompilationElement(BufferCommands.WriteStartElement)
                        {
                            Name = reader.Name,
                            Parent = element
                        };
                        for (var i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            element.Attributes.Add(new AttributeModel
                            {
                                Name = reader.Name,
                                Value = reader.Value
                            });
                        }
                        if (reader.AttributeCount > 0) reader.MoveToElement();
                        if (reader.IsEmptyElement) goto case XmlNodeType.EndElement;
                        break;
                    case XmlNodeType.Text:
                        new CompilationElement(BufferCommands.WriteString)
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
        
        private class CompilationElement
        {
            private CompilationElement _parent;
            public CompilationElement(BufferCommands type)
            {
                Attributes = new List<AttributeModel>();
                Children = new List<CompilationElement>();
                Type = type;
                Scope = new Dictionary<string, dynamic>();
            }

            private BufferCommands Type { get; }
            public string Name { get; set; }
            public string Value { get; set; }
            public List<AttributeModel> Attributes { get; private set; }
            public CompilationElement Parent
            {
                get { return _parent; }
                set
                {
                    _parent = value;
                    _parent?.Children.Add(this);
                }
            }
            public List<CompilationElement> Children { get; private set; }
            public Dictionary<string, dynamic> Scope { get; set; }
            public bool RequiresExpansion
            {
                get
                {
                    return Attributes.Any(x => x.Name == RepeaterKey);
                }
            }

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
                var items = scopeName.Length > 0
                    ? GetValueFromScope(scopeName)
                    : JsonConvert.DeserializeObject<dynamic>(match.Groups[2].ToString());

                var i = 0;

                foreach (var item in items)
                {
                    yield return new Dictionary<string, dynamic>
                    {
                        [repeater] = item,
                        ["$index"] = i++
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

                foreach (Match match in _varNameRegex.Matches(" " + expression))
                {
                    var g = match.Groups[1].Value;
                    dynamic varValue = GetValueFromScope(g);
                    expression = expression.Replace(g, "[p" + p + "]");
                    parameters.Add("p" + p, varValue);
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

            private CompilationElement Clone(CompilationElement source, CompilationElement parent, Dictionary<string,dynamic> scope)
            {
                var clone = new CompilationElement(source.Type)
                {
                    Name = source.Name,
                    Value = source.Value,
                    Attributes = source.Attributes,
                    Parent = parent,
                    Scope = scope,
                    Children = source.Children
                };
                return clone;
            }

            public void Run(XmlWriter writer)
            {
                switch (Type)
                {
                    case BufferCommands.WriteStartElement:
                        foreach (var scope in Scopes())
                        {
                            Scope = scope ?? Scope;
                            if (!If()) continue;
                            var isTemplate = Name == TemplateKey;
                            if (!isTemplate) writer.WriteStartElement(Name);
                            foreach (var attribute in Attributes.Where(attribute => attribute.Name != RepeaterKey))
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
                    case BufferCommands.WriteString:
                        writer.WriteString(Inject(Value));
                        break;
                }
            }
        }

        private class AttributeModel
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private enum BufferCommands
        {
            WriteStartElement,
            WriteString,
            NewDocument
        }
    }
}
