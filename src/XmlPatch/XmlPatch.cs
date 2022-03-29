using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlPatch
{
	public class XmlPatch
	{
		private const string Before = "before";
		private const string After = "after";
		private const string Both = "both";

		public static string GetInnerXML(XElement element)
        {
			var reader = element.CreateReader();
			reader.MoveToContent();
			return reader.ReadInnerXml();
		}

		public static string GetInnerXML(XDocument document)
		{
			var reader = document.CreateReader();
			reader.MoveToContent();
			return reader.ReadInnerXml();
		}

		public static IEnumerable<XAttribute> GetAttributes(XElement element) => from att in element.Attributes() select att;

		public static XmlDocument ToXmlDocument(XDocument xDocument)
		{
			var xmlDocument = new XmlDocument();
			using (var xmlReader = xDocument.CreateReader())
			{
				xmlDocument.Load(xmlReader);
			}
			return xmlDocument;
		}

		public static XDocument ToXDocument(XmlDocument xmlDocument)
		{
			using (var nodeReader = new XmlNodeReader(xmlDocument))
			{
				nodeReader.MoveToContent();
				return XDocument.Load(nodeReader);
			}
		}

		public XDocument Patch(string baseFile, string diffFile)
		{
			var baseDoc = File.TryLoadXml(baseFile);
			var diffDoc = File.TryLoadXml(diffFile);

			return Patch(baseDoc, diffDoc);
		}

		public XDocument Patch(XDocument baseXDoc, XDocument diffXDoc)
		{
			var baseXmlDoc = ToXmlDocument(baseXDoc);
			var diffXmlDoc = ToXmlDocument(diffXDoc);
			foreach (XmlNode node in diffXmlDoc.SelectSingleNode("/diff").ChildNodes)
			{
				var targetNode = baseXmlDoc.SelectSingleNode(node.Attributes["sel"].Value);

				switch (node.Name)
				{
					case "add":
						Add(baseXmlDoc, node, targetNode);
						break;
					case "remove":
						Remove(node, targetNode);
						break;
					case "replace":
						Replace(baseXmlDoc, node, targetNode);
						break;
				}
			}

			return ToXDocument(baseXmlDoc);
		}
		private static void Add(XmlDocument baseDoc, XmlNode diffNode, XmlNode targetNode)
		{
			var typeAttribute = diffNode.Attributes["type"];
			var posAttribute = diffNode.Attributes["pos"];
			string position = "prepend";
			if (posAttribute != null)
			{
				position = posAttribute.Value;
			}

			if (typeAttribute == null)
			{
				foreach (XmlNode diffChild in diffNode.ChildNodes)
				{
					var importNode = baseDoc.ImportNode(diffChild, true);

					switch (position)
					{
						case After:
							targetNode.ParentNode.InsertAfter(importNode, targetNode);
							break;
						case Before:
							targetNode.ParentNode.InsertBefore(importNode, targetNode);
							break;
						default:
							targetNode.AppendChild(importNode);
							break;
					}
				}
				return;
			}

			if (typeAttribute.Value.StartsWith("@"))
			{
				var attribute = baseDoc.CreateAttribute(typeAttribute.Value.Substring(1));
				attribute.Value = diffNode.InnerXml;
				targetNode.Attributes.Append(attribute);

				return;
			}
			if (typeAttribute.Value.StartsWith("namespace::"))
			{
				var attribute = baseDoc.CreateAttribute("xmlns:" + typeAttribute.Value.Substring(11));
				attribute.Value = diffNode.InnerXml;

				targetNode.Attributes.Append(attribute);
				return;
			}

			throw new NotImplementedException();
		}

		private static void Remove(XmlNode diffNode, XmlNode targetNode)
		{
			var whitespace = Both;
			var whitespaceAttr = diffNode.Attributes["ws"];
			if (whitespaceAttr != null)
			{
				whitespace = whitespaceAttr.Value;
				if (whitespace != Before && whitespace != After)
				{
					whitespace = Both;
				}
			}

			if (targetNode.ParentNode is XmlElement)
			{
				var parentNode = (XmlElement)targetNode.ParentNode;
				parentNode.RemoveChild(targetNode);
				parentNode.IsEmpty = whitespace == Both && string.IsNullOrEmpty(parentNode.InnerXml);
				return;
			}
			if (targetNode is XmlAttribute)
			{
				var attribute = (XmlAttribute)targetNode;
				attribute.OwnerElement.RemoveAttributeNode(attribute);
				return;
			}
			throw new NotImplementedException();
		}

		private static void Replace(XmlDocument baseDoc, XmlNode diffNode, XmlNode targetNode)
		{
			if (targetNode is XmlAttribute)
			{
				targetNode.Value = diffNode.InnerXml;
				return;
			}

			foreach (XmlNode diffChild in diffNode.ChildNodes)
			{
				var importNode = baseDoc.ImportNode(diffChild, true);
				targetNode.ParentNode.InsertAfter(importNode, targetNode);
			}
			targetNode.ParentNode.RemoveChild(targetNode);
		}
	}
}