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
                         private bool _isregistered = false;
                         /// <summary>
                         /// [ Source Generator ] Indicates whether the hotkey is currently successfully registered
                         /// </summary>
                         public virtual bool IsRegistered
                         {
                             get => _isregistered;
                             protected set
                             {
                                _isregistered = value;

                                if(value)
                                {
                                   OnSuccess();
                                }
                                else
                                {
                                   OnFailed();
                                }
                             }
                         }
                         /// <summary>
                         /// [ Source Generator ] Triggered on successful registration
                         /// </summary>
                         partial void OnFailed();
                         /// <summary>
                         /// [ Source Generator ] Triggered on failed registration
                         /// </summary>
                         partial void OnSuccess();

                         /// <summary>
                         /// [ Source Generator ] one of the builders of hotkey
                         /// <para>Low level : Modifying this item will immediately register or modify the hotkey without updating the UI</para>
                         /// </summary>
                         public uint VirtualModifiers
                         {
                             get { return (uint)GetValue(VirtualModifiersProperty); }
                             set { SetValue(VirtualModifiersProperty, value); }
                         }
                         public static readonly DependencyProperty VirtualModifiersProperty =
                             DependencyProperty.Register("VirtualModifiers", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnModifiersChanged));
                         public static void Inner_OnModifiersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister((uint)e.OldValue,target.VirtualKeys);
                                var id = GlobalHotKey.Register(target);
                                target.IsRegistered = id != 0 && id != -1;
                                target.OnModifiersChanged((uint)e.OldValue, (uint)e.NewValue);
                             }                            
                         }
                         /// <summary>
                         /// [ Source Generator ] Optionally extend the logic where the key has been modified
                         /// </summary>
                         partial void OnModifiersChanged(uint oldKeys, uint newKeys);

                         /// <summary>
                         /// [ Source Generator ] one of the builders of hotkey
                         /// <para>Low level : Modifying this item will immediately register or modify the hotkey without updating the UI</para>
                         /// </summary>
                         public uint VirtualKeys
                         {
                             get { return (uint)GetValue(VirtualKeysProperty); }
                             set { SetValue(VirtualKeysProperty, value); }
                         }
                         public static readonly DependencyProperty VirtualKeysProperty =
                             DependencyProperty.Register("VirtualKeys", typeof(uint), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(default(uint), Inner_OnKeysChanged));
                         public static void Inner_OnKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                         {
                             if(d is {{Syntax.Identifier.Text}} target)
                             {
                                GlobalHotKey.Unregister(target.VirtualModifiers,(uint)e.OldValue);
                                var id = GlobalHotKey.Register(target);
                                target.IsRegistered = id != 0 && id != -1;
                                target.OnKeysChanged((uint)e.OldValue, (uint)e.NewValue);
                             }
                         }
                         /// <summary>
                         /// [ Source Generator ] Optionally extend the logic where the key has been modified
                         /// </summary>
                         partial void OnKeysChanged(uint oldKeys, uint newKeys);

                         private event HotKeyEventHandler? handlers;

                         /// <summary>
                         /// [ Source Generator ] This event is executed when the hotkey is triggered
                         /// </summary>
                         public event HotKeyEventHandler Handler
                         {
                             add { handlers += value; }
                             remove { handlers -= value; }
                         }

                         /// <summary>
                         /// [ Source Generator ] How to call hotkey handling events
                         /// </summary>
                         public virtual void Invoke()
                         {
                             OnHotKeyInvoking();

                             handlers?.Invoke(this, new HotKeyEventArgs(VirtualModifiers,VirtualKeys));

                             OnHotKeyInvoked();
                         }
                         /// <summary>
                         /// [ Source Generator ] Before the hotkey event is triggered
                         /// </summary>
                         partial void OnHotKeyInvoking();
                         /// <summary>
                         /// [ Source Generator ] After the hotkey event is triggered
                         /// </summary>
                         partial void OnHotKeyInvoked();

                         /// <summary>
                         /// [ Source Generator ] If the key combination duplicates this instance when a hotkey is registered elsewhere, this instance is overwritten
                         /// </summary>
                         public virtual void Covered()
                         {
                             OnCovering();

                             Text = string.Empty;
                             modifiers.Clear();
                             key = 0x0000;
                             VirtualModifiers = 0x0000;
                             VirtualKeys = 0x0000;

                             OnCovered();
                         }
                         /// <summary>
                         /// [ Source Generator ] Before the component is covered
                         /// </summary>
                         partial void OnCovering();
                         /// <summary>
                         /// [ Source Generator ] After the component is covered
                         /// </summary>
                         partial void OnCovered();

                         private HashSet<VirtualModifiers> modifiers = [];
                         private VirtualKeys key = 0x0000;

                         /// <summary>
                         /// [ Source Generator ] By default, the plus sign is used to connect key characters, which is usually updated automatically for data binding purposes
                         /// <para>The origin of the character :</para>
                         /// <para>HashSet&lt;VirtualModifiers> modifiers</para>
                         /// <para>VirtualKeys key</para>
                         /// </summary>
                         public string Text
                         {
                             get { return (string)GetValue(TextProperty); }
                             protected set { SetValue(TextProperty, value); }
                         }
                         public static readonly DependencyProperty TextProperty =
                             DependencyProperty.Register("Text", typeof(string), typeof({{Syntax.Identifier.Text}}), new PropertyMetadata(string.Empty));

                         /// <summary>
                         /// [ Source Generator ] Handles WPF user keyboard events for binding. It will update the UI
                         /// </summary>
                         protected virtual void OnHotKeyReceived(object sender, System.Windows.Input.KeyEventArgs e)
                         {
                             var input = (e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key);
                             if (KeyHelper.WinApiModifiersMapping.TryGetValue(input, out var modifier))
                             {
                                 if (!modifiers.Remove(modifier))
                                 {
                                     modifiers.Add(modifier);
                                 }
                             }
                             else if (KeyHelper.WinApiKeysMapping.TryGetValue(input, out var trigger))
                             {
                                 key = trigger;
                             }

                             e.Handled = true;
                             UpdateHotKey();
                         }

                         /// <summary>
                         /// [ Source Generator ] This is executed when the user's keyboard has been processed
                         /// </summary>
                         protected virtual void UpdateHotKey()
                         {
                             OnHotKeyUpdating();
                             VirtualModifiers = modifiers.GetUint();
                             VirtualKeys = (uint)key;
                             Text = string.Join(" + ", [.. modifiers.GetNames(), key.ToString()]);
                             OnHotKeyUpdated();
                         }
                         /// <summary>
                         /// [ Source Generator ] Before the hotkey is updated
                         /// </summary>
                         partial void OnHotKeyUpdating();
                         /// <summary>
                         /// [ Source Generator ] After the hotkey is updated
                         /// </summary>
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
