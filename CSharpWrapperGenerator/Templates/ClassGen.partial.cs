using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator.Templates
{
	partial class ClassGen
	{
		private readonly CSharpParser.ClassDef classDef;
		private readonly IEnumerable<PropertyDef> properties;

		internal ClassGen(CSharpParser.ClassDef classDef, IEnumerable<PropertyDef> properties)
		{
			this.classDef = classDef;
			this.properties = properties;
		}
	}
}
