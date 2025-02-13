using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastHotKeyForWPF.Generator
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
            hashUsings.Add("using FastHotKeyForWPF;");
            hashUsings.Add("using System.Windows;");
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
        public string GenerateHorKeyComponent()
        {
            return $$"""
                         public uint ModifierKeys
                         {
                             get { return (uint)GetValue(ModifierKeysProperty); }
                             set { SetValue(ModifierKeysProperty, value); }
                         }
                         public static readonly DependencyProperty ModifierKeysProperty =
                             DependencyProperty.Register("ModifierKeys", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnModifierKeysChanged));
                         public static void Inner_OnModifierKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister((uint)e.OldValue,target.TriggerKeys);
                                GlobalHotKey.Register(target);
                                target.OnModifierKeysChanged((uint)e.OldValue, (uint)e.NewValue);
                             }                            
                         }
                         partial void OnModifierKeysChanged(uint oldKeys, uint newKeys);

                         public uint TriggerKeys
                         {
                             get { return (uint)GetValue(TriggerKeysProperty); }
                             set { SetValue(TriggerKeysProperty, value); }
                         }
                         public static readonly DependencyProperty TriggerKeysProperty =
                             DependencyProperty.Register("TriggerKeys", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnTriggerKeysChanged));
                         public static void Inner_OnTriggerKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister(target.ModifierKeys,(uint)e.OldValue);
                                GlobalHotKey.Register(target);
                                target.OnTriggerKeysChanged((uint)e.OldValue, (uint)e.NewValue);
                             }   
                         }
                         partial void OnTriggerKeysChanged(uint oldKeys, uint newKeys);

                         private event HotKeyEventHandler? handlers;
                         public event HotKeyEventHandler Handler
                         {
                             add { handlers += value; }
                             remove { handlers -= value; }
                         }

                         public void Invoke()
                         {
                             OnHotKeyInvoking();

                             handlers?.Invoke(this, new HotKeyEventArgs(ModifierKeys,TriggerKeys));

                             OnHotKeyInvoked();
                         }
                         partial void OnHotKeyInvoking();
                         partial void OnHotKeyInvoked();

                         public void Covered()
                         {
                             OnCovered();
                         }
                         partial void OnCovered();
                   """;
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
