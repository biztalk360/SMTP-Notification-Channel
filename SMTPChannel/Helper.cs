#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

#endregion

namespace B360.Notifier.SMTP
{
    public class Helper
    {
        internal static string GetResourceFileContent(string filename)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetName().Name;

            using (Stream stream = assembly.GetManifestResourceStream(name + "." + filename))
            {
                if (stream == null) throw new Exception(string.Format("Cannot read {0} make sure the file exists and it's an embedded resource", filename));
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string ToXml<T>(T obj) where T
            : new()
        {
            var stringwriter = new StringWriter();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stringwriter, obj);
            return stringwriter.ToString();
        }
        public static string TransformXMLToHTML(string xsltString, string xmlData)
        {
            XslCompiledTransform transform = new XslCompiledTransform();

            using (XmlReader reader = XmlReader.Create(new StringReader(xsltString)))
            {
                transform.Load(reader);
            }
            UTF8StringWriter results = new UTF8StringWriter();
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData)))
            {
                transform.Transform(reader, null, results);
            }
            return results.ToString();
        }
        public class UTF8StringWriter : StringWriter
        {
            public UTF8StringWriter() { }
            public UTF8StringWriter(IFormatProvider formatProvider) : base(formatProvider) { }
            public UTF8StringWriter(StringBuilder sb) : base(sb) { }
            public UTF8StringWriter(StringBuilder sb, IFormatProvider formatProvider) : base(sb, formatProvider) { }

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }
        }
    }
}
