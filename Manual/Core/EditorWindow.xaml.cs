using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Editors;

namespace Manual.Core;

/// <summary>
/// Lógica de interacción para EditorWindow.xaml
/// </summary>
/// 

public partial class EditorWindow : UserControl
{

    public EditorWindow()
    {
        InitializeComponent();

        Unloaded += EditorWindow_Unloaded;
    }



    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        var d = DataContext as WorkspaceEditor;
        if (d == null) return;

       // d.PropertyChanged += D_PropertyChanged;

        //SetGlow();
    }
    private void EditorWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        var d = DataContext as WorkspaceEditor;
        if (d == null) return;

       // d.PropertyChanged -= D_PropertyChanged;
    }




    //private void D_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if(e.PropertyName == nameof(WorkspaceEditor.IsGlow))
    //    {
    //        //SetGlow();
    //    }
    //}

    //void SetGlow()
    //{
    //    var d = DataContext as WorkspaceEditor;
    //    if (d == null) return;

    //    var wcon = AppModel.FindAncestor<IWorkspaceControl>(this);
    //    if(wcon is WorkspaceControlRow wrow)
    //    {
    //        if (d.IsGlow)
    //        {
    //            SetGlowDirection(d.GlowDirection);

    //            ContentControl content;
    //            if (wrow.space0 != this)
    //                content = wrow.space0;
    //            else
    //                content = wrow.space1;

    //            if(glow.Visual != content)
    //                glow.Visual = content;
    //        }

    //    }
    //}

    //void SetGlowDirection(Dock dir)
    //{
    //    int offset = -700;
    //    switch (dir)
    //    {
    //        case Dock.Left:
    //            glow.Margin = new Thickness(offset, 0, 0, 0);
    //            break;
    //        case Dock.Top:
    //            glow.Margin = new Thickness(0, offset, 0, 0);
    //            break;
    //        case Dock.Right:
    //            glow.Margin = new Thickness(0, 0, offset, 0);
    //            break;
    //        case Dock.Bottom:
    //            glow.Margin = new Thickness(0, 0, 0, offset);
    //            break;
    //        default:
    //            break;
    //    }
      
    //}


    //public event PropertyChangedEventHandler? PropertyChanged;

    //public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    if (PropertyChanged != null)
    //    {
    //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}





    public static readonly DependencyProperty InnerContentProperty =
       DependencyProperty.Register("InnerContent", typeof(object), typeof(EditorWindow));


    public object InnerContent
    {
        get { return (object)GetValue(InnerContentProperty); }
        set { SetValue(InnerContentProperty, value); }
    }



    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;


        if (menuItem.Header.ToString() == "Vertical split")
        {
            Split(EditorsSpace.ColumnWay);
        }
        else if (menuItem.Header.ToString() == "Horizontal split")
        {
            Split(EditorsSpace.RowWay);
        }


        else if (menuItem.Header.ToString() == "Close Editor")
        {

            FrameworkElement? parent = this;
            while (parent != null && parent is not WorkspaceControlRow && parent is not WorkspaceControlColumn)
            {
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }

            FrameworkElement? grandParent = this;
            int count = 0;
            while (grandParent != null && count < 2)
            {
                if (grandParent is WorkspaceControlRow || grandParent is WorkspaceControlColumn || grandParent is WorkspaceControlSingle)
                {
                    if (count == 1)
                    {
                        break;
                    }
                    count++;
                }
                grandParent = VisualTreeHelper.GetParent(grandParent) as FrameworkElement;
            }

            if (parent is null) // is single
            {
                Output.Show("you cannot close a fullscreen editor", "info");
                return;
            }

            Space s = parent.DataContext as Space;
            Space s2 = grandParent.DataContext as Space;


            AppModel.project.editorsSpace.CloseEditor(this.DataContext as WorkspaceEditor, s, s2);

        }
    }

    private void Split(string way)
    {
        FrameworkElement? parent = this;
        while (parent != null && parent is not WorkspaceControlRow && parent is not WorkspaceControlColumn)
        {
            if (parent is WorkspaceControlSingle && parent.DataContext is WorkspaceSingle wks) // single
            {
                AppModel.project.editorsSpace.AddEditor(
                    this.DataContext as WorkspaceEditor,
                    wks,
                    way);

                return;
            }

            parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
        }

        Space s = parent.DataContext as Space;

        AppModel.project.editorsSpace.AddEditor(
            this.DataContext as WorkspaceEditor,
            s,
            way);
    }

    //private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    //{
    //    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
    //    {
    //        FrameworkElement? parent = this;
    //        while (parent != null && parent is not WorkspaceControlRow && parent is not WorkspaceControlColumn && parent is not WorkspaceControlSingle)
    //        {
    //            parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
    //        }
    //        Output.Log(parent.DataContext);
    //        if(AppModel.project.editorsSpace.Current_Workspace is WorkspaceSingle ws)
    //        {
    //            ws.SwitchFullScreenEditor(this.DataContext as WorkspaceEditor);
    //        }
    //    }
    //}



    public void SwitchFullScreen()
    {
        if (AppModel.project.editorsSpace.Current_Workspace is WorkspaceSingle ws)
        {
            ws.SwitchFullScreenEditor(this.DataContext as WorkspaceEditor);
        }
    }


}
