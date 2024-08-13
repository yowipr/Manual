using Manual.API;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Manual.Objects;

/// <summary>
/// Lógica de interacción para EffectView.xaml
/// </summary>
public partial class EffectView : UserControl
{
    public EffectView()
    {
        InitializeComponent();
    }

    private void M_Expander_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {

        Expander expander = (Expander)sender;
        ContextMenu contextMenu = expander.ContextMenu;
        if (contextMenu != null)
        {
            contextMenu.PlacementTarget = expander;
            contextMenu.IsOpen = true;
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var m = sender as MenuItem;
        var header = (string)m.Header;

        var effect = (Effect)DataContext;

        if (header == "Apply")
        {
            effect.Apply();
        }
        else if (header == "Remove")
        {
            effect.Remove();
        }
        else if (header == "Move Up")
        {
            effect.MoveUp();
        }
        else if (header == "Move Down")
        {
            effect.MoveDown();
        }
    }

    private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        Keyboard.Focus(this);
        var effect = (Effect)DataContext;
        if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            effect.Target.SelectedEffects.Add(effect);
        else
            effect.Target.SelectedEffects.SelectSingleItem(effect);

        e.Handled = true;
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Delete)
        {
            var effect = (Effect)DataContext;
            effect.Remove();
        }
    }


}




