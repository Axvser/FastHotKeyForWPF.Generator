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
            hashUsings.Remove("using System.Windows.Input;");
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
                         public uint VirtualModifiers
                         {
                             get { return (uint)GetValue(ModifierKeysProperty); }
                             set { SetValue(ModifierKeysProperty, value); }
                         }
                         public static readonly DependencyProperty ModifierKeysProperty =
                             DependencyProperty.Register("VirtualModifiers", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnModifierKeysChanged));
                         public static void Inner_OnModifierKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister((uint)e.OldValue,target.VirtualKeys);
                                GlobalHotKey.Register(target);
                                target.OnModifierKeysChanged((uint)e.OldValue, (uint)e.NewValue);
                             }                            
                         }
                         partial void OnModifierKeysChanged(uint oldKeys, uint newKeys);

                         public uint VirtualKeys
                         {
                             get { return (uint)GetValue(TriggerKeysProperty); }
                             set { SetValue(TriggerKeysProperty, value); }
                         }
                         public static readonly DependencyProperty TriggerKeysProperty =
                             DependencyProperty.Register("VirtualKeys", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnTriggerKeysChanged));
                         public static void Inner_OnTriggerKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister(target.VirtualModifiers,(uint)e.OldValue);
                                GlobalHotKey.Register(target);
                                target.OnModifierKeysChanged((uint)e.OldValue, (uint)e.NewValue);
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

                             handlers?.Invoke(this, new HotKeyEventArgs(VirtualModifiers,VirtualKeys));

                             OnHotKeyInvoked();
                         }
                         partial void OnHotKeyInvoking();
                         partial void OnHotKeyInvoked();

                         public void Covered()
                         {
                             Text = string.Empty;
                             modifiers.Clear();
                             triggers.Clear();
                             VirtualModifiers = 0x0000;
                             VirtualKeys = 0x0000;

                             OnCovered();
                         }
                         partial void OnCovered();

                         private HashSet<VirtualModifiers> modifiers = [];
                         private HashSet<VirtualKeys> triggers = [];

                         public string Text
                         {
                             get { return (string)GetValue(TextProperty); }
                             protected set { SetValue(TextProperty, value); }
                         }
                         public static readonly DependencyProperty TextProperty =
                             DependencyProperty.Register("Text", typeof(string), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(string.Empty));

                         protected virtual void OnHotKeyReceived(object sender, System.Windows.Input.KeyEventArgs e)
                         {
                             var key = (e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key);
                             if (GlobalHotKey.WinApiModifiersMapping.TryGetValue(key, out var modifier))
                             {
                                 if (!modifiers.Remove(modifier))
                                 {
                                     modifiers.Add(modifier);
                                 }
                             }
                             else if (GlobalHotKey.WinApiKeysMapping.TryGetValue(key, out var trigger))
                             {
                                 if (!triggers.Remove(trigger))
                                 {
                                     triggers.Add(trigger);
                                 }
                             }

                             e.Handled = true;
                             UpdateValue();
                             OnHotKeyUpdated();
                         }

                         protected virtual void UpdateValue()
                         {
                             VirtualModifiers = modifiers.GetUint();
                             VirtualKeys = triggers.GetUint();
                             Text = string.Join(" + ", [.. modifiers.GetNames(), .. triggers.GetNames()]);
                         }
                         partial void OnHotKeyUpdated();
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
