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

using System.IO;

using Manual.Objects;
using Manual.API;
using static Manual.API.ManualAPI;
using System.Collections.ObjectModel;
using Manual.MUI;
using System.Windows.Controls.Primitives;
using ManualToolkit.Generic;

namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para LayerView.xaml
/// </summary>
public partial class LayerView : UserControl
{
    private readonly Project _model;
    public LayerView()
    {

        InitializeComponent();
        list.Items.Clear(); // remove designer objects


        _model = AppModel.project;
        DataContext = _model;


        // Agregar manejador para capturar clics en todo el Slider
        this.OpacitySlider.AddHandler(
            Slider.PreviewMouseDownEvent,
            new MouseButtonEventHandler(slider_MouseLeftButtonDown),
            true 
        );


        this.OpacitySlider.AddHandler(
        Slider.PreviewMouseMoveEvent,
        new MouseEventHandler(slider_MouseLeftButtonMove),
        true // Este true indica que queremos escuchar el evento incluso si ha sido marcado como manejado.
    );


        this.OpacitySlider.AddHandler(
         Slider.PreviewMouseUpEvent,
         new MouseButtonEventHandler(slider_MouseLeftButtonUp),
         true 
     );

       

    }


    private void list_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(object)) is object droppedLayer)
        {
            draggedItem.Visibility = Visibility.Collapsed;
            content.Content = null;
            Shortcuts.DragDown();
        }
    }

    private void list_DropEnter(object sender, DragEventArgs e)
    {
        draggedItem.Visibility = Visibility.Visible;
        if (e.Data.GetData(typeof(object)) is object droppedLayer)
        {
            Shortcuts.DragDropAdorner_Close();
            content.Content = droppedLayer;
        }
    }

    private void list_DragLeave(object sender, DragEventArgs e)
    {
        draggedItem.Visibility = Visibility.Collapsed;
        var dropl = content.Content;
        content.Content = null;
        Shortcuts.DragDown();

        Shortcuts.DragDropAdorner_Show(dropl, typeof(LayerView_Layer), e);
    }

    private void list_DragOver(object sender, DragEventArgs e)
    {

        Point p = e.GetPosition(scrollviewer);
        draggedItem.Margin = new Thickness(0, p.Y - 17, 0, 0);

    }


    private void UserControl_MouseEnter(object sender, MouseEventArgs e)
    {
        Focus();
    }


    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            Mouse.OverrideCursor = Cursors.ScrollS;
        }

        else if(e.Key == Key.Delete)
        {
            if (SelectedLayers.Count > 1)
                SelectedLayers.Delete();
            else
                SelectedShot.RemoveLayer();
        }
        
    }
    private void UserControl_KeyUp(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void list_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        //  e.Handled = true;
    }


    private void slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {

        // Suponiendo que 'mySlider' es tu Slider.
        var slider = OpacitySlider;

        if (slider == null) return;

        // Calcular y establecer el nuevo valor del Slider basado en la posición del clic.
        var point = e.GetPosition(slider);
        double newValue = ((point.X - slider.Margin.Left) / slider.ActualWidth) * (slider.Maximum - slider.Minimum) + slider.Minimum;
        slider.Value = newValue;

        // Iniciar el proceso de seguimiento manualmente, esto requiere más trabajo
        // Necesitarías implementar el seguimiento del movimiento del mouse y ajustar el valor del Slider en consecuencia
        // hasta que el botón del mouse se suelte. Esto podría implicar manejar eventos de MouseMove y MouseUp a nivel de la ventana o el control padre.
        slider.CaptureMouse();
        ActionHistory.StartAction(ManualAPI.SelectedLayer, "Opacity");
    }

    private void slider_MouseLeftButtonMove(object sender, MouseEventArgs e)
    {

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var thumb = (Thumb)OpacitySlider.Template.FindName("Thumb", OpacitySlider);
            double thumbWidth = thumb?.ActualWidth ?? 0;

            var slider = OpacitySlider;
            if (slider == null) return;

            Point point = e.GetPosition(slider);
            double thumbOffset = thumbWidth / 2; // Consideramos la mitad del ancho del Thumb para centrar el valor sobre el clic.
            double relativePos = point.X - thumbOffset;
            double relativeWidth = slider.ActualWidth - thumbWidth; // Ajustamos el ancho relativo para compensar el ancho del Thumb.

            // Asegúrate de que la posición relativa no sea negativa y no exceda el ancho ajustado del Slider.
            relativePos = Math.Max(0, Math.Min(relativePos, relativeWidth));

            double newValue = relativePos / relativeWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;


            // Redondear el valor al múltiplo más cercano de TickFrequency.
            double tickFrequency = slider.TickFrequency;
            newValue = Math.Round(newValue / tickFrequency) * tickFrequency;

            slider.Value = newValue;
        }

    }

    private void slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    { 

        if (OpacitySlider.IsMouseCaptured)
        {
            OpacitySlider.ReleaseMouseCapture();
            ActionHistory.FinalizeAction();
            // Aquí terminaría el proceso de "arrastre"
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
       var l = ((FrameworkElement)sender).DataContext as LayerBase;
        l?.ShotParent?.UpdateRender();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var header = AppModel.GetHeader(sender);

        if(header == "Duplicate Layer")
        {
            AnimationManager.DuplicateTrack(SelectedLayer, true);
        }
        else if (header == "Light Duplicate Layer")
        {
            var l = SelectedLayer.CloneFast();
            AddLayerBase(l);
        }
        else if (header == "Delete")
        {
            RemoveLayers(SelectedLayers);
        }
    }




    private void BlendMode_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var comboBox = sender as ComboBox;  // Asumiendo que el sender es un ComboBox

        if (comboBox == null || comboBox.Items.Count == 0)
            return;

        int currentIndex = comboBox.SelectedIndex;

        if (e.Key == Key.Up)
        {
            // Mover hacia arriba en el ComboBox
            if (currentIndex > 0)  // Asegúrate de que no estás en el primer ítem
            {
                comboBox.SelectedIndex = currentIndex - 1;
                Shot.UpdateCurrentRender();
            }
        }
        else if (e.Key == Key.Down)
        {
            // Mover hacia abajo en el ComboBox
            if (currentIndex < comboBox.Items.Count - 1)  // Asegúrate de que no estás en el último ítem
            {
                comboBox.SelectedIndex = currentIndex + 1;
                Shot.UpdateCurrentRender();
            }
        }
        if(e.Key == Key.Enter)
        {
            comboBox.IsDropDownOpen = false;
            Shot.UpdateCurrentRender();
            e.Handled = true;
        }

    }
    private void BlendMode_Selectionchanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;

        var blendMode = e.AddedItems[0] as LayerBlendMode?;

        if (blendMode.HasValue)
        {
            SelectedLayers.ForEach(l => l.BlendMode = blendMode.Value);
            Shot.UpdateCurrentRender();
        }
    }
    private void ComboBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        var blendMode = ((TextBlock)sender).DataContext as LayerBlendMode?;

        if (blendMode.HasValue)
        {
            SelectedLayers.ForEach(l => l.BlendMode = blendMode.Value);
            Shot.UpdateCurrentRender();
        }
    }
}




public partial class ED_LayerView : Editor
{
    public ED_LayerView()
    {

    }
}



