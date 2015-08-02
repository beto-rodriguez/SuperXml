using System.Collections.Generic;
using System.Xml;

namespace SuperXml
{
    public class Compiler
    {
        public Dictionary<string, dynamic> Scope { get; set; }
        public XmlNode Node { get; set; }

        public Compiler SetDocument(XmlNode xmlNode)
        {
            Node = xmlNode;
            return this;
        }

        public Compiler SetDocumentFromFile(string path)
        {
            var readerSettings = new XmlReaderSettings { IgnoreComments = true };
            using (var reader = XmlReader.Create(path, readerSettings))
            {
                var xml = new XmlDocument();
                xml.Load(reader);

                Node = xml.DocumentElement;
            }
            return this;
        }

        public Compiler SetScope(Dictionary<string, dynamic> scope)
        {
            Scope = scope;
            return this;
        }

        public Compiler AddElementToScope(string key, dynamic value)
        {
            if (Scope == null) Scope = new Dictionary<string, dynamic>();
            Scope.Add(key, value);
            return this;
        }

        public XmlNode Compile()
        {
            Scope = Scope ?? new Dictionary<string, dynamic>();
            if (Node == null) return null;
            return Node.Compile(Scope);
        }
    }
}
