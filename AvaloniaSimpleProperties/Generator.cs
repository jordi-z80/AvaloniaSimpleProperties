/*
Tutorials to make a code generator:
  * https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview  (actually, this is quite good for being a ms tutorial. Start here)
  * https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvaloniaEasyProperties
{
	[Generator]
	public class InjectSourceGenerator : ISourceGenerator
	{
		//=============================================================================
		/// <summary></summary>
		public void Initialize (GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications (() => new SyntaxReceiver ());
		}

		//=============================================================================
		/// <summary></summary>
		public void Execute (GeneratorExecutionContext context)
		{
			//Debugger.Launch ();

			var receiver = context.SyntaxReceiver as SyntaxReceiver;
			if (receiver == null) return;

			foreach (ClassDeclarationSyntax classDeclaration in receiver.Classes)
			{
				// no need to generate code if the class does not have any fields with our attributes
				if (!HasExtensionFields (classDeclaration)) continue;

				string code = GeneratePartialClassCode (context, classDeclaration);

				if (!String.IsNullOrWhiteSpace (code))
				{
					// add the source
					var className = classDeclaration.Identifier.Text;
					context.AddSource ($"{className}.AvaloniaEasyProperties.cs", SourceText.From (code, Encoding.UTF8));
				}
			}

		}

		//=============================================================================
		/// <summary>Checks if the class has fields that we manage in this extension</summary>
		private bool HasExtensionFields (ClassDeclarationSyntax classDeclaration)
		{
			return classDeclaration.Members.OfType<FieldDeclarationSyntax> ()
				.Any (f => f.AttributeLists.Any (al => al.Attributes.Any (a => IsExtensionAttribute (a))));
		}


		//=============================================================================
		/// <summary></summary>
		bool IsExtensionAttribute (string name)
		{
			if (name == nameof (SimpleStyledProperty)) return true;
			if (name == nameof (SimpleAttachedProperty)) return true;
			return false;
		}

		//=============================================================================
		/// <summary>Detects the attributes we manage in this extension.</summary>
		bool IsExtensionAttribute (AttributeSyntax attributeSyntax)
		{
			// there's probably a better way to do this
			if ( IsExtensionAttribute (attributeSyntax.Name.ToString ())) return true;
			return false;
		}

		//=============================================================================
		/// <summary></summary>
		private string GetNamespace (ClassDeclarationSyntax classDeclaration)
		{
			string namespaceName = null;

			// I think this catches namespace XYZZY {} but not namespace XYZZY;
			var nsDecl = classDeclaration.Ancestors ().OfType<NamespaceDeclarationSyntax> ().FirstOrDefault ();
			if (nsDecl != null)
			{
				namespaceName = nsDecl.Name?.ToString ();
			}

			if (namespaceName == null)
			{
				// This is 4.7+ only. I think we're stuck to 3.8 I'm not sure why (maybe avalonia requirement)
				/*
				var fileScopedNsDecl = classDeclaration.Ancestors ().OfType<FileScopedNamespaceDeclarationSyntax> ().FirstOrDefault ();
				if (fileScopedNsDecl != null)
				{
					namespaceName = fileScopedNsDecl.Name?.ToString ();
				}
				*/

				// we'll use a lame alternative
				var fileScopedNsDecl = classDeclaration.Ancestors ().
					Where (ancestor => ancestor.GetType ().Name == "FileScopedNamespaceDeclarationSyntax")
					.FirstOrDefault ();

				if (fileScopedNsDecl != null)
				{
					// use reflection to get Name
					var nameProperty = fileScopedNsDecl.GetType ().GetProperty ("Name");
					namespaceName = nameProperty.GetValue (fileScopedNsDecl).ToString ();
				}
			}

			return namespaceName;
		}

		//=============================================================================
		/// <summary></summary>
		string GeneratePartialClassCode (GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
		{
			string _namespace = GetNamespace (classDeclaration);
			string className = classDeclaration.Identifier.Text;


			// Get the fields that we want to generate code for
			var fields = classDeclaration.Members.OfType<FieldDeclarationSyntax> ()
				.Where (f => f.AttributeLists.Any (al => al.Attributes.Any ( a => IsExtensionAttribute (a) )));

			string generatedCode = "";

			// we assign each variable to itself to avoid warnings. This is because the initial variable
			// declaration is just not used. This is a hack to avoid the warning. 
			string warningKiller = "";

			foreach (var field in fields)
			{
				string fieldName = field.Declaration.Variables.First ().Identifier.Text;
				string type = field.Declaration.Type.ToString ();
				if (!fieldName.StartsWith ("_")) throw new Exception ("Field name must start with _");

				var attribute = field.AttributeLists
					.SelectMany (al => al.Attributes)
					.FirstOrDefault (a => IsExtensionAttribute (a.Name.ToString ()));

				// this should never happend
				if (attribute == null) continue;

				// use a dictionary if this grows
				switch (attribute.Name.ToString ())
				{
					case nameof (SimpleStyledProperty):
						generatedCode += GenerateCodeForSimpleStyledProperty (className, fieldName, type, attribute);
						break;
					case nameof (SimpleAttachedProperty):
						generatedCode += GenerateCodeForSimpleAttachedProperty (className, fieldName, type, attribute);
						break;
					default:
						throw new Exception ($"Unknown attribute {attribute.Name}");
				}

				warningKiller += $"{fieldName}={fieldName};\n";


			}





			string rv = $@"
using System;
using Avalonia;
using Avalonia.Media;				// IBrush, ...
using Avalonia.Media.Imaging;		// Bitmap, ...
using Avalonia.Controls.Primitives;	// TemplatedControl
using System.Windows.Input;			// ICommand
namespace {_namespace}
{{
	public partial class {className}
	{{
{generatedCode}
		
// disable self-assignment warning
#pragma warning disable CS1717
void warningKiller ()
{{
	{warningKiller}
}}
	}}
}}
";

			return rv;
		}

		private string GenerateCodeForSimpleAttachedProperty (string className, string fieldName, string type, AttributeSyntax attribute)
		{
			string modFieldName = fieldName.Substring (1);
			modFieldName = char.ToUpper (modFieldName[0]) + modFieldName.Substring (1);

			string rv = $@"
public static readonly AttachedProperty<{type}> {modFieldName}Property = AvaloniaProperty.RegisterAttached<{className}, TemplatedControl, {type}> (nameof ({modFieldName}));

public {type} {modFieldName}
{{
	get => GetValue ({modFieldName}Property);
	set => SetValue ({modFieldName}Property, value);
}}
";
			return rv;
		}


		//=============================================================================
		/// <summary></summary>
		private string GenerateCodeForSimpleStyledProperty (string className, string fieldName, string type, AttributeSyntax attribute)
		{
			string modFieldName = fieldName.Substring (1);
			modFieldName = char.ToUpper (modFieldName[0]) + modFieldName.Substring (1);

			string rv = $@"
public static readonly StyledProperty<{type}> {modFieldName}Property = AvaloniaProperty.Register<{className}, {type}> (nameof ({modFieldName}));

public {type} {modFieldName}
{{
	get => GetValue ({modFieldName}Property);
	set => SetValue ({modFieldName}Property, value);
}}
";
			return rv;
		}

		//=============================================================================
		/// <summary></summary>
		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax> ();

			public void OnVisitSyntaxNode (SyntaxNode syntaxNode)
			{
				if (syntaxNode is ClassDeclarationSyntax)
				{
					Classes.Add ((ClassDeclarationSyntax)syntaxNode);
				}
			}
		}

	}
}
