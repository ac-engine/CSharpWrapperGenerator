using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator.Templates
{
	partial class ClassGen
	{
		private readonly ClassDef classDef;
		private readonly string className;

		internal ClassGen(string className, ClassDef classDef)
		{
			this.className = className;
			this.classDef = classDef;
		}
	}
}
