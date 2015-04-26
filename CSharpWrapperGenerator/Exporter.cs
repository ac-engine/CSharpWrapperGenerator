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
		public void Export(string path, DoxygenParser doxygen, CSharpParser csharp)
		{
			List<string> codes = new List<string>();

			codes.Add("using System;");
			codes.Add("namespace ace {");

			foreach(var e in doxygen.EnumDefs.Join(csharp.Enumdefs, x => x.Name, x => x.Name, (o, i) => o))
			{
				codes.Add(BuildEnum(e));
			}

			AddClasses(codes, doxygen, csharp);

			codes.Add("}");

			File.WriteAllLines(path, codes.ToArray());
		}

		private void AddClasses(List<string> codes, DoxygenParser doxygen, CSharpParser csharp)
		{
			List<ClassDef> classes = csharp.ClassDefs.ToList();
			Dictionary<string, string> coreNameToEngineName = new Dictionary<string, string>();

			var classException = new string[]
			{
				"Core", "Core_Imp", "ace_core", "ace_corePINVOKE"
			};
			classes.RemoveAll(x => classException.Contains(x.Name));

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

			classes = doxygen.ClassDefs.Join(
				classes,
				x => x.Name,
				x => coreNameToEngineName.ContainsKey(x.Name) ? coreNameToEngineName[x.Name] : x.Name,
				(o, i) => new ClassDef
			{
				Name = i.Name,
				Brief = o.Brief,
				Methods = o.Methods.Join(i.Methods, y => y.Name, y => y.Name, (o2, i2) => new MethodDef
				{
					Name = o2.Name,
					Brief = o2.Brief,
					BriefOfReturn = o2.BriefOfReturn,
					ReturnType = i2.ReturnType,
					Parameters = i2.Parameters.Select(z => new ParameterDef
					{
						Name = z.Name,
						Type = z.Type,
						Brief = o2.Parameters.Any(_4 => _4.Name == z.Name) ? o2.Parameters.First(_4 => _4.Name == z.Name).Brief : "",
					}).ToList(),
				}).ToList(),
			}).ToList();

			foreach(var c in classes)
			{
				codes.Add(BuildClass(c, coreNameToEngineName));
			}
		}

		private string BuildClass(ClassDef c, Dictionary<string, string> coreNameToEngineName)
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

			SetProperties(c);

			var name = coreNameToEngineName.ContainsKey(c.Name) ? coreNameToEngineName[c.Name] : c.Name;
			var template = new Templates.ClassGen(name, c);
			return template.TransformText();
		}

		private static void SetProperties(ClassDef c)
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
				else
				{
					var start設定する = item.Brief.IndexOf("を設定する");
					properties[name] = new PropertyDef
					{
						Type = type,
						Name = name,
						HaveSetter = true,
						Brief = start設定する != -1 ? item.Brief.Remove(start設定する) : "",
					};
				}
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
					sb.Add(string.Format(@"{0} = ace.swig.{1}.{2},", em.Name, e.Name, em.Name));
				}

				sb.Add(@"}");
			}
		}
	}
}
