using Manual.API;
using Manual.Core;
using Manual.Editors;
using Manual.Editors.Displays;
using Manual.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Manual.API.ManualAPI;
namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para LayerView_Layer.xaml
/// </summary>
public partial class LayerView_Layer : UserControl
{


    AnimateUI anim;
    public LayerView_Layer()
    {
        InitializeComponent();

        anim = new AnimateUI(borderOver, focusValue: 0.2, unFocusValue: 0, subscribeTo: mainGrid);
    }


    //------------------------------------------------------- LAYER MOVED
    private void item_Drop(object sender, DragEventArgs e)
    {
        rectangleDrag.Visibility = Visibility.Collapsed;
        if (e.Data.GetData(typeof(object)) is LayerBase dropLayer)
        {
            if (sender is FrameworkElement g)
            {
                var newL = (LayerBase)g.DataContext;    
                Layer.Move(dropLayer, newL);
              //  Shortcuts.DragDown();
               
                SelectOneLayer(dropLayer);
            }
        }
    }


    private void item_DragOver(object sender, DragEventArgs e)
    {

        var data = e.Data.GetData(typeof(object));

       
        if (e.Data.GetData(typeof(object)) is LayerBase dropLayer)
        {
            
            if (sender is FrameworkElement g)
            {
                var newL = (LayerBase)g.DataContext;
                //Layer newL = (Layer)grid.DataContext;
                int oldIndex = ManualAPI.SelectedShot.layers.IndexOf(dropLayer);
                int newIndex = ManualAPI.SelectedShot.layers.IndexOf(newL);

                if(newIndex < oldIndex)
                {
                    rectangleDrag.VerticalAlignment = VerticalAlignment.Top;
                    rectangleDrag.Visibility = Visibility.Visible;

                }
                else if (newIndex == oldIndex)
                {
                    rectangleDrag.Visibility = Visibility.Collapsed;

                }
                else
                {
                    rectangleDrag.VerticalAlignment = VerticalAlignment.Bottom;
                    rectangleDrag.Visibility = Visibility.Visible;
                }
                // ManualAPI.SelectedShot.layers.Move(oldIndex, newIndex);
                //  MessageBox.Show($"old {oldIndex}   new {newIndex}");
            }
        }
    }

    private void item_DragLeave(object sender, DragEventArgs e)
    {
        rectangleDrag.Visibility = Visibility.Collapsed;
        Shortcuts.DragDown();
    }



    //------------------------ MOUSE

    private void LayerMouseMove(object sender, MouseEventArgs e)
    {
        if (Shortcuts.IsMouseDraggable)
        {                 
            DragDrop.DoDragDrop(this, new DataObject(typeof(object), this.DataContext), DragDropEffects.Move);
        }
    }


    private void LayerMouseUp(object sender, MouseButtonEventArgs e)
    {

        var layer = (LayerBase)((FrameworkElement)sender).DataContext;

        if (Shortcuts.IsAltPressed)
        {
            layer.IsAlphaMask = true;
        }



        else if (!Shortcuts.IsMouseDraggable)
        {


            if (Shortcuts.IsShiftPressed)
            {
                if (SelectedLayer == null && SelectedLayer != layer)
                    return;

                var Vlayers = SelectedShot.LayersFromView;

                int start = Vlayers.IndexOf(SelectedLayer);
                int end = Vlayers.IndexOf(layer);

                SelectedLayers.Clear();

                if (start <= end)
                {
                    for (int i = start; i <= end; i++)
                    {
                        SelectedLayers.Add(Vlayers[i]);
                    }
                }
                else
                {
                    for (int i = start; i >= end; i--)
                    {
                        SelectedLayers.Add(Vlayers[i]);
                    }
                }
            }
            else if (Shortcuts.IsCtrlPressed)
            {
                if (SelectedLayer == null && SelectedLayer != layer)
                    return;

                SelectedLayers.Add(layer);
            }
            else // clicked
            {
                SelectOneLayer(layer);
            }



        }
    }


    //------------ ANIMATION
    private void Grid_MouseEnter(object sender, MouseEventArgs e)
    {
        borderOver.Visibility = Visibility.Visible;
  //      AnimateOpacity(0, 0.2);
    }

    private void Grid_MouseLeave(object sender, MouseEventArgs e)
    {
        //   AnimateOpacity(0.2, 0);
    }


    private void JumpToTrack_Click(object sender, MouseButtonEventArgs e)
    {
        var layer = DataContext as LayerBase;
        ED_CanvasView.FocusLayer(layer);
        e.Handled = true;
    }


    private void visibleToggle_MouseEnter(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var l = DataContext as LayerBase;
            if (l != null && l._Animation.IsActuallyVisible)
                l.Visible = !l.Visible;
        }
    }
     
    private void ToggleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        visibleToggle.IsChecked = !visibleToggle.IsChecked;
        e.Handled = true;
    }

    private void ToggleButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {

    }
}

