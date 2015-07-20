using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{
	class Exporter
	{
		private Settings settings;
		private ParseResult doxygen;
		private ParseResult swig;

		public Exporter(Settings settings, ParseResult doxygen, ParseResult swig)
		{
			this.settings = settings;
			this.doxygen = doxygen;
			this.swig = swig;
		}

		public void Export()
		{
			List<string> codes = new List<string>();

			codes.Add("using System;");
			codes.Add("namespace asd {");

			foreach(var e in doxygen.EnumDefs.Join(swig.EnumDefs, x => x.Name, x => x.Name, (o, i) => o))
			{
				codes.Add(BuildEnum(e));
			}

			AddClasses(codes);

			codes.Add("}");

			File.WriteAllLines(settings.ExportFilePath, codes.ToArray());
		}

		private void AddClasses(List<string> codes)
		{
			List<ClassDef> classes = swig.ClassDefs.ToList();
			Dictionary<string, string> coreNameToEngineName = new Dictionary<string, string>();

			classes.RemoveAll(x => settings.ClassBlackList.Contains(x.Name));

			var beRemoved = new List<string>();
			foreach(var item in classes.Where(x => x.Name.EndsWith("_Imp")))
			{
				var newName = item.Name.Replace("_Imp", "");
				beRemoved.Add(newName);
				coreNameToEngineName[item.Name] = newName;
			}
			classes.RemoveAll(x => beRemoved.Contains(x.Name));

			foreach(var item in classes.Where(x => x.Name.StartsWith("Core")))
			{
				coreNameToEngineName[item.Name] = item.Name.Replace("Core", "");
			}

			foreach(var c in classes)
			{
				var name = coreNameToEngineName.ContainsKey(c.Name) ? coreNameToEngineName[c.Name] : c.Name;
				var doxygenClass = doxygen.ClassDefs.FirstOrDefault(_2 => _2.Name == name);
				if (doxygenClass != null)
				{
					c.Brief = doxygenClass.Brief;
					c.Note = doxygenClass.Note;
				}

				if(settings.ListOfClassWhoseCoreIsPrivate.Contains(c.Name))
				{
					c.CoreIsPrivate = true;
				}

				foreach(var method in c.Methods)
				{
					var doxygenMethod = doxygenClass != null ? doxygenClass.Methods.FirstOrDefault(_3 => _3.Name == method.Name) : null;
					if (doxygenMethod != null)
					{
						method.Brief = doxygenMethod.Brief;
						method.BriefOfReturn = doxygenMethod.BriefOfReturn;
						method.Note = doxygenMethod.Note;
					}

					foreach(var parameter in method.Parameters)
					{
						var doxygenParameter = doxygenMethod != null ? doxygenMethod.Parameters.FirstOrDefault(_4 => _4.Name == parameter.Name) : null;
						parameter.Brief = doxygenParameter != null ? doxygenParameter.Brief : "";
                    }
				}
            }

			foreach(var c in classes)
			{
				codes.Add(BuildClass(c, coreNameToEngineName));
			}
		}

		private string BuildClass(ClassDef c, Dictionary<string, string> coreNameToEngineName)
		{
			c.Methods.RemoveAll(method => settings.MethodBlackList.Any(x =>
			{
				var patterns = x.Split('.');
				if(patterns[0] != "*" && patterns[0] != c.Name)
				{
					return false;
				}
				if(patterns[1] != method.Name)
				{
					return false;
				}
				return true;
			}));

			c.Methods.RemoveAll(method => swig.ClassDefs.Any(x => method.ReturnType == x.Name));

			foreach(var method in c.Methods)
			{
				method.ReturnIsEnum = swig.EnumDefs.Any(x => x.Name == method.ReturnType);
				if(coreNameToEngineName.ContainsKey(method.ReturnType))
				{
					method.ReturnType = coreNameToEngineName[method.ReturnType];
				}
				foreach(var parameter in method.Parameters)
				{
					parameter.IsEnum = swig.EnumDefs.Any(x => x.Name == parameter.Type);
					parameter.IsWrappingObject = swig.ClassDefs.Any(x => x.Name == parameter.Type);
					if(coreNameToEngineName.ContainsKey(parameter.Type))
					{
						parameter.Type = coreNameToEngineName[parameter.Type];
					}
				}
			}

			SetProperties(c);

			var name = coreNameToEngineName.ContainsKey(c.Name) ? coreNameToEngineName[c.Name] : c.Name;
			var template = new Templates.ClassGen(name, c);
			return template.TransformText();
		}

		private void SetProperties(ClassDef c)
		{
			var properties = new Dictionary<string, PropertyDef>();

			var getters = c.Methods.Where(x => x.Name.StartsWith("Get"))
				.Where(x => x.Parameters.Count == 0)
				.Where(x => x.ReturnType != "void")
				.ToArray();

			var setters = c.Methods.Where(x => x.Name.StartsWith("Set"))
				.Where(x => x.Parameters.Count == 1)
				.Where(x => x.ReturnType == "void")
				.ToArray();

			c.Methods.RemoveAll(getters.Contains);
			c.Methods.RemoveAll(setters.Contains);

			foreach(var item in getters)
			{
				var name = item.Name.Replace("Get", "");
				var start取得する = item.Brief.IndexOf("を取得する");
				properties[name] = new PropertyDef
				{
					Type = item.ReturnType,
					Name = name,
					HaveGetter = true,
					Brief = start取得する != -1 ? item.Brief.Remove(start取得する) : "",
					IsEnum = item.ReturnIsEnum,
				};
			}

			foreach(var item in setters)
			{
				var name = item.Name.Replace("Set", "");
				var type = item.Parameters[0].Type;
				if(swig.ClassDefs.Any(x => x.Name == type))
				{
					continue;
				}

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
				else
				{
					var start設定する = item.Brief.IndexOf("を設定する");
					properties[name] = new PropertyDef
					{
						Type = type,
						Name = name,
						HaveSetter = true,
						Brief = start設定する != -1 ? item.Brief.Remove(start設定する) : "",
						IsEnum = item.Parameters[0].IsEnum,
					};
				}
				properties[name].IsRefForSet = item.Parameters[0].IsRef;
			}

			foreach(var property in properties.Values)
			{
				if(property.Brief == string.Empty)
				{
					continue;
				}

				var verbs = new List<string>();
				if(property.HaveGetter)
				{
					verbs.Add("取得");
				}
				if(property.HaveSetter)
				{
					verbs.Add("設定");
				}
				property.Brief += "を" + string.Join("または", verbs) + "する。";
			}

			c.Properties = new List<PropertyDef>(properties.Values);
		}

		private string BuildEnum(EnumDef enumDef)
		{
			var template = new Templates.EnumGen(enumDef);
			return template.TransformText();
		}

		void ExportEnum(List<string> sb, DoxygenParser doxygen, CSharpParser csharp)
		{
			// Csharpのswigに存在しているenumのみ出力
			foreach(var e in doxygen.EnumDefs.Where(_ => csharp.Enumdefs.Any(__ => __.Name == _.Name)))
			{
				sb.Add(@"/// <summary>");
				sb.Add(string.Format(@"/// {0}", e.Brief));
				sb.Add(@"/// </summary>");
				sb.Add(@"public enum " + e.Name + " : int {");

				foreach(var em in e.Members)
				{
					sb.Add(@"/// <summary>");
					sb.Add(string.Format(@"/// {0}", em.Brief));
					sb.Add(@"/// </summary>");
					sb.Add(string.Format(@"{0} = asd.swig.{1}.{2},", em.Name, e.Name, em.Name));
				}

				sb.Add(@"}");
			}
		}
	}
}
