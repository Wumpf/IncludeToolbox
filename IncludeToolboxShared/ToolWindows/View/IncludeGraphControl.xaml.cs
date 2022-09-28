using Community.VisualStudio.Toolkit;
using IncludeToolbox.Commands;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IncludeToolbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IncludeGraphControl : UserControl
    {
        public IncludeGraphControl()
        {
            InitializeComponent();
            DataContext = new IncludeGraphViewModel();
        }

        private void OnIncludeTreeItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                if (sender is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.DataContext is IncludeTreeViewItem treeItem)   // Arguably a bit hacky to go over the DataContext, but it seems to be a good direct route.
                    {
                        treeItem.NavigateToIncludeAsync().FireAndForget();
                    }
                }
            }
        }
    }
}
