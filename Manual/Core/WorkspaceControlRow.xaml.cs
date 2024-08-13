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

/// <summary>
/// Lógica de interacción para WorkspaceControl.xaml
/// </summary>
/// 

public partial class WorkspaceControlRow : UserControl, IWorkspaceControl
{
    public WorkspaceDirection Direction => WorkspaceDirection.Row;
    public WorkspaceControlRow()
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
