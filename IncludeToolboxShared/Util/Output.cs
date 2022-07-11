using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public class Output
    {
        static public Output Instance { private set; get; } = new Output();

        public enum MessageResult
        {
            Yes,
            No
        }

        private const int VsMessageBoxResult_Yes = 6;

        private Output()
        {
        }

        private OutputWindowPane outputWindowPane = null;

        private void Init()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte == null)
                return;

            OutputWindow outputWindow = dte.ToolWindows.OutputWindow;
            outputWindowPane = outputWindow.OutputWindowPanes.Add("IncludeToolbox");
        }

        public void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (outputWindowPane == null)
                Init();
            outputWindowPane.Clear();
        }

        public async Task WriteInternal(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (outputWindowPane == null)
                Init();

            if (outputWindowPane != null)
            {
                System.Diagnostics.Debug.Assert(outputWindowPane != null);
                outputWindowPane.OutputString(text);
            }
        }

        public void Write(string text)
        {
            // Typically we don't care if the message was already written, so let's move this to a different (main-thread) task.
            _ = WriteInternal(text);
        }

        public void Write(string text, params object[] stringParams)
        {
            string output = string.Format(text, stringParams);
            Write(output);
        }

        public void WriteLine(string line)
        {
            Write(line + '\n');
        }

        public void WriteLine(string line, params object[] stringParams)
        {
            string output = string.Format(line, stringParams);
            WriteLine(output);
        }

        public async Task ErrorMsg(string message, params object[] stringParams)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string output = string.Format(message, stringParams);
            WriteLine(output);
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, output, "Include Toolbox", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public async Task InfoMsg(string message, params object[] stringParams)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string output = string.Format(message, stringParams);
            WriteLine(output);
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, output, "Include Toolbox", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public async Task<MessageResult> YesNoMsg(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            int result = VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, message, "Include Toolbox", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return result == VsMessageBoxResult_Yes ? MessageResult.Yes : MessageResult.No;
        }

        public void OutputToForeground()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            outputWindowPane.Activate();
        }
    }
}
