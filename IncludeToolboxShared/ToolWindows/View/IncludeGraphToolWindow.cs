using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public class IncludeGraphToolWindow : BaseToolWindow<IncludeGraphToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Include Graph";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new IncludeGraphControl());
        }

        [Guid("dc2e50f8-d627-4f55-9095-5d783ad9d475")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}