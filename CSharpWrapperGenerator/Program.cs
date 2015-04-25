using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CSharpWrapperGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var doxygenDirPath = "CPP_XML/xml/";
			var swigCSharpDirPath = "swig/";

			var doxygenParser = new DoxygenParser();
			doxygenParser.AddNamespaceFile(doxygenDirPath + "namespaceace.xml");

			var csharpParser = new CSharpParser();
			var cs = System.IO.Directory.EnumerateFiles(swigCSharpDirPath, "*.cs", System.IO.SearchOption.AllDirectories).ToArray();
			csharpParser.Parse(cs);

			var exporter = new Exporter();
			exporter.Export("Gen/", doxygenParser, csharpParser);
		}
	}



}
