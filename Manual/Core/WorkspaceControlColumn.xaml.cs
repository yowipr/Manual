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

namespace Manual.Core;

public enum WorkspaceDirection
{
    Column,
    Row,
    Single
}
public interface IWorkspaceControl
{
    WorkspaceDirection Direction { get; }
}
/// <summary>
/// Lógica de interacción para WorkspaceControl.xaml
/// </summary>
/// 
public partial class WorkspaceControlColumn : UserControl, IWorkspaceControl
{
    public WorkspaceDirection Direction => WorkspaceDirection.Column;
    public WorkspaceControlColumn()
    {
        InitializeComponent();
    }

    private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        EditorsSpace.instance.UpdateGlow();
    }

    private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        EditorsSpace.instance.UpdateGlow();
    }
}
