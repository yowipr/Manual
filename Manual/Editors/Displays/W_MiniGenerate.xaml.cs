using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.API;
using Manual.Core;
using Manual.MUI;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Manual.Editors.Displays;

/// <summary>
/// Lógica de interacción para W_MiniGenerate.xaml
/// </summary>
public partial class W_MiniGenerate : W_WindowContent
{
    public W_MiniGenerate()
    {
        InitializeComponent();

    }

    private void W_MiniGenerate_Loaded(object sender, RoutedEventArgs e)
    {

        var d = (MiniGenerate)DataContext;
        promptBox.OnEnter = d.Generate;

        promptBox.FocusText();
        promptBox.textBox.SelectAll();
    }

    public override void Close_Click(object sender, RoutedEventArgs e)
    {
        var d = (MiniGenerate)DataContext;
        d.Cancel();
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var d = (MiniGenerate)DataContext;
        d.Generate();
    }

    public override void Window_Closed(object? sender, EventArgs e)
    {
       //base.Window_Closed(sender, e);
    }
}




public partial class MiniGenerate : ObservableObject
{

    public Size Scale = new Size(600, 180);
 
    public W_MiniGenerate view;

    public StackPanel Body { get; set; } = new();

    [ObservableProperty] string headerButton = "Generate";

    [ObservableProperty] string prompt = "";

    [ObservableProperty] string name = "Prompt";


    [ObservableProperty] bool isPromptEnabled = true;
    [ObservableProperty] string promptPlaceholder = "Prompt...";
    [ObservableProperty] int promptCaret = 0;
    public bool randomPlaceholder = true;
    public MiniGenerate()
    {
           
    }

   public List<string> Placeholders = [
        "Describe...",
    ];

    public virtual void InitializeUI()
    {

    }

    public void Open()
    {
        Body.Children.Clear();
        InitializeUI();
        if(IsPromptEnabled)
            Scale = new Size(600, 160);
        else
            Scale = new Size(300, 160);

        if (randomPlaceholder)
        {
            Random random = new Random();
            if (random.NextDouble() > 0.5)
                PromptPlaceholder = Placeholders[random.Next(Placeholders.Count - 1)];
            else
                PromptPlaceholder = Placeholders[0];
        }

        view = new W_MiniGenerate() { DataContext = this };

        var w = new M_Window(view, "", M_Window.TabButtonsType.X);
        w.fadeMain = false;
        w.ShowInTaskbar = false;
        view.window = w;

        view.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
        view.Arrange(new Rect(0, 0, view.DesiredSize.Width, view.DesiredSize.Height));


        w.Width = Scale.Width;
        w.Height = Scale.Height;

        w.Opacity = 0;
        w.Show();
        var op = AnimationLibrary.Opacity(w, 0.98, 0.3);
        op.Begin();
        var m = AnimationLibrary.Translate(w, 0, 0, 0.3, 0, 20);
        m.Begin();

        w.PositionWindowAtMouse();
        OnOpen();
    }
    public void Close()
    {
        OnClose();
 
        //menudo lio esto
        var op = AnimationLibrary.Opacity(view.window, 0, 0.3);
        op.OnFinalized(() =>
        {
            Body.Children.Clear();
            view.window.Close();
        });
        op.Begin();

        var m = AnimationLibrary.Translate(view.window, 0, 20, 0.3);
        m.Begin();


    }

    public void Cancel()
    {
        OnCancel();
        Close();
    }

    public void ui(FrameworkElement element, string enabledBinding = "")
    {
        ManualAPI.add(element, Body);
        if(enabledBinding != "")
            element.SetBinding(FrameworkElement.IsEnabledProperty, new Binding(enabledBinding));
        
    }

    public Grid columns(FrameworkElement[] elements)
    {
        var g = api.columns(elements);
        ui(g);
        return g;
    }

    public virtual void Generate()
    {
        Output.Log("Generate button is not implemented!");
        Close();
    }

    public virtual void OnOpen()
    {

    }

    public virtual void OnClose()
    {

    }
    public virtual void OnCancel()
    {

    }


}
