using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace XmlPatch
{
    public class File
    {
        public static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = true,
        };

        public static XmlReader CreateReader(System.IO.Stream stream, string baseUri = "")
            => XmlReader.Create(stream, ReaderSettings, baseUri);

        public static XDocument TryLoadXml(string filePath)
        {
            XDocument doc;
            try
            {
                using FileStream stream = System.IO.File.Open(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                using XmlReader reader = CreateReader(stream, Path.GetFullPath(filePath));
                doc = XDocument.Load(reader, LoadOptions.SetBaseUri);
            }
            catch (Exception e)
            {
                return null;
            }
            if (doc?.Root == null)
            {
                return null;
            }
            return doc;
        }
    }
}
