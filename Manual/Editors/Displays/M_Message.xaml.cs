using Manual.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

namespace Manual.Editors.Displays;

/// <summary>
/// Lógica de interacción para M_Message.xaml
/// </summary>
public partial class M_Message : UserControl
{
    public event EventHandler OnOKPressed;
    public event EventHandler OnCancelPressed;
    public event EventHandler OnNoPressed;

    public M_Message()
    {
        InitializeComponent();
    }

    public M_Message(string message, MessageBoxButton buttons)
    {
        InitializeComponent();

        messageText.Text = message;

        if (buttons == MessageBoxButton.OK)
        {
            btnCancel.Visibility = Visibility.Collapsed;
            btnOK.Visibility = Visibility.Visible;
            btnNo.Visibility = Visibility.Collapsed;
        }
        else if (buttons == MessageBoxButton.OKCancel)
        {
            btnCancel.Visibility = Visibility.Visible;
            btnOK.Visibility = Visibility.Visible;
            btnNo.Visibility = Visibility.Collapsed;
        }
        else if (buttons == MessageBoxButton.YesNoCancel)
        {
            btnCancel.Visibility = Visibility.Visible;
            btnOK.Visibility = Visibility.Visible;
            btnNo.Visibility = Visibility.Visible;
        }
    }
    public M_Message(string message, IEnumerable<Button> buttons)
    {
        InitializeComponent();

        messageText.Text = message;

        stackButtons.Children.Clear();

        foreach (var button in buttons)
        {
            stackButtons.Children.Insert(0, button);
        }

    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        OnOKPressed?.Invoke(this, EventArgs.Empty);
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        OnCancelPressed?.Invoke(this, EventArgs.Empty);
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        OnNoPressed?.Invoke(this, EventArgs.Empty);
    }
}

public partial class M_MessageBox : M_Window
{
    public Action OKPressed;
    public Action CancelPressed;
    public Action NoPressed;
    public M_MessageBox(string message)
    {
        InitializeComponent();
        SetMessageBox(message, "", MessageBoxButton.OK);
    }
    public M_MessageBox(string message, string title)
    {
        InitializeComponent();
        SetMessageBox(message, title, MessageBoxButton.OK);
    }
    public M_MessageBox(string message, string title, MessageBoxButton buttons)
    {
        InitializeComponent();
        SetMessageBox(message, title, buttons);
    }
    public M_MessageBox(string message, string title, MessageBoxButton buttons, Action okPressed)
    {
        InitializeComponent();
        SetMessageBox(message, title, buttons);
        OKPressed = okPressed;
    }
    public M_MessageBox(string message, string title, MessageBoxButton buttons, Action okPressed, Action cancelPressed)
    {
        InitializeComponent();
        SetMessageBox(message, title, buttons);
        OKPressed = okPressed;
        CancelPressed = cancelPressed;
    }
    public M_MessageBox(string message, string title, MessageBoxButton buttons, Action okPressed, Action noPressed, Action cancelPressed)
    {
        InitializeComponent();
        SetMessageBox(message, title, buttons);
        OKPressed = okPressed;
        CancelPressed = cancelPressed;
        NoPressed = noPressed;
    }

    public string messageBox { get; private set; }
    public string titleBox { get; private set; }
    void SetMessageBox(string message, string title, MessageBoxButton buttons) // master ctor
    {
       messageBox = message;
       titleBox = title;

      // ResizeMode = ResizeMode.NoResize;
        Owner = AppModel.mainW;
        Width = 410;
        Height = 160;


        titleWindow.Text = "";//title;
        TabButtons = TabButtonsType.X;

        var mbox = new M_Message(message, buttons);
        mbox.titleText.Text = title;
        mbox.OnOKPressed += Mbox_OnOKPressed;
        mbox.OnCancelPressed += Mbox_OnCancelPressed;
        mbox.OnNoPressed += Mbox_OnNoPressed;

        contentWindow.Content = mbox;
    }



    private void Mbox_OnOKPressed(object? sender, EventArgs e)
    {
        OKPressed?.Invoke();
        Close();
    }
    private void Mbox_OnCancelPressed(object? sender, EventArgs e)
    {
        CancelPressed?.Invoke();
        Close();
    }
    private void Mbox_OnNoPressed(object? sender, EventArgs e)
    {
        NoPressed?.Invoke();
        Close();
    }

    public static void Show(object message)
    {
        M_MessageBox mbox = new(message.ToString());
        ShowD(mbox);
    }
    public static void Show(string message)
    {
        M_MessageBox mbox = new(message);
        ShowD(mbox);
    }
    public static void Show(string message, string title)
    {
        M_MessageBox mbox = new(message, title);
        ShowD(mbox);
    }
    public static void Show(string message, string title, MessageBoxButton buttons)
    {
        M_MessageBox mbox = new(message, title, buttons);
        ShowD(mbox);
    }
    public static M_MessageBox Show(string message, string title, MessageBoxButton buttons, Action okPressed)
    {
        M_MessageBox mbox = new(message, title, buttons, okPressed);
        ShowD(mbox);
        return mbox;
    }
    public static M_MessageBox Show(string message, string title, MessageBoxButton buttons, Action okPressed, string okPressedMsg)
    {
        M_MessageBox mbox = new(message, title, buttons, okPressed);
        mbox.SetBtnNames(okPressedMsg, "");
        ShowD(mbox);
        return mbox;
    }
    public static M_MessageBox Show(string message, string title, MessageBoxButton buttons, Action okPressed, Action cancelPressed)
    {
        M_MessageBox mbox = new(message, title, buttons, okPressed, cancelPressed);
        ShowD(mbox);
        return mbox;
    }
    public static M_MessageBox Show(string message, string title, MessageBoxButton buttons, Action okPressed, Action noPressed, Action cancelPressed)
    {
        M_MessageBox mbox = new(message, title, buttons, okPressed, noPressed, cancelPressed);
        ShowD(mbox);
        return mbox;
    }

    public static void ShowD(M_MessageBox mbox) // main show
    {
        //Output.AlertSound();
        //System.Media.SystemSounds.Asterisk.Play();
        Output.Log(mbox.messageBox, mbox.titleBox);

        Application.Current.Dispatcher.Invoke(() =>
        {
            mbox.ShowDialog();
        });
    }

    internal void SetBtnNames(string ok, string cancel)
    {
        var m = contentWindow.Content as M_Message;
        m.btnOK.Content = ok;
        m.btnCancel.Content = cancel;
    }
    internal void SetBtnNames(string ok, string no, string cancel)
    {
        var m = contentWindow.Content as M_Message;
        m.btnOK.Content = ok;
        m.btnNo.Content = no;
        m.btnCancel.Content = cancel;
    }
}


