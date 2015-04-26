using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{
	class EnumDef
	{
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public List<EnumMemberDef> Members = new List<EnumMemberDef>();
	}

	class EnumMemberDef
	{
		public string Name = string.Empty;
		public string Brief = string.Empty;
	}

	class ClassDef
	{
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public List<MethodDef> Methods = new List<MethodDef>();
		public List<PropertyDef> Properties = new List<PropertyDef>();

		public override string ToString()
		{
			return string.Format("ClassDef {0}, Method x {1}", Name, Methods.Count);
		}
	}

	class MethodDef
	{
		public string ReturnType = string.Empty;
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public string BriefOfReturn = string.Empty;
		public List<ParameterDef> Parameters = new List<ParameterDef>();

		public override string ToString()
		{
			return string.Format("MethodDef {0}, Parameters x {1}", Name, Parameters.Count);
		}
	}

	class ParameterDef
	{
		public string Type = string.Empty;
		public string Name = string.Empty;
		public string Brief = string.Empty;

		public override string ToString()
		{
			return string.Format("ParameterDef {0} {1}", Type, Name);
		}
	}

	class PropertyDef
	{
		public string Type = string.Empty;
		public string Name = string.Empty;
		public bool HaveGetter = false;
		public bool HaveSetter = false;
		public string Brief = string.Empty;
	}
}
