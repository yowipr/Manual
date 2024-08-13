using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core;
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
using System.Windows.Shapes;

namespace Manual.Objects;

/// <summary>
/// Lógica de interacción para Tag.xaml
/// </summary>
public partial class PromptTagView : UserControl
{
  

    public PromptTagView()
    {
        InitializeComponent();
    }

    string PromptText
    {
        get { return token.Content.ToString(); }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Output.Log( SentenceTagger.GetTagType(PromptText).ToString() );

    }
}




