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

			classes = classes.Select(_1 => JoinClass(_1, doxygen, coreNameToEngineName)).ToList();

			foreach(var c in classes)
			{
				codes.Add(BuildClass(c, coreNameToEngineName));
			}
		}

		private static ClassDef JoinClass(ClassDef scClass, DoxygenParser doxygen, Dictionary<string, string> coreNameToEngineName)
		{
			var name = coreNameToEngineName.ContainsKey(scClass.Name) ? coreNameToEngineName[scClass.Name] : scClass.Name;
			var doxygenClass = doxygen.ClassDefs.FirstOrDefault(_2 => _2.Name == name);
			return new ClassDef
			{
				Name = scClass.Name,
				Brief = doxygenClass != null ? doxygenClass.Brief : "",
				Methods = scClass.Methods.Select(_2 => JoinMethod(_2, doxygenClass)).ToList(),
			};
		}

		private static MethodDef JoinMethod(MethodDef csMethod, ClassDef doxygenClass)
		{
			var doxygenMethod = doxygenClass != null ? doxygenClass.Methods.FirstOrDefault(_3 => _3.Name == csMethod.Name) : null;
			return new MethodDef
			{
				Name = csMethod.Name,
				ReturnType = csMethod.ReturnType,
				Brief = doxygenMethod != null ? doxygenMethod.Brief : "",
				BriefOfReturn = doxygenMethod != null ? doxygenMethod.BriefOfReturn : "",
				Parameters = csMethod.Parameters.Select(_3 => JoinParameter(_3, doxygenMethod)).ToList(),
			};
		}

		private static ParameterDef JoinParameter(ParameterDef csParameter, MethodDef doxygenMethod)
		{
			var doxygenParameter = doxygenMethod != null ? doxygenMethod.Parameters.FirstOrDefault(_4 => _4.Name == csParameter.Name) : null;
			return new ParameterDef
			{
				Name = csParameter.Name,
				Type = csParameter.Type,
				Brief = doxygenParameter != null ? doxygenParameter.Brief : "",
			};
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
