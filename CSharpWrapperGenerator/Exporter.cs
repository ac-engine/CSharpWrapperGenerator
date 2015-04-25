using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{
	class PropertyDef
	{
		public string Type = string.Empty;
		public string Name = string.Empty;
		public bool HaveGetter = false;
		public bool HaveSetter = false;
	}

	class Exporter
	{
		public void Export(string directory, DoxygenParser doxygen, CSharpParser csharp)
		{
			Dictionary<string, string> coreNameToEngineName = new Dictionary<string, string>();

			Action<string, string> save = (name, contents) =>
			{
				var path = Path.Combine(directory, name + "_Gen.cs");
				File.WriteAllText(path, contents);
			};

			if(!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			foreach(var e in doxygen.Enumdefs.Join(csharp.Enumdefs, x => x.Name, x => x.Name, (o, i) => o))
			{
				save(e.Name, BuildEnum(e));
			}

			var classException = new string[]
			{
				"Core", "Core_Imp", "ace_core", "ace_corePINVOKE"
			};
			csharp.ClassDefs.RemoveAll(x => classException.Contains(x.Name));

			var beRemoved = new List<CSharpParser.ClassDef>();
			foreach(var item in csharp.ClassDefs.Where(x => x.Name.EndsWith("_Imp")))
			{
				var newName = item.Name.Replace("_Imp", "");
				beRemoved.Add(csharp.ClassDefs.Find(x => x.Name == newName));
				coreNameToEngineName[item.Name] = newName;
			}
			beRemoved.ForEach(x => csharp.ClassDefs.Remove(x));

			foreach(var item in csharp.ClassDefs.Where(x => x.Name.StartsWith("Core")))
			{
				coreNameToEngineName[item.Name] = item.Name.Replace("Core", "");
			}

			foreach(var c in csharp.ClassDefs)
			{
				var name = coreNameToEngineName.ContainsKey(c.Name) ? coreNameToEngineName[c.Name] : c.Name;
				save(name, BuildClass(c, coreNameToEngineName));
			}
		}

		private string BuildClass(CSharpParser.ClassDef c, Dictionary<string, string> coreNameToEngineName)
		{
			var methodException = new string[]
			{
				"GetPtr", "getCPtr", "Dispose", "Create"
			};

			c.Methods.RemoveAll(x => methodException.Contains(x.Name));

			foreach(var method in c.Methods)
			{
				if(coreNameToEngineName.ContainsKey(method.ReturnType))
				{
					method.ReturnType = coreNameToEngineName[method.ReturnType];
				}
				foreach(var parameter in method.Parameters)
				{
					if(coreNameToEngineName.ContainsKey(parameter.Type))
					{
						parameter.Type = coreNameToEngineName[parameter.Type];
					}
				}
			}

			Dictionary<string, PropertyDef> properties = BuildProperties(c);

			var name = coreNameToEngineName.ContainsKey(c.Name) ? coreNameToEngineName[c.Name] : c.Name;
			var template = new Templates.ClassGen(name, c, properties.Values);
			return template.TransformText();
		}

		private static Dictionary<string, PropertyDef> BuildProperties(CSharpParser.ClassDef c)
		{
			var properties = new Dictionary<string, PropertyDef>();

			var getters = c.Methods.Where(x => x.Name.StartsWith("Get"))
				.ToArray();

			var setters = c.Methods.Where(x => x.Name.StartsWith("Set"))
				.Where(x => x.Parameters.Count == 1)
				.ToArray();

			c.Methods.RemoveAll(getters.Contains);
			c.Methods.RemoveAll(setters.Contains);

			foreach(var item in getters)
			{
				var type = item.ReturnType;
				var name = item.Name.Replace("Get", "");
				properties[name] = new PropertyDef
				{
					Type = type,
					Name = name,
					HaveGetter = true,
				};
			}

			foreach(var item in setters)
			{
				var name = item.Name.Replace("Set", "");
				var type = item.Parameters[0].Type;
				if(properties.ContainsKey(name))
				{
					if(properties[name].Type == type)
					{
						properties[name].HaveSetter = true;
					}
					else
					{
						throw new Exception("Getter/Setterの不一致");
					}
				}
			}

			return properties;
		}

		private string BuildEnum(DoxygenParser.EnumDef enumDef)
		{
			var template = new Templates.EnumGen(enumDef);
			return template.TransformText();
		}

		void ExportEnum(List<string> sb, DoxygenParser doxygen, CSharpParser csharp)
		{
			// Csharpのswigに存在しているenumのみ出力
			foreach (var e in doxygen.Enumdefs.Where( _=> csharp.Enumdefs.Any(__=> __.Name ==_.Name)))
			{
				sb.Add(@"/// <summary>");
				sb.Add(string.Format(@"/// {0}", e.Brief));
				sb.Add(@"/// </summary>");
				sb.Add(@"public enum " + e.Name + " : int {");

				foreach (var em in e.Members)
				{
					sb.Add(@"/// <summary>");
					sb.Add(string.Format(@"/// {0}", em.Brief));
					sb.Add(@"/// </summary>");
					sb.Add(string.Format(@"{0} = ace.swig.{1}.{2},", em.Name, e.Name, em.Name));
				}

				sb.Add(@"}");
			}
		}
	}
}
