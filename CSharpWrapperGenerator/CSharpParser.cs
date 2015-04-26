using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpWrapperGenerator
{
	class CSharpParser
	{
		public List<EnumDef> Enumdefs = new List<EnumDef>();
		public List<ClassDef> ClassDefs = new List<ClassDef>();

		public void Parse(string[] pathes)
		{
			List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
			foreach(var path in pathes)
			{
				var tree = CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(path));
				syntaxTrees.Add(tree);
			}

			var assemblyPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location);

			var mscorelib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "mscorlib.dll"));

			var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
						"Compilation",
						syntaxTrees: syntaxTrees.ToArray(),
						references: new[] { mscorelib },
						options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
												  Microsoft.CodeAnalysis.OutputKind.ConsoleApplication));

			foreach(var tree in syntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(tree);

				var decl = semanticModel.GetDeclarationDiagnostics();
				var methodBodies = semanticModel.GetMethodBodyDiagnostics();
				var root = semanticModel.SyntaxTree.GetRoot();

				ParseRoot(root, semanticModel);
			}
		}

		void ParseRoot(SyntaxNode root, SemanticModel semanticModel)
		{
			var compilationUnitSyntax = root as CompilationUnitSyntax;

			var usings = compilationUnitSyntax.Usings;
			var members = compilationUnitSyntax.Members;

			foreach(var member in members)
			{
				var namespaceSyntax = member as NamespaceDeclarationSyntax;
				ParseNamespace(namespaceSyntax, semanticModel);
			}
		}

		void ParseNamespace(NamespaceDeclarationSyntax namespaceSyntax, SemanticModel semanticModel)
		{
			var members = namespaceSyntax.Members;

			foreach(var member in members)
			{
				var classSyntax = member as ClassDeclarationSyntax;
				var enumSyntax = member as EnumDeclarationSyntax;
				var structSyntax = member as StructDeclarationSyntax;

				if(enumSyntax != null)
				{
					ParseEnum(enumSyntax, semanticModel);
				}
				if(classSyntax != null)
				{
					ParseClass(classSyntax);
				}
			}
		}

		void ParseClass(ClassDeclarationSyntax classSyntax)
		{
			var classDef = new ClassDef();
			classDef.Name = classSyntax.Identifier.ValueText;

			foreach(var member in classSyntax.Members)
			{
				var methodSyntax = member as MethodDeclarationSyntax;

				if(methodSyntax != null)
				{
					classDef.Methods.Add(ParseMethod(methodSyntax));
				}
			}

			ClassDefs.Add(classDef);
		}

		private MethodDef ParseMethod(MethodDeclarationSyntax methodSyntax)
		{
			var methodDef = new MethodDef();
			methodDef.Name = methodSyntax.Identifier.ValueText;
			methodDef.ReturnType = methodSyntax.ReturnType.GetText().ToString().Trim();

			foreach(var parameter in methodSyntax.ParameterList.Parameters)
			{
				var parameterDef = new ParameterDef();
				parameterDef.Name = parameter.Identifier.ValueText;
				parameterDef.Type = parameter.Type.GetText().ToString().Trim();

				methodDef.Parameters.Add(parameterDef);
			}

			return methodDef;
		}

		void ParseEnum(EnumDeclarationSyntax enumSyntax, SemanticModel semanticModel)
		{
			var enumDef = new EnumDef();

			// 名称
			enumDef.Name = enumSyntax.Identifier.ValueText;
			
			// メンバー
			foreach(var member in enumSyntax.Members)
			{
				var enumMemberDef = new EnumMemberDef();

				// 名称
				enumMemberDef.Name = member.Identifier.ValueText;

				enumDef.Members.Add(enumMemberDef);
			}

			Enumdefs.Add(enumDef);
		}
	}
}
