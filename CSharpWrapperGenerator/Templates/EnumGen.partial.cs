using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator.Templates
{
	partial class EnumGen
	{
		private readonly DoxygenParser.EnumDef enumDef;

		internal EnumGen(DoxygenParser.EnumDef enumDef)
		{
			this.enumDef = enumDef;
		}
	}
}
