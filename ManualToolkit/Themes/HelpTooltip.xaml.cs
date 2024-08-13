using System.Windows;
using System.Windows.Controls;

namespace ManualToolkit.Themes
{
    public partial class HelpTooltip : UserControl
    {
        public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register("ToolTipText", typeof(string), typeof(HelpTooltip), new PropertyMetadata(default(string)));

        public string ToolTipText
        {
            get { return (string)GetValue(ToolTipTextProperty); }
            set { SetValue(ToolTipTextProperty, value); }
        }

        public HelpTooltip()
        {
            InitializeComponent();
        }
    }
}
