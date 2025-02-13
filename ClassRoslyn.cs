using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MinimalisticWPF.Generator
{
    internal class ClassRoslyn
    {
        internal ClassRoslyn(ClassDeclarationSyntax classDeclarationSyntax, INamedTypeSymbol namedTypeSymbol)
        {
            Syntax = classDeclarationSyntax;
            Symbol = namedTypeSymbol;
        }

        public ClassDeclarationSyntax Syntax { get; private set; }
        public INamedTypeSymbol Symbol { get; private set; }

        public static HashSet<string> GetReferencedNamespaces(INamedTypeSymbol namedTypeSymbol)
        {
            HashSet<string> namespaces = [];

            var syntaxRef = namedTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
            {
                return namespaces;
            }
            var classDecl = syntaxRef.GetSyntax() as ClassDeclarationSyntax;
            if (classDecl == null)
            {
                return namespaces;
            }
            var compilationUnit = classDecl.FirstAncestorOrSelf<CompilationUnitSyntax>();
            if (compilationUnit == null)
            {
                return namespaces;
            }

            foreach (var usingDirective in compilationUnit.Usings)
            {
                if (usingDirective != null)
                {
                    if (usingDirective.Name != null)
                    {
                        namespaces.Add($"using {usingDirective.Name};");
                    }
                }
            }

            return namespaces;
        }

        public string GenerateUsing()
        {
            StringBuilder sourceBuilder = new();
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine();
            var hashUsings = GetReferencedNamespaces(Symbol);
            hashUsings.Add("using FastHotKeyForWPF.Custom;");
            foreach (var use in hashUsings)
            {
                sourceBuilder.AppendLine(use);
            }
            sourceBuilder.AppendLine();
            return sourceBuilder.ToString();
        }
        public string GenerateNamespace()
        {
            StringBuilder sourceBuilder = new();
            sourceBuilder.AppendLine($"namespace {Symbol.ContainingNamespace}");
            sourceBuilder.AppendLine("{");
            return sourceBuilder.ToString();
        }
        public string GeneratePartialClass(bool ignoreViewModel = false, bool ignorePool = false, bool ignoreTheme = false)
        {
            StringBuilder sourceBuilder = new();
            string share = $"   {Syntax.Modifiers} class {Syntax.Identifier.Text} : IHotKeyComponent";
            sourceBuilder.AppendLine(share);
            sourceBuilder.AppendLine("   {");

            return sourceBuilder.ToString();
        }
        public string GenerateEnd()
        {
            StringBuilder sourceBuilder = new();
            sourceBuilder.AppendLine("   }");
            sourceBuilder.AppendLine("}");
            return sourceBuilder.ToString();
        }
    }
}
