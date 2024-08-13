using Manual.API;
using Manual.Core;
using Manual.Objects;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para PromptBox.xaml
/// </summary>
public partial class PromptBox : UserControl, IManualElement
{
    public void InitializeBind(Binding bind)
    {
        SetBinding(PromptProperty, bind);
    }

    public IManualElement Clone()
    {
        var clone = new PromptBox();
        return clone;
    }



    public static readonly DependencyProperty IsUpdatePromptProperty =
           DependencyProperty.Register(
               nameof(IsUpdatePrompt),
               typeof(bool),
               typeof(PromptBox),
               new PropertyMetadata(true));

    public bool IsUpdatePrompt
    {
        get { return (bool)GetValue(IsUpdatePromptProperty); }
        set { SetValue(IsUpdatePromptProperty, value); }
    }




    public static readonly DependencyProperty PromptProperty =
        DependencyProperty.Register(
            nameof(Prompt),
            typeof(ObservableCollection<PromptTag>),
            typeof(PromptBox),
            new FrameworkPropertyMetadata(
     null, 
     FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
     OnValueChanged,
     CoerceValue,
     true,
     UpdateSourceTrigger.PropertyChanged));

    public ObservableCollection<PromptTag> Prompt
    {
        get { return (ObservableCollection<PromptTag>)GetValue(PromptProperty); }
        set {
            SetValue(PromptProperty, value);

          //  if (RealPrompt != value)
           //     UpdateRichTextBox();
        }
    }


    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

        PromptBox promptBox = (PromptBox)d;

        if (promptBox.IsUpdatePrompt && e.OldValue != e.NewValue)
        {
            promptBox.UpdateRichTextBox();
        }
        else if (promptBox.IsUpdatePrompt == false && promptBox.updateOnce)
        {
            promptBox.UpdateRichTextBox();
            promptBox.updateOnce = false;
        }
        
    }

    public bool updateOnce = true;

    public PromptBox()
    {
        InitializeComponent();

        promptBox.TextChanged += promptBox_TextChanged;
        promptBox.LostFocus += PromptBox_LostFocus;
        promptBox.Document.IsEnabled = true;
        
        DataObject.AddPastingHandler(promptBox,OnPaste);


    }

    private void PromptBox_LostFocus(object sender, RoutedEventArgs e)
    {
        UpdatePrompt();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // UpdatePrompt();
        //   MessageBox.Show( ManualAPI.SelectedPreset.Prompt.ToString() + "\n" + RealPrompt.ToString() );
    //    var a = SentenceTagger.ConvertToPromptTags(ManualAPI.SelectedPreset.Prompt);

        Output.Log(GetPromptTagsFromRichTextBox()[0].Prompt);

        var b = SentenceTagger.ConvertToString(GetPromptTagsFromRichTextBox());
        Output.Log(b);


     //   MessageBox.Show(a[0].Prompt);
     //   MessageBox.Show(SentenceTagger.ConvertToString(a) + "\n" + ManualAPI.SelectedPreset.Prompt);
    }


    PromptTag TokenMatcher(string prompt)
    {
        if (prompt.EndsWith(", "))
        {
            // Remove the ','
            string p = prompt.Substring(0, prompt.Length - 2).Trim();

             PromptTag ptag = new(p);
          //  GroupTag ptag = new(p);

            return ptag;
        }

        return null;
    }


    private void promptBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = promptBox.CaretPosition.GetTextInRun(LogicalDirection.Backward);
        if (TokenMatcher != null)
        {
            var token = TokenMatcher(text);
            if (token != null)
            {
                ReplaceTextWithToken(text, token);
            }
        }

    }

    private void ReplaceTextWithToken(string inputText, object token)
    {
        // Remove the handler temporarily as we will be modifying tokens below, causing more TextChanged events
        promptBox.TextChanged -= promptBox_TextChanged;

        var para = promptBox.CaretPosition.Paragraph;
        para.LineHeight = 1;
        var matchedRun = para.Inlines.FirstOrDefault(inline =>
        {
            var run = inline as Run;
            return (run != null && run.Text.EndsWith(inputText));
        }) as Run;
        if (matchedRun != null) // Found a Run that matched the inputText
        {
            var tokenContainer = CreateTokenContainer(token, false); 
            para.Inlines.InsertBefore(matchedRun, tokenContainer);

            // Remove only if the Text in the Run is the same as inputText, else split up
            if (matchedRun.Text == inputText)
            {
                para.Inlines.Remove(matchedRun);
            }
            else // Split up
            {
                var index = matchedRun.Text.IndexOf(inputText) + inputText.Length;
                var tailEnd = new Run(matchedRun.Text.Substring(index));
                para.Inlines.InsertAfter(matchedRun, tailEnd);
                para.Inlines.Remove(matchedRun);
            }
        }
  
        promptBox.TextChanged += promptBox_TextChanged;
    }


    private InlineUIContainer CreateTokenContainer(object token, bool atCaretPosition = true)
    {
        var presenter = new ContentPresenter()   {   Content = token,  };
        InlineUIContainer result;
        if(atCaretPosition)
          result  = new InlineUIContainer(presenter, promptBox.CaretPosition) { BaselineAlignment = BaselineAlignment.Center };
        else
            result = new InlineUIContainer(presenter) { BaselineAlignment = BaselineAlignment.Center };
        //  result.IsEnabled = true;

        return result;
    }






    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
        if (!isText) return;

        var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
        PasteText(text);

        e.CancelCommand();
}
    private void PasteText(string clipboardText)
    {     
        foreach (PromptTag promptTag in SentenceTagger.ConvertToPromptTags(clipboardText) )
        {      
            AddPromptTag(promptTag);
        }
    }
    private void AddPromptTag(PromptTag promptTag)
    {
        CreateTokenContainer(promptTag);
    }






    //----------------------- MVVM -----------------------\\
    ObservableCollection<PromptTag> GetPromptTagsFromRichTextBox()
    {
        List<PromptTag> promptTags = new List<PromptTag>();

        foreach (Block block in promptBox.Document.Blocks)
        {
            if (block is Paragraph paragraph)
            {
                foreach (Inline inline in paragraph.Inlines)
                {
                    InlineUIContainer container = inline as InlineUIContainer;
                    if (container == null) // is text
                        continue;

                    ContentPresenter presenter = container.Child as ContentPresenter;
                    PromptTag promptTag = presenter.Content as PromptTag;

                    if (inline != null && promptTag != null)
                    {
                        promptTags.Add(promptTag);
                    }
                }
            }
        }
        promptTags.Reverse();

        return new ObservableCollection<PromptTag>(promptTags);
    }

    void UpdatePrompt()
    {
        Prompt = GetPromptTagsFromRichTextBox();
    }
    void UpdateRichTextBox()
    {
        promptBox.TextChanged -= promptBox_TextChanged;

        promptBox.Document = new FlowDocument();
        var tags = Prompt;
        // tags.Reverse();
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                AddPromptTag(tag);
            }
        }

        promptBox.TextChanged += promptBox_TextChanged;

    }

    private void promptBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.G)
        {
           List<PromptTag> promptTags = new List<PromptTag>();

            List<Inline> inlinesToRemove = new List<Inline>();

      
            foreach (Block block in promptBox.Document.Blocks)
            {
                Paragraph p = block as Paragraph;
                if (p != null)
                {
                   
                     foreach (Inline inline in p.Inlines)
                    {
                      //  if (inline == p.Inlines.First())
                      //      return;

                        InlineUIContainer iuic = inline as InlineUIContainer;
                        if (iuic != null)
                        {

                            if (promptBox.Selection.Contains(iuic.ContentStart))
                            {
                                // Agrega el inline a la lista de inlines a eliminar
                                inlinesToRemove.Add(inline);
                            }
                        }
                    }
                }
            }

            // Elimina los inlines fuera del bucle
            foreach (Inline inlineToRemove in inlinesToRemove)
            {
                if (inlineToRemove == inlinesToRemove.Last())
                    continue;

                InlineUIContainer iuic = inlineToRemove as InlineUIContainer;
                ContentPresenter presenter = iuic.Child as ContentPresenter;
                PromptTag ptag = presenter.Content as PromptTag;

                promptTags.Add(ptag);

                ((Paragraph)inlineToRemove.Parent).Inlines.Remove(inlineToRemove);
            }

            //  promptTags.Remove(promptTags.First());

           // promptTags.Reverse();
            GroupTag groupTag = new GroupTag( new ObservableCollection<PromptTag>(promptTags) );
            AddPromptTag(groupTag);

            e.Handled = true;
        }
    }

}


