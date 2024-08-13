﻿using Manual.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
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
using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Manual.API
{
    /// <summary>
    /// Lógica de interacción para ToolSpaceContainer.xaml
    /// </summary>

    public partial class ToolSpaceContainerView : UserControl // DEPRECATED
    {    
        public ToolSpaceContainerView()
        {
            InitializeComponent();
            DataContext = AppModel.project.toolManager;

        }
    }
}
