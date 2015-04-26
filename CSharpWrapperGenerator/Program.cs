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
			if(args.Length < 1)
			{
				Console.WriteLine("第１引数に設定ファイルを指定してください");
				return;
			}

			Settings settings;
			var settingFilePath = args[0];
			var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Settings));
			using (var file = File.Open(settingFilePath, FileMode.Open))
			{
				settings = serializer.ReadObject(file) as Settings;
			}

			var settingsDirectory = Path.GetDirectoryName(args[0]);

			var doxygenParser = new DoxygenParser();
			doxygenParser.AddNamespaceFile(Path.Combine(settingsDirectory, settings.DoxygenXmlDirPath, "namespaceace.xml"));
			var docs = Directory.EnumerateFiles(Path.Combine(settingsDirectory, settings.DoxygenXmlDirPath), "classace_*.xml", SearchOption.TopDirectoryOnly).ToArray();
			doxygenParser.AddClassFiles(docs);

			var csharpParser = new CSharpParser();
			var cs = Directory.EnumerateFiles(Path.Combine(settingsDirectory, settings.SwigCSharpDirPath), "*.cs", SearchOption.AllDirectories).ToArray();
			csharpParser.Parse(cs);

			settings.ExportFilePath = Path.Combine(settingsDirectory, settings.ExportFilePath);
			var exporter = new Exporter(settings, doxygenParser.Result, csharpParser.Result);
			exporter.Export();
		}
	}



}
