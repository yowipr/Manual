using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using Manual.Core;
using System.Linq.Expressions;
using System.ComponentModel.Composition.Hosting;
using Microsoft.Win32;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.CSharp.RuntimeBinder;
using System.Dynamic;
using Manual.API;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Manual.Core.Nodes;

namespace Manual.Editors;


public partial class ED_CodeEditorView : Editor
{
    //   [ObservableProperty] private string consoleLog = "hola";

    public ED_CodeEditorView()
    {

    }
}



/// <summary>
/// Lógica de interacción para CodeEditorView.xaml
/// </summary>
public partial class CodeEditorView : UserControl
{
    //string dllPath = "/.";
    //string filename = "/.";
    public CodeEditorView()
    {
        InitializeComponent();

        DataContext = ScriptingManager.Instance;

        //   var a = new Microsoft.CSharp.RuntimeBinder.Binder();
        //   if (RuntimeBinderException.ReferenceEquals(1,2)) { }

        textEditor.Loaded += TextEditor_Loaded;
     
        SetTextEditor();

         textEditor.DataContextChanged += TextEditor_DataContextChanged;

         textEditor.SyntaxHighlighting = LoadYSyntaxHighlighting();

        textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
        textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindVisualChild<ScrollViewer>(textEditor);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }
    }

    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T correctlyTyped)
            {
                return correctlyTyped;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var scriptingManager = DataContext as ScriptingManager;
        var viewModel = scriptingManager?.SelectedScript;
        if (viewModel != null)
        {
            viewModel.ScrollPosition = e.VerticalOffset;
        }
    }




    CompletionWindow completionWindow;
    void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == ".")
        {
            // Open code completion after the user has pressed dot:
            completionWindow = new CompletionWindow(textEditor.TextArea);
            completionWindow.Background = textEditor.Background;
            completionWindow.Foreground = textEditor.Foreground;

            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            data.Add(new MyCompletionData("Item1"));
            data.Add(new MyCompletionData("Item2"));
            data.Add(new MyCompletionData("Item3"));
            completionWindow.Show();
            completionWindow.Closed += delegate {
                completionWindow = null;
            };
        }
    }

    void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                // Whenever a non-letter is typed while the completion window is open,
                // insert the currently selected element.
                completionWindow.CompletionList.RequestInsertion(e);
            }
        }
        // Do not set e.Handled=true.
        // We still want to insert the character that was typed.
    }



    IHighlightingDefinition yhighlight;


    private void TextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var script = e.NewValue as Script;
        if (script == null)
            return;
      //  ChangeHighlightExtension(script);

        var scrollViewer = FindVisualChild<ScrollViewer>(textEditor);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(script.ScrollPosition);
        }

    }

    // NOT USING IT, BUT USEFUL
    void ChangeHighlightExtension(Script script)
    {

        //DIFERENT HIGHLIGHTHNG FOR DIFERENT LANGUAGES
        if (script.Extension == ".y")
        {
            if (yhighlight != null)
                textEditor.SyntaxHighlighting = yhighlight;
            else
                textEditor.SyntaxHighlighting = LoadYSyntaxHighlighting();
        }
        else
        {
            IHighlightingDefinition definition = HighlightingManager.Instance.GetDefinitionByExtension(script.Extension);
            textEditor.SyntaxHighlighting = definition;
        }
    }


    void SetTextEditor()
    {
        var lineNumbersMargin = textEditor.TextArea.LeftMargins.FirstOrDefault(m => m is LineNumberMargin) as LineNumberMargin;

        if (lineNumbersMargin != null)
        {
            // Establecer el Padding deseado
            lineNumbersMargin.Margin = new Thickness(left: 10, top: 0, right: 10, bottom: 0);
        }
    }



    private IHighlightingDefinition LoadYSyntaxHighlighting()
    {
        string syntax_res = $"{App.LocalPath}Resources/YSyntax.xshd";
        if (Output.DEBUG_BUILD())
        {
            var purl = Constants.ProyectURL();
            var syntax_build = Path.Combine(purl, "Manual", "Resources", "YSyntax.xshd");
            try
            {
                // Leer el contenido del archivo 'syntax_build'
                string syntaxBuildContent = File.ReadAllText(syntax_build);

                // Escribir el contenido en el archivo 'syntax_res'
                File.WriteAllText(syntax_res, syntaxBuildContent);

                Console.WriteLine("Archivo copiado exitosamente.");
            }
            catch (Exception ex)
            {
                Output.Log($"Error al copiar el archivo: {ex.Message}");
            }
        }

        using (Stream s = File.OpenRead(syntax_res))
        using (XmlTextReader reader = new XmlTextReader(s))
        {
            var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            yhighlight = definition;
            return definition;
        }
    }



    bool darkMode = true;
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      //  if (darkMode)
      //      codeText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222222"));
      //  else
         //   codeText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bfc1c8"));

        darkMode = !darkMode;
    }

    private void ScrollViewer_Drop(object sender, DragEventArgs e)
    {
        // Verificar si los datos arrastrados son archivos
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Obtener la ruta de los archivos arrastrados
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Iterar sobre cada archivo
            foreach (var file in files)
            {
             //   string extension = System.IO.Path.GetExtension(file).ToLower();

                // Verificar si el archivo tiene la extensión .cs o .y
               // if (extension == ".cs" || extension == ".y")
              //  {
                    // Llamar a ScriptingManager.LoadScript con la ruta completa del archivo
                    ScriptingManager.LoadScript(file);
               // }
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------ MENUITEMS CLICK
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = s.Header as string;

        if (header == null)
            return;

        if(header == "Refresh")
        {
            textEditor.SyntaxHighlighting = LoadYSyntaxHighlighting();
        }
        else if (header == "Testing")
        {
            var nlp = ScriptingManager.Instance.SelectedScript as NLPScript;
            string comfy_path = Path.Combine(Settings.instance.AIServer.DirPath, "ComfyUI", "custom_nodes", "Manual");
            nlp.AddNodesToInit(comfy_path, "Node1:Node1,MyNode:My Node,CLIPLoader:CLIP Loader");
        }
        else if (header == "Y Script")
        {
            ScriptingManager.Instance.AddScript(new YScript());
        }
        else if (header == "C# Script")
        {
            ScriptingManager.Instance.AddScript(new CSharpScript());
        }
        else if (header == "Node Template")
        {
            ScriptingManager.Instance.AddScript(CSharpScript.ComfyNodeTemplate());
        }
    }

    private void MenuItem_Click_Templates(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = (MenuItemNode)s.Header;

        ScriptingManager.Instance.AddScriptTemplate(header.Name);

    }





    private void TextEditor_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var editor = sender as TextEditor;
        if (editor != null)
        {
            var caretOffset = editor.CaretOffset;
            switch (e.Text)
            {
                case "\"":
                    InsertCharacter(editor, "\"", caretOffset);
                    break;
                case "{":
                    InsertCharacter(editor, " }", caretOffset);
                    editor.CaretOffset = caretOffset;
                    break;
            }
        }
    }

    private void InsertCharacter(TextEditor editor, string character, int caretOffset)
    {
        textEditor.PreviewTextInput -= TextEditor_PreviewTextInput;

        editor.Document.Insert(caretOffset, character);
        editor.CaretOffset = caretOffset;

        textEditor.PreviewTextInput += TextEditor_PreviewTextInput;
    }

    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var editor = sender as TextEditor;
            if (editor != null)
            {
                var caretOffset = editor.CaretOffset;
                var document = editor.Document;
                var line = document.GetLineByOffset(caretOffset);
                var lineText = document.GetText(line.Offset, line.Length);
                int cursorPositionInLine = caretOffset - line.Offset;

                string textBeforeCaret = lineText.Substring(0, cursorPositionInLine);
                string textAfterCaret = lineText.Substring(cursorPositionInLine);

                int lastOpenBraceIndex = textBeforeCaret.LastIndexOf('{');
                int firstCloseBraceIndex = textAfterCaret.IndexOf('}');

                if (lastOpenBraceIndex != -1 && firstCloseBraceIndex != -1)
                {
                    // Borrar '{'
                    document.Remove(line.Offset + lastOpenBraceIndex, 1);

                    // Ajustar la posición del '}' debido a la eliminación del '{'
                    firstCloseBraceIndex = textAfterCaret.IndexOf('}', firstCloseBraceIndex - 1);

                    // Borrar '}'
                    if (firstCloseBraceIndex != -1)
                    {
                        document.Remove(line.Offset + cursorPositionInLine + firstCloseBraceIndex - 2, 2);
                    }

                    // Ajustar la posición del cursor
                    editor.CaretOffset = line.Offset + lastOpenBraceIndex;

                    // Llamar al método para insertar enter y sangría
                    InsertEnterAndIndent(editor, editor.CaretOffset, line.Offset);
                    e.Handled = true;
                }
            }
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.S) // save
        {
            ScriptingManager.Instance.SaveScript();
        }
    }

    private void InsertEnterAndIndent(TextEditor editor, int caretOffset, int lineOffset)
    {
        textEditor.PreviewTextInput -= TextEditor_PreviewTextInput;

        // Obtener la sangría de la línea actual
        var line = editor.Document.GetLineByOffset(caretOffset);
        string lineText = editor.Document.GetText(line.Offset, line.Length);
        string indentation = new string(lineText.TakeWhile(char.IsWhiteSpace).ToArray());

        // Construir la cadena de texto con la sangría adecuada
    
        string before = "\n" + indentation + "{\n" + indentation + "    \n";
        string newText = before + indentation + "}";
        // Insertar la nueva línea y sangría
        editor.Document.Insert(caretOffset, newText);
        // editor.CaretOffset = caretOffset + indentation.Length + 5; // Ajustar según la longitud de la sangría
        var lastindex = before.Length - 1;
        editor.CaretOffset = caretOffset + lastindex;

        textEditor.PreviewTextInput += TextEditor_PreviewTextInput;
    }


}

/// Implements AvalonEdit ICompletionData interface to provide the entries in the
/// completion drop down.
public class MyCompletionData : ICompletionData
{
    public MyCompletionData(string text)
    {
        this.Text = text;
    }

    public System.Windows.Media.ImageSource Image
    {
        get { return null; }
    }

    public string Text { get; private set; }

    // Use this property if you want to show a fancy UIElement in the list.
    public object Content
    {
        get { return this.Text; }
    }

    public object Description
    {
        get { return "Description for " + this.Text; }
    }

    public double Priority => default;

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, this.Text);
    }
}

public class ColorizeAvalonEdit : DocumentColorizingTransformer
{
protected override void ColorizeLine(DocumentLine line)
{
    int lineStartOffset = line.Offset;
    string text = CurrentContext.Document.GetText(line);
    int start = 0;
    int index;
    while ((index = text.IndexOf("AvalonEdit", start)) >= 0)
    {
        base.ChangeLinePart(
            lineStartOffset + index, // startOffset
            lineStartOffset + index + 10, // endOffset
            (VisualLineElement element) => {
                // This lambda gets called once for every VisualLineElement
                // between the specified offsets.
                Typeface tf = element.TextRunProperties.Typeface;
                // Replace the typeface with a modified version of
                // the same typeface
                element.TextRunProperties.SetTypeface(new Typeface(
                    tf.FontFamily,
                    FontStyles.Italic,
                    FontWeights.Bold,
                    tf.Stretch
                ));
            });
        start = index + 1; // search for next occurrence
    }
}
}