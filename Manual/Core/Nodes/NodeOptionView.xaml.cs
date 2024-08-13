using Manual.API;
using Manual.Core.Nodes.ComfyUI;
using Manual.Editors;
using Manual.Editors.Displays;
using Manual.MUI;
using Manual.Objects;
using ManualToolkit.Generic;
using SkiaSharp;
using Svg.FilterEffects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
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
using System.Xml;

namespace Manual.Core.Nodes;

/// <summary>
/// Lógica de interacción para NodeOptionView.xaml
/// </summary>
public partial class NodeOptionView : UserControl
{
    public static readonly DependencyProperty TypeColorProperty = DependencyProperty.Register(
       nameof(TypeColor),
       typeof(SolidColorBrush),
       typeof(NodeOptionView),
       new PropertyMetadata( new SolidColorBrush(Colors.White)) );

    public SolidColorBrush TypeColor
    {
        get => (SolidColorBrush)GetValue(TypeColorProperty);
        set => SetValue(TypeColorProperty, value);
    }
    void SetTypeColor(string type)
    {
        TypeColor = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type)];
    }



    public NodeOptionView()
    {
        InitializeComponent();

        this.MouseMove += UserControl_MouseMove;
        this.MouseLeftButtonUp += UserControl_MouseLeftButtonUp;

        this.Loaded += NodeOptionView_Loaded;


    }

    private void NodeOptionView_Loaded(object sender, RoutedEventArgs e) //------------------- INSTANTIATE MANUAL ELEMENT
    {
       if(DataContext is NodeOption nodeop)
        {
            if (nodeop.Direction == NodeOptionDirection.InputField || nodeop.Direction == NodeOptionDirection.Field)
            {
                var manualElement = nodeop.FieldElement?.Invoke();
                if(manualElement is null)
                {
                    Output.Log($"something went wrong loading the PromptPreset {nodeop.Name}", "NodeOptionView");
                    fieldPresenter.Content = new TextBlock().Text = "error";
                    return;
                }

             
                if (nodeop.Direction == NodeOptionDirection.Field || nodeop.Direction == NodeOptionDirection.InputField)
                    fieldPresenter.Content = manualElement;

                var binding = new Binding(nameof(NodeOption.FieldValue))
                {
                    Source = nodeop,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                manualElement.InitializeBind(binding);

            }

            //------- SET
            SetTypeColor(nodeop.Type);

            nodeop.UINode = this;
            nodeop.InvokeFieldValueChanged();

        }


    }


    private Point startPoint;
    private bool isDragging;

    LineConnection? DragLine;
     
    //---------------------------- CONNECT OUTPUT
    private void DragPointOutput_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = GetCanvas.elementPositionCenter(DragPointO);

        var preset = GenerationManager.Instance.SelectedPreset;
        var nodeop = DataContext as NodeOption;
        DragLine = new LineConnection(startPoint, nodeop.Type);
        preset.LineConnections.Add(DragLine);

        isDragging = true;
        Cursor = null;
        e.Handled = true;
        DragPointO.CaptureMouse();

        GenerationManager.SetReachableConnection(nodeop);
    }

    //---------------------------- DISCONECCTING INPUT
    private void DragPointInput_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = sender as FrameworkElement;
        pointIField = p;

        var preset = GenerationManager.Instance.SelectedPreset;
        var nodeOption2 = DataContext as NodeOption;


        if (nodeOption2.LineConnect.Count == 0) // CONNECT FROM INPUT
        {
        
            fromInput = true;
            startPoint = GetCanvas.elementPositionCenter(DragPointI);
           
            DragLine = new LineConnection(startPoint, nodeOption2.Type);
            DragLine.EndPoint = startPoint;
            preset.LineConnections.Add(DragLine);

            isDragging = true;
            Cursor = null;
            e.Handled = true;
            DragPointI.CaptureMouse();

            GenerationManager.SetReachableConnection(nodeOption2);
        }
        else // DISCONNECTING
        {
            _deltaOutput = nodeOption2.LineConnect[0].Output;

            fromInput = false;
            startPoint = GetCanvas.elementPositionCenter(DragPointI).Subtract(nodeOption2.LineConnect[0].EndPoint);

            DragLine = nodeOption2.LineConnect[0];

            isDragging = true;
            pointIField.CaptureMouse();

            Cursor = null;
            e.Handled = true;

            GenerationManager.SetReachableConnection(nodeOption2.Connections[0]);



        }


    }
    FrameworkElement? pointIField;



    bool fromInput = false;
    Grid oldOutput;
    //-------------------------------------------------------------------------- MOVING LINE
    private void UserControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
          
            var canvasPos = GetCanvas.mousePosition();

            if (LCanvas == null)
                LCanvas = GetCanvas;


            //--------------------------------------- WHEN MOUSE IS IN A NODEOPTION
            var hitElement = PointHit(LCanvas, e);
            if (hitElement != null && DragLine != null &&
             AppModel.FindAncestor<NodeOptionView>(hitElement) is NodeOptionView nodeOptionView2 && nodeOptionView2 != this && ((NodeOption)DataContext).HasType(((NodeOption)nodeOptionView2.DataContext)))
            {

                if (!fromInput) // connecting from output to input
                {
                    if (nodeOptionView2.DataContext is NodeOption nodeop && nodeop.IsInputOrField())
                    {
                        if (DataContext is NodeOption nodeop0)
                        {
                            if (nodeop0.IsInputOrField()) // disconnecting from input, so nodeop is the output
                            {

                                if (oldOutput == null)
                                    oldOutput = DragLine.Output.UINode.DragPointO;

                                DragLine.UpdatePos(oldOutput, nodeOptionView2.DragPointI, GetCanvas);

                            }
                            else// connecting from output to input
                            {
                                DragLine.Output = nodeop;
                                DragLine.UpdatePos(this.DragPointO, nodeOptionView2.DragPointI, GetCanvas);


                                ChangeTip(DragPointI, true, nodeop0.RegisteredTypes.GetValueOrDefault(nodeop.Type));
                            }
                        }


                        if (Mouse.OverrideCursor != Cursors.Cross) //  cursor is on nodeoption2
                            Mouse.OverrideCursor = Cursors.Cross;

                        return;
                    }

                }
                else // connecting from input to output
                {
                    if (nodeOptionView2.DataContext is NodeOption nodeop && nodeop.Direction == NodeOptionDirection.Output)
                    {
                        var nodeop0 = ((NodeOption)DataContext);

                        DragLine.Input = nodeop;
                        DragLine.UpdatePos(nodeOptionView2.DragPointO, this.DragPointI, GetCanvas);

                        ChangeTip(DragPointO, true, nodeop.RegisteredTypes.GetValueOrDefault(nodeop0.Type));

                        if (Mouse.OverrideCursor != Cursors.Cross) //  cursor is on nodeoption2
                            Mouse.OverrideCursor = Cursors.Cross;

                        return;
                    }
                }

            

            }

            if (!fromInput)
            {
                var p = new Point(canvasPos.X - startPoint.X,
                               canvasPos.Y - startPoint.Y);

                DragLine?.UpdatePositionEnd(p);

                //when cursor leave nodeoption
                if (Mouse.OverrideCursor != null)
                {
                    Mouse.OverrideCursor = null;
                    DragLine.Output = null;

                    ChangeTip(DragPointI, false);
                }
            }
            else //output
            {
                var p = new Point(-canvasPos.X + startPoint.X,
                              -canvasPos.Y + startPoint.Y);

                DragLine?.UpdatePosition(canvasPos, p);

                //when cursor leave nodeoption
                if (Mouse.OverrideCursor != null)
                {
                    DragLine.Input = null;
                    Mouse.OverrideCursor = null;

                    ChangeTip(DragPointO, false);
                }
            }



        }
    }

    void ChangeTip(FrameworkElement point, bool isOpen, RegisterType content = null)
    {
        //tip
        if (point.ToolTip is ToolTip tip) 
        {
            if (content == null || (content != null && content.Description == null))
            {
                tip.IsOpen = false;
            }
            else
            {
                tip.IsOpen = isOpen;
                tip.Content = content?.Description;
            }
        }
    }

    //------------------ CONNECT
    NodeOption _deltaOutput;
    public CanvasAreaControl GetCanvas => AppModel.FindAncestor<CanvasAreaControl>(this);

    CanvasAreaControl LCanvas;
    private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDragging)
            return;
        // Finalizar el arrastre de un output a un input
        isDragging = false;
        DragPointO.ReleaseMouseCapture();
        pointIField?.ReleaseMouseCapture();
        oldOutput = null;

        Mouse.OverrideCursor = null;

        var canvas = GetCanvas;
       var hitElement = PointHit(canvas, e);//canvas.HitTest(e);

        if (hitElement != null && DragLine != null &&
            AppModel.FindAncestor<NodeOptionView>(hitElement) is NodeOptionView nodeOptionView2)
        {
            // Realizar acciones si es NodeOptionView        
            if (DataContext is NodeOption nodeOption1 && nodeOptionView2.DataContext is NodeOption nodeOption2)
            {
              
                if (nodeOption1 != nodeOption2)
                {
                    if (nodeOptionView2 != this)
                    {
                        GenerationManager.Instance.SelectedPreset.LineConnections.Remove(DragLine);
                        if (nodeOption1.ConnectionInput != null) // cuando se desconecta y se conecta en otro: nodeOption1 = nodoI desconectado, nodeOption2 = nuevo nodo a conectar
                        {
                            var no2 = nodeOption1.ConnectionInput;
                            nodeOption1.Disconnect();

                            if (no2.HasType(nodeOption2))
                                no2.Connect(nodeOption2);
                        }
                        else // conexion normal
                        {
                            if(nodeOption1.HasType(nodeOption2))
                                  nodeOption1.Connect(nodeOption2);
                        }
                    }

                    GenerationManager.Instance.InvokeUpdatePromptPreset();
                }
                else // connected to the same
                {
                    GenerationManager.Instance.SelectedPreset.LineConnections.Remove(DragLine);

                    //DragLine.Output = _deltaOutput;
                    //DragLine.Input = nodeOption2;
                    //DragLine.UpdatePosition();

                }
            }

        }
        else // cancel connection
        {
            var nodeOption1 = DataContext as NodeOption;
            if (nodeOption1 == null)
                return;

            if (nodeOption1.ConnectionInput != null && hitElement?.DataContext != nodeOption1)
            {
                nodeOption1.Disconnect();
            }
            else if (DragLine is not null) // open searchbox and connect
            {
                GenerationManager.Instance.SelectedPreset.LineConnections.Remove(DragLine);
                var latentview = AppModel.FindAncestor<LatentNodesView>(canvas);
                ManualAPI.SelectedPreset.newNodeConnect = nodeOption1;
                latentview.OpenSearchMenuBox();
                //this adds a node connected, source: LatentNodeSystem.cs  AddNode(node);
            }
            

        }


        //finalize
        DragLine = null;

        ChangeTip(DragPointI, false);
        ChangeTip(DragPointO, false);

        GenerationManager.SetReachableConnection(null);

        //  e.Handled = true;
    }




    private FrameworkElement _hitTestResultElement;

    public FrameworkElement PointHit(Canvas canvas, MouseEventArgs e)
    {
        Point hitPoint = e.GetPosition(canvas);
        _hitTestResultElement = null; // Resetea el resultado del hit test

        // Define el callback del resultado del hit test
        HitTestResultCallback resultCallback = new HitTestResultCallback(HitTestResultHandler);

        // Realiza el hit test
        VisualTreeHelper.HitTest(canvas, null, resultCallback, new PointHitTestParameters(hitPoint));

        // Retorna el resultado del hit test después de que se haya completado
        return _hitTestResultElement;
    }

    // Esta función maneja los resultados del hit test
    private HitTestResultBehavior HitTestResultHandler(HitTestResult result)
    {
        if (result.VisualHit is Grid grid && grid.Tag as string == "point")
        {
            _hitTestResultElement = grid; // Guarda el resultado del hit test
            return HitTestResultBehavior.Stop; // Detiene el hit test
        }

        return HitTestResultBehavior.Continue; // Continúa con el hit test
    }

    private void DragPoint_MouseEnter(object sender, MouseEventArgs e)
    {
       // if(!isDragging)
          Cursor = Cursors.Cross;
    }

    private void DragPoint_MouseLeave(object sender, MouseEventArgs e)
    {
        Cursor = null;
    }





  
    private void RightClickMenu(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;

        var nodeop = DataContext as NodeOption;
        if (nodeop == null) return;

        if (header == "Convert to Input")
        {
            nodeop.ConvertToInput();
        }
        if (header == "Convert to Field")
        {
            nodeop.ConvertToField();
        }


        if(header == "Edit Driver")
        {
            //  var sourceName = NodeOption.DetermineSourceName(nodeop);
            //  var driver = nodeop.Driver == null ? new Driver(nodeop, nameof(nodeop.FieldValue), ManualAPI.SelectedPreset.Prompt, expression: sourceName, true) : nodeop.Driver;

            var driver = ManualAPI.SelectedPreset.Prompt.DetermineDriver(nodeop, initialize: false);  
            M_Window.NewShow(new W_DriverView(driver)
            {
                onApplying = () =>
                {
                    if (
                    driver.ExpressionCode == "RealPositivePrompt" || 
                    driver.ExpressionCode == "RealNegativePrompt" || 
                    driver.ExpressionCode == "AltPositivePrompt" ||
                    driver.ExpressionCode == "AltNegativePrompt" ||
                    driver.ExpressionCode == "PositivePrompt" || 
                    driver.ExpressionCode == "NegativePrompt" ||

                    driver.ExpressionCode == "Lora0" ||
                    driver.ExpressionCode == "Lora0Strength" ||
                    driver.ExpressionCode == "Lora1" ||
                       driver.ExpressionCode == "Lora1Strength"
                    )
                        driver.UpdateSource(nodeop.FieldValue);
                }

            }, "", M_Window.TabButtonsType.X, false);

        }
        if (header == "Delete Driver")
        {
            Driver.Delete(nodeop);
        }
    }




}

