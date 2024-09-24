using System.IO;
using System.Xml;
using UnityEditor;

public class CustomAssetPostprocessor : AssetPostprocessor
{
	private static string OnGeneratedCSProject(string path, string content)
	{
		if (!path.Contains("Assembly-CSharp"))
		{
			return content;
		}
		var xml = new XmlDocument();
		xml.LoadXml(content);
		XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
		const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
		nsmgr.AddNamespace("ns", ns);
		XmlNode node;

		node = xml.SelectSingleNode($"//ns:LangVersion", nsmgr);
		node.InnerText = "11.0"; // Unity的csc Preview对应的是C#11

		using var writer = new StringWriter();
		using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "\t",
			NewLineOnAttributes = false
		});
		xml.Save(xmlWriter);
		return writer.ToString();
	}
}
