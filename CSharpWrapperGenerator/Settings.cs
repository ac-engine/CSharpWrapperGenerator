using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSharpWrapperGenerator
{
	[DataContract]
	class Settings
	{
		[DataMember]
		public string DoxygenXmlDirPath { get; set; }
		[DataMember]
		public string SwigCSharpDirPath { get; set; }
		[DataMember]
		public string ExportFilePath { get; set; }
		[DataMember]
		public string[] ClassBlackList { get; set; }
		[DataMember]
		public string[] MethodBlackList { get; set; }
		[DataMember]
		public string[] ListOfClassWhoseCoreIsPrivate { get; set; }
	}
}
