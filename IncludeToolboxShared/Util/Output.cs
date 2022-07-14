using Community.VisualStudio.Toolkit;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    internal static class Output
    {
        static private OutputWindowPane pane;
        static public async Task InitializeAsync()
        {
            pane = await VS.Windows.CreateOutputWindowPaneAsync("Include Minimizer");
        }
        static public async Task WriteLineAsync(string str)
        {
            await pane.WriteLineAsync(str);
        }
        static public void WriteLine(string str)
        {
            pane.WriteLine(str);
        }
        static public async Task BringForwardAsync()
        {
            await pane.ActivateAsync();
        }
        static public async Task ClearAsync()
        {
            await pane.ClearAsync();
        }
    }
}