using Manual.API;
using Manual.Core.Nodes.ComfyUI;
using Manual.Editors;
using Manual.Objects;
using ManualToolkit.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;
using System.Xml.Linq;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
namespace Manual.Core.Nodes;

/// <summary>
/// Lógica de interacción para NodeView.xaml
/// </summary>
public partial class NodeView : UserControl
{
    const int RealMinHeight = 38;
    const int RealMinWidth = 152;

    public NodeView()
    {
        InitializeComponent();

        progressBar.Visibility = Visibility.Visible;

        this.DataContextChanged += OnDataContextChanged;

        this.Loaded += NodeView_Loaded;
        Unloaded += NodeView_Unloaded;


    }
    //--------------------- PROPERTY CHANGED
    private void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeBase.SizeY) || e.PropertyName == nameof(NodeBase.SizeX))
        {
            CalculatePreviewSize(ActualHeight);
        }
        else if (e.PropertyName == nameof(NodeBase.IsCollapsed))
        {
            Content_CollapsedChanged();
        }
    }


    public void Refresh()
    {

    }


    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is NodeBase node)
        {
            Context = node;
        }

    }
    NodeBase Context;
    private void NodeView_Unloaded(object sender, RoutedEventArgs e)
    {
        var node = Context;
        if (node != null)
        {
            node.PropertyChanged -= Node_PropertyChanged;
            node.UINode = null;

            if (node is WrapperNode wn)
            {
                wn.AttachedPreset.UpdateUI();
            }
        }
        if(canvas != null)
         this.canvas.MouseMove -= Canvas_MouseMove;
    }

    bool loadedOnce = true;
    private void NodeView_Loaded(object sender, RoutedEventArgs e)
    {


        // Obtener el Canvas padre
        canvas = AppModel.FindAncestor<CanvasAreaControl>(this);
        if (canvas != null)
        {
            // Aquí puedes suscribirte a los eventos de parentCanvas si lo necesitas

            var node = DataContext as NodeBase;
            if (node != null)
            {

                node.UINode = this;
                node.PropertyChanged += Node_PropertyChanged;

                if(node.IsCollapsed)
                    Content_CollapsedChanged();
            }

            this.canvas.MouseMove += Canvas_MouseMove;


        }

        progressBar.Visibility = Visibility.Collapsed;
        previewImageGrid.Visibility = Visibility.Collapsed;

        if (DataContext is NodeBase n)
        {
            if(!n.IsCollapsed)
                SetBinding(WidthProperty, new Binding("SizeX"));

            if (loadedOnce) //Loaded se suele llamar más de una vez por hacer Move a la colección
                CalculateHeight();
            else if (n.EnablePreview)
                CalculatePreviewSize(n.SizeY);

            if (n is WrapperNode wn)
            {
                wn.UpdateWrapPos();
            }


            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                n.OnUINodeLoaded?.Invoke();
                n.OnUINodeLoaded = null;
            }, DispatcherPriority.Render);
           
        }

        loadedOnce = false;
    }


    //------------------------------------------------------------------------------------------- WHEN NODE LOADED CORRECTLY

    public void CalculateHeight()
    {
        AppModel.InvokeNext(HeightM);    
        progressBar.SetBinding(VisibilityProperty, new Binding("IsWorking") { Converter = new BooleanToVisibilityConverter() });

    }
    
    void HeightM()
    {
      
        var node = DataContext as NodeBase;
        if (node == null)
            return;

    
        if (!node.IsCollapsed)
        {
            MinHeight = ActualHeight;

            // Realiza el segundo cálculo que depende de la UI actualizada aquí
            if (node.IsCustomSize == true)
            {
                if (node.SizeY <= ActualHeight)
                    node.SizeY = (float)MinHeight;

            }
            else // loaded
            {
                MinHeight = ActualHeight;

                if (node.PreviewImage != null)
                    OpenPreview();
                else
                    node.SizeY = (float)ActualHeight;
            }


            if (node.EnablePreview)
                CalculatePreviewSize(node.SizeY);

            SetBinding(HeightProperty, new Binding("SizeY"));
            bindHeight = true;
        }


        node.UpdatePosition();


        //ANIMATION
        if (node.ui_isLoaded == false)
        {
            AnimationLibrary.BounceLoaded(mainGrid).Completed += (s, e) =>
            {
                node.UpdatePosition();
            };
            node.ui_isLoaded = true;
        }
        else
        {
            mainGrid.RenderTransform = null;
        }


    }
    public void OpenPreview()
    {
        ((NodeBase)DataContext).SizeY = (float)ActualHeight + 196; //added extra height to see preview image
    }
    public void ClosePreview()
    {
        ((NodeBase)DataContext).SizeY = (float)MinHeight;
    }


    void CalculatePreviewSize(double height)
    {
        var newHeight = height - MinHeight;

        if (newHeight < 30 && previewImageGrid.Visibility == Visibility.Visible)
            previewImageGrid.Visibility = Visibility.Collapsed;
        else if (newHeight > 30 && previewImageGrid.Visibility == Visibility.Collapsed)
            previewImageGrid.Visibility = Visibility.Visible;

      //  var d = DataContext as NodeBase;
     //   var aspectRatio = (double)d.PreviewImage.PixelWidth / d.PreviewImage.PixelHeight;

        if (newHeight < ActualWidth)
        {

            var x = (newHeight - 10);// * aspectRatio;
            var y = newHeight - 20;
            if (x > 0 && y > 0)
            {
                previewImage.Width = x;
                previewImage.Height = y;
            }
        }
        else
        {
            var x = (ActualWidth - 10);// * aspectRatio;
            var y = ActualWidth - 10;
            if (x > 0 && y > 0)
            {
                previewImage.Width = x;
                previewImage.Height = y;
            }
        }
    }


// Destructor para des-suscribirse de los eventos
CanvasAreaControl canvas;
    ~NodeView()
    {
        if (this.canvas != null)
        {
            this.canvas.MouseMove -= Canvas_MouseMove;
        }
    }


    public bool isDragging = false;
    private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        deltaMouse = canvas.mousePosition();
        var d = DataContext as LatentNode;


        //MARRON: move items Nodes
        //DISABLED: esto se usaba, pero daba un bug con m_combobox_layers, PD: en realidad da error con preview size y todo, marrón espectacular esto enableado
        var index = d.AttachedPreset.LatentNodes.IndexOf(d);
        var lastIndex = d.AttachedPreset.LatentNodes.Count - 1; // Índice de la última posición
        if (index < lastIndex) // Si d ya está al final, no es necesario moverlo.
            d.AttachedPreset.LatentNodes.Move(index, lastIndex);


        var selectedNodes = d.AttachedPreset.SelectedNodes;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) //MULTIPLE SELECT
            selectedNodes.Add(d);

        else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) //DUPLICATE NODE
        {
            var newSelect = Node.DuplicateNodes(selectedNodes, ManualAPI.SelectedPreset);
            selectedNodes.Select(newSelect);
        }
        else if (!selectedNodes.Contains(d)) //ONE SELECT
            selectedNodes.SelectSingleItem(d);


        //save node delta position
        deltaNodes.Clear();
        foreach (var node in selectedNodes)
        {
            //node.UINode.CacheMode = new BitmapCache();
            deltaNodes.Add(node, new Vector(node.PositionGlobalX, node.PositionGlobalY));
        }

        ////CACHE
        //foreach (UIElement item in canvas.Children)
        //{
        //    if(item != this)
        //       item.CacheMode = new BitmapCache();
        //}



        isDragging = true;
    }

    private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        foreach (var kvp in deltaNodes)
        {
            LatentNode node = kvp.Key;
            node.UpdatePosition();
           //node.UINode.CacheMode = null;
        }
        deltaNodes.Clear();
       // canvas.CacheMode = null;

        isDragging = false;

        //e.Handled = true;
        var n = ((NodeBase)DataContext);
        var overlapNode = n.CheckOverlappingLines();
        if (overlapNode != null && n.CanConnectByPass(overlapNode))
        {
            ((NodeBase)DataContext).ConnectByPass(overlapNode);

            if (overlappedNode != null)
                overlappedNode.IsWorking = false;

        }
        else
            ToolTip = null;
    }
    NodeBase overlappedNode;

    Point deltaMouse;

    Dictionary<LatentNode, Vector> deltaNodes { get; set; } = new(); 
    
    
    bool _awaitRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
    
        if (!canvas.dragging)
            isDragging = false;

        if (isDragging && !canvas.isPan) // dragging line to connect
        {
            if (_awaitRender)
                return;


            var mousePos = canvas.mousePosition();

            // move nodes
            if (deltaNodes.Count > 0)
            {
                _awaitRender = true;
                CompositionTarget.Rendering += CompositionTarget_Rendering;

                foreach (var kvp in deltaNodes)
                {
                    LatentNode node = kvp.Key;

                   
                    Vector deltaNode = kvp.Value;
                    Point p = mousePos + deltaNode.Subtract(deltaMouse);
                    node.ChangePosition(p);

                }


                //OVERLAPPING NODE CONNECT BY PASS
                var n = (NodeBase)DataContext;
                var overlapNode = n.CheckOverlappingLines();
                if (overlapNode != null && n.CanConnectByPass(overlapNode))
                {
                    if(overlappedNode != null)
                     overlappedNode.IsWorking = false;

                  //  Output.Log(overlapNode);
                    overlappedNode = overlapNode;
                    overlappedNode.IsWorking = true;

                }
                else
                {
                    if (overlappedNode != null)
                        overlappedNode.IsWorking = false;

                    overlappedNode = null;
                }

            }




        }
    }


    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        string header = s.Header.ToString();

        var node = DataContext as NodeBase;
        if (node == null)
            return;

        if (header == "Apply to Selected Layer")
        {
            if (node.PreviewImage != null)
                ManualAPI.SelectedLayer.Image = node.PreviewImage;
        }
        else if (header == "Copy")
        {
            ManualClipboard.Copy(node.PreviewImage);
        }

        Shot.UpdateCurrentRender();

    }

    private void onDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        var node = DataContext as NodeBase;
        if (node == null)
            return;

        if (node.IsCollapsed)
            return;


        if (node.IsCustomSize == false)
            node.IsCustomSize = true;

        if (node.IsCustomSize == true)
        {
            if (!bindHeight)
            {

                MinHeight = ActualHeight;
                node.SizeY = (float)ActualHeight;
                SetBinding(HeightProperty, new Binding("SizeY"));
                bindHeight = true;
            }
        }



        float xadjust = node.SizeX + (float)e.HorizontalChange;
        float yadjust = node.SizeY + (float)e.VerticalChange;

        //Output.Log($"{xadjust}, {yadjust}");

        if(xadjust > MinWidth)
        node.SizeX = xadjust;
        if(yadjust > MinHeight)
        node.SizeY = yadjust;
    }

    bool bindHeight = false;

    private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
    {
        NodeBase node = DataContext as NodeBase; if (node == null) return;
        node.SwitchCollapse();

        e.Handled = true;

    }
    double oldHeight = 0;
    private void Content_CollapsedChanged()
    {
        var node = DataContext as NodeBase; if (node == null) return;
        if (node.IsCollapsed)
        {
            BindingOperations.ClearBinding(this, FrameworkElement.HeightProperty);
            BindingOperations.ClearBinding(this, FrameworkElement.WidthProperty);

            oldHeight = MinHeight;
            MinHeight = RealMinHeight;
            MinWidth = RealMinWidth;

            Height = RealMinHeight;
            Width = RealMinWidth;
        }
        else
        {
            if (oldHeight != 0)
              MinHeight = oldHeight;

            if (bindHeight == false)
            {
                Height = Double.NaN;
                CalculateHeight();
            }
            else
                SetBinding(HeightProperty, new Binding("SizeY"));

            SetBinding(WidthProperty, new Binding("SizeX"));
          
        }

        Dispatcher.InvokeAsync(node.UpdatePosition, DispatcherPriority.Render);
    }

    private void Button_AddField_Click(object sender, RoutedEventArgs e)
    {
        var d = DataContext as NodeBase;

        var name = Namer.SetName("Output", d.Fields);
        d.AddInput(name, FieldTypes.ANY);

        Height = Double.NaN;
        CalculateHeight();

    }
}
