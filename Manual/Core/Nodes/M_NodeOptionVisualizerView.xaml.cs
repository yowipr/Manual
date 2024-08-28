using Manual.API;
using Manual.Core.Nodes.ComfyUI;
using Manual.MUI;
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

namespace Manual.Core.Nodes;

/// <summary>
/// Lógica de interacción para M_NodeOptionVisualizerView.xaml
/// </summary>
public partial class M_NodeOptionVisualizerView : UserControl, IManualElement
{
    public IManualElement Clone()
    {
        var clone = new M_NodeOptionVisualizerView();
        return clone;
    }

    public void InitializeBind(Binding bind)
    {
        SetBinding(DataContextProperty, bind);
    }


    public M_NodeOptionVisualizerView()
    {
        InitializeComponent();

        DataContextChanged += M_NodeOptionVisualizerView_DataContextChanged;
    }



    public static GridNodeOptionContext WrapInGridContext(string nodeName, string nodeOptionName)
    {
        return new GridNodeOptionContext(nodeName, nodeOptionName, new M_NodeOptionVisualizerView());
    }
    public static GridNodeOptionContextOutput WrapInGridContextOutput()
    {
        var g = new GridNodeOptionContextOutput();
        g.Add(new M_NodeOptionVisualizerView());
        return g;
    }


    public static readonly DependencyProperty TypeColorProperty = DependencyProperty.Register(
   nameof(TypeColor),
   typeof(SolidColorBrush),
   typeof(M_NodeOptionVisualizerView),
   new PropertyMetadata(new SolidColorBrush(Colors.White)));

    public SolidColorBrush TypeColor
    {
        get => (SolidColorBrush)GetValue(TypeColorProperty);
        set => SetValue(TypeColorProperty, value);
    }
    void SetTypeColor(string type)
    {
        TypeColor = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type)];
    }

    private void M_NodeOptionVisualizerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is NodeOption nodeop)
        {
            if (nodeop.Direction == NodeOptionDirection.InputField || nodeop.Direction == NodeOptionDirection.Field) // colocar el manualElement en el view
            {
                var manualElement = nodeop.GetFieldElement();
                if (manualElement is null)
                {
                    if (Output.DEBUGGING_MODE)
                    {
                        Output.Log($"something went wrong loading the PromptPreset {nodeop.Name}", "M_NodeOptionVisualizerView");
                       // fieldPresenter.Content = new TextBlock().Text = "error";
                    }
                    return;
                }
                else
                {
                    fieldPresenter.Content = null;
                }

                if (nodeop.Direction == NodeOptionDirection.InputField)
                    inputFieldPresenter.Content = manualElement;

                if (nodeop.Direction == NodeOptionDirection.Field)
                    fieldPresenter.Content = manualElement;

                var binding = new Binding(nameof(NodeOption.FieldValue))
                {
                    Source = nodeop,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                manualElement.InitializeBind(binding);

            }


            else if (nodeop.Direction == NodeOptionDirection.Output) // nodeop is output ex: Generation
            {       
                var node = nodeop.AttachedNode;
                fieldPresenter.Content = node;
            }

            //set color
            SetTypeColor(nodeop.Type);
        }
        else
        {
            fieldPresenter.Content = null;
        }
    }


}











public partial class GridNodeOptionContext : Grid, IManualElement
{
    public Action<object> OnNotified;

    public IManualElement Clone()
    {
        var clone = new GridNodeOptionContext(NodeBinded, NodeOptionBinded);
        AppModel.CopyBinding(clone, this, DataContextProperty);

        foreach (var children in Children)
        {
            if (children is IManualElement element)
                clone.Add(element.Clone());
        }

        return clone;
    }
    public void InitializeBind(Binding bind)
    {
        SetBinding(DataContextProperty, bind);
    }


    public void Add(IManualElement element)
    {
        Children.Add((FrameworkElement)element);
    }

    public void AddRange(params IManualElement[] elements)
    {
        foreach (IManualElement element in elements)
        {
            Children.Add((FrameworkElement)element);
        }
    }



    public static readonly DependencyProperty NodeBindedProperty = DependencyProperty.Register(
        nameof(NodeBinded),
        typeof(string),
        typeof(GridNodeOptionContext),
        new PropertyMetadata("Output")
    );

    public string NodeBinded
    {
        get { return (string)GetValue(NodeBindedProperty); }
        set { SetValue(NodeBindedProperty, value); }
    }



    public static readonly DependencyProperty NodeOptionBindedProperty = DependencyProperty.Register(
    nameof(NodeOptionBinded),
    typeof(string),
    typeof(GridNodeOptionContext),
    new PropertyMetadata("Result")
);

    public string NodeOptionBinded
    {
        get { return (string)GetValue(NodeOptionBindedProperty); }
        set { SetValue(NodeOptionBindedProperty, value); }
    }




    //----------- NEEDS DATACONTEXT CHANGE AUTOMATICALLY FOR SELECTED PRESET


    public GridNodeOptionContext()
    {
         GenerationManager.OnPromptPresetChanged += OnSelectedPresetChanged;

         Unloaded += M_NodeVisualizerView_Unloaded;

         Loaded += GridNodeOptionContext_Loaded;   
    }

    private void GridNodeOptionContext_Loaded(object sender, RoutedEventArgs e)
    {
          OnSelectedPresetChanged(ManualAPI.SelectedPreset);
    }

    public GridNodeOptionContext(string nodeName, string nodeOptionName)
    {

        NodeBinded = nodeName;
        NodeOptionBinded = nodeOptionName;

        GenerationManager.OnPromptPresetChanged += OnSelectedPresetChanged;

        Unloaded += M_NodeVisualizerView_Unloaded;

        Loaded += GridNodeOptionContext_Loaded;
    }
    public GridNodeOptionContext(string nodeName, string nodeOptionName, IManualElement element)
    {

        Add(element);

        NodeBinded = nodeName;
        NodeOptionBinded = nodeOptionName;

        GenerationManager.OnPromptPresetChanged += OnSelectedPresetChanged;

        Unloaded += M_NodeVisualizerView_Unloaded;

        Loaded += GridNodeOptionContext_Loaded;
    }


    private void M_NodeVisualizerView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ActualPromptPreset != null)
        {
            GenerationManager.OnPromptPresetChanged -= OnSelectedPresetChanged;
        }
        if (ActualPromptPreset != null && ActualPromptPreset.LatentNodes != null)
        {
            ActualPromptPreset.LatentNodes.CollectionChanged -= LatentNodes_CollectionChanged;
        }
        if (ActualNodeOption != null)
        {
            ActualNodeOption.OnConnectionChanged -= OnConnectionInputChanged;
        }
    }



    private void M_NodeVisualizerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is PromptPreset preset)
        {
            OnSelectedPresetChanged(preset);
        }
    }

    PromptPreset? ActualPromptPreset;
    NodeBase? ActualNode;
    NodeOption? ActualNodeOption; // the output, example: Result,    ConnectionInput: principled latent
    void OnSelectedPresetChanged(PromptPreset preset)
    {
        if (ActualPromptPreset is not null)
            ActualPromptPreset.LatentNodes.CollectionChanged -= LatentNodes_CollectionChanged;

        ActualPromptPreset = preset;
        if(preset != null)
         preset.LatentNodes.CollectionChanged += LatentNodes_CollectionChanged;

        OnLatentNodesChanged();
    }

    private void LatentNodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnLatentNodesChanged();
    }


    void OnLatentNodesChanged()
    {
        var preset = ActualPromptPreset;
        if (preset is not null && FindNodeBind(preset) is NodeBase node) //you can change it
        {
            NodeOption nodeOption = FindFieldBind(node);
            if (nodeOption is not null)
            {
                if (ActualNodeOption is not null)
                    ActualNodeOption.OnConnectionChanged -= OnConnectionInputChanged;

                nodeOption.OnConnectionChanged += OnConnectionInputChanged;

                ActualNodeOption = nodeOption;
            }
            if(nodeOption?.Direction == NodeOptionDirection.Output)
                OnConnectionInputChanged(nodeOption?.ConnectionInput); // pasarle la conexión
            else
                OnConnectionInputChanged(nodeOption); // es un field, pasarle el valor

            ActualNode = node;
        }
        else //nodo no encontrado
        {
            OnConnectionInputChanged(null);
        }
    }

    void OnConnectionInputChanged(NodeOption? fieldInput) // --------------------------------------------------- field is ConnectionInput of nodeOption
    {
        if (fieldInput is not null)
        {
            DataContext = fieldInput;

            if (Children.Contains(border))
                Children.Remove(border);
            ToolTip = null;
           // Opacity = 1;
            Visibility = Visibility.Visible;
        }
        else
        {
            DataContext = null;
            if(!Children.Contains(border))
            Children.Add(border);
            ToolTip = $"node or field not find. {NodeBinded} {NodeOptionBinded}";
          //  Opacity = 0.75;
            Visibility = Visibility.Collapsed;
        }
    }
    Border border = new() { BorderThickness= new Thickness(1), BorderBrush = new SolidColorBrush(Colors.Red), Opacity=0.3};


    //----- customizable
    public virtual NodeBase FindNodeBind(PromptPreset preset)
    {
        return preset.FindNode(NodeBinded);
    }
    public virtual NodeOption FindFieldBind(NodeBase node)
    {
        return node.FindField(NodeOptionBinded);
    }

}

public class GridNodeOptionContextOutput : GridNodeOptionContext
{


    public override NodeBase FindNodeBind(PromptPreset preset)
    {
        return preset.GetOutputNode();
    }
    public override NodeOption FindFieldBind(NodeBase node)
    {
        return node.Inputs[0];
    }
}


public class GridNodeOptionContextMultiple : GridNodeOptionContext
{


    private NodeContext _successfulContext = null;

    public ObservableCollection<NodeContext> Contexts { get; set; } = new ObservableCollection<NodeContext>();

    public override NodeBase FindNodeBind(PromptPreset preset)
    {
        foreach (var context in Contexts)
        {
            var node = context.FindNodePredicate?.Invoke(preset) ?? null;
            if (node != null)
            {
                _successfulContext = context; // Guardar el contexto exitoso
                return node;
            }
        }
        return null;
    }
    public override NodeOption FindFieldBind(NodeBase node)
    {
        // Verificar si existe un contexto exitoso y usarlo para buscar el NodeOption
        if (_successfulContext != null)
        {
            return _successfulContext.FindFieldPredicate?.Invoke(node) ?? null;
        }
        return null;
    }
    
    public void AddContext(NodeContext context)
    {
        Contexts.Add(context);
    }
    public void AddContext(params NodeContext[] contexts)
    {
        foreach (var context in contexts)
        {
            AddContext(context);
        }
    }
}

public class NodeContext
{
    internal Func<PromptPreset, NodeBase> FindNodePredicate { get; set; }
    internal Func<NodeBase, NodeOption> FindFieldPredicate { get; set; }
    public NodeContext(Func<PromptPreset, NodeBase> findNodePredicate, Func<NodeBase, NodeOption> findFieldPredicate)
    {
        FindNodePredicate = findNodePredicate;
        FindFieldPredicate = findFieldPredicate;
    }
    public NodeContext()
    {
    }
    public NodeContext(string nodeName, string fieldName)
    {
        FindNodePredicate = (preset) => preset.LatentNodes.FirstOrDefault(node => node.Name == nodeName);
        FindFieldPredicate = (node) => node.Fields.FirstOrDefault(option => option.Name == fieldName);
    }
    public NodeContext(Func<NodeBase, bool> findNodePredicate, Func<NodeOption, bool> findFieldPredicate)
    {
        FindNodePredicate = (preset) => preset.LatentNodes.FirstOrDefault(findNodePredicate);
        FindFieldPredicate = (node) => node.Fields.FirstOrDefault(findFieldPredicate);
    }

    public NodeContext(string nodeName, Func<NodeOption, bool> findFieldPredicate)
    {
        FindNodePredicate = (preset) => preset.LatentNodes.FirstOrDefault(node => node.Name == nodeName);
        FindFieldPredicate = (node) => node.Fields.FirstOrDefault(findFieldPredicate);
    }


    public void FindNodeType(string nodeType)
    {
        FindNodePredicate = (preset) => preset.LatentNodes.FirstOrDefault(node => node.Name == nodeType);
    }
    public void FindNodeName(string nodeName)
    {
        FindNodePredicate = (preset) => preset.LatentNodes.FirstOrDefault(node => node.Name == nodeName);
    }
    public void FindFieldName(string fieldName)
    {
        FindFieldPredicate = (node) => node.Fields.FirstOrDefault(option => option.Name == fieldName);
    }
}
