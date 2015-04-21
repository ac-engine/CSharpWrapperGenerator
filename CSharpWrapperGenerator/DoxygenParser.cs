using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{

	/// <summary>
	/// Doxygenのxml解析
	/// </summary>
	class DoxygenParser
	{
		public List<EnumDef> Enumdefs = new List<EnumDef>();

		public void AddNamespaceFile(string path)
		{
			var doc = System.Xml.Linq.XDocument.Load(path);

			var doxygen = doc.Document.Elements("doxygen").FirstOrDefault();
			var compound = doxygen.Elements("compounddef").FirstOrDefault();
			var section_enum = compound.Elements("sectiondef").Where(_ => _.Attribute("kind") != null && _.Attribute("kind").Value == "enum").FirstOrDefault();

			// enum
			if (section_enum != null)
			{
				var enumdefs = section_enum.Elements().Where(_ => _.Attribute("kind") != null && _.Attribute("kind").Value == "enum");

				foreach (var enumdef in enumdefs)
				{
					var edef = new EnumDef();

					{
						// 名前
						edef.Name = enumdef.Element("name").Value;

						// 要約
						var briefdescription = enumdef.Element("briefdescription");
						if (briefdescription != null && briefdescription.Element("para") != null)
						{
							var para = briefdescription.Element("para");
							edef.Brief = para.Value;
						}
					}


					// メンバー
					var enumvalues = enumdef.Elements("enumvalue");
					foreach (var enumvalue in enumvalues)
					{
						var emd = new EnumMemberDef();

						// 名前
						emd.Name = enumvalue.Element("name").Value;

						// 要約
						var briefdescription = enumvalue.Element("briefdescription");
						if (briefdescription != null && briefdescription.Element("para") != null)
						{
							var para = briefdescription.Element("para");
							emd.Brief = para.Value;
						}

						edef.Members.Add(emd);
					}

					Enumdefs.Add(edef);
				}
			}
		}

		public class EnumDef
		{
			public string Name = string.Empty;
			public string Brief = string.Empty;
			public List<EnumMemberDef> Members = new List<EnumMemberDef>();
		}

		public class EnumMemberDef
		{
			public string Name = string.Empty;
			public string Brief = string.Empty;
		}
	}
}
