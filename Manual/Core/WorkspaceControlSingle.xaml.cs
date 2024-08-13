using Manual.Editors;
using ManualToolkit.Themes;
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
using System.Windows.Threading;

namespace Manual.Core;

/// <summary>
/// Lógica de interacción para WorkspaceControlSingle.xaml
/// </summary>
public partial class WorkspaceControlSingle : UserControl, IWorkspaceControl
{
    public WorkspaceDirection Direction => WorkspaceDirection.Single;
    public WorkspaceControlSingle()
    {
        InitializeComponent();
    }


}
