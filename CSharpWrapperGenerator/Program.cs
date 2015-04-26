using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace CSharpWrapperGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Settings settings;
			var settingFilePath = "CSharpWrapperGeneratorSettings.json";
			var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Settings));
			using (var file = File.Open(settingFilePath, FileMode.Open))
			{
				settings = serializer.ReadObject(file) as Settings;
			}

			var doxygenParser = new DoxygenParser();
			doxygenParser.AddNamespaceFile(Path.Combine(settings.DoxygenXmlDirPath, "namespaceace.xml"));
			var docs = Directory.EnumerateFiles(settings.DoxygenXmlDirPath, "classace_*.xml", SearchOption.TopDirectoryOnly).ToArray();
			doxygenParser.AddClassFiles(docs);

			var csharpParser = new CSharpParser();
			var cs = Directory.EnumerateFiles(settings.SwigCSharpDirPath, "*.cs", SearchOption.AllDirectories).ToArray();
			csharpParser.Parse(cs);

			var exporter = new Exporter(settings, doxygenParser.Result, csharpParser.Result);
			exporter.Export();
		}
	}



}
