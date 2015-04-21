using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{
	class Exporter
	{
		public void Export(string path, DoxygenParser doxygen, CSharpParser csharp)
		{
			List<string> sb = new List<string>();

			sb.Add(@"using System;");
			sb.Add(@"namespace ace {");

			ExportEnum(sb, doxygen, csharp);

			sb.Add(@"}");

			System.IO.File.WriteAllLines(path, sb.ToArray());
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
