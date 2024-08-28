using Manual.API;
using Manual.Core;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// Lógica de interacción para LogMessageView.xaml
/// </summary>
public partial class LogMessageView : UserControl
{
    AnimateUI anim;
    public LogMessageView()
    {
        InitializeComponent();


        anim = new AnimateUI(borderOver, focusValue: 0.4, unFocusValue: 0, subscribeTo: this);
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = (string)s.Header;
        if(header == "Copy")
        {
            if(s.DataContext is WriteableBitmap bitmap)
            {
                ManualClipboard.Copy(bitmap);
            }
            else if (s.DataContext is string strin)
            {
                ManualClipboard.Copy(strin);
            }
        }
    }
}



public class LogMessage
{
    public ImageSource Icon { get; set; } = AppModel.GetResource<BitmapImage>("manuallogo2");
    public string? Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Time { get; set; } = "";

    public Color MessageColor { get; set; } = Colors.White;

    public OutputType OutputType = OutputType.Message;
    public MvvmHelpers.ObservableRangeCollection<object> Files { get; set; } = [];
  //  public MvvmHelpers.ObservableRangeCollection<WriteableBitmap> Images { get; set; } = [];


    public LogMessage(string message)
    {
        Message = message;
        Time = DateTime.Now.ToString("HH:mm:ss");
    }
    public LogMessage(string message, string title)
    {
        Message = message;
        Title = title;
        Time = DateTime.Now.ToString("HH:mm:ss");
    }
    public LogMessage(SKBitmap message)
    {
        Time = DateTime.Now.ToString("HH:mm:ss");
        Files.Add(message.ToWriteableBitmap());

    }
}

