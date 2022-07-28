using Community.VisualStudio.Toolkit;
using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;



namespace IncludeToolbox.Commands
{
    [Command(PackageIds.IWYUProjId)]
    internal sealed class RunIWYUProject : BaseCommand<RunIWYUProject>
    {
        static readonly Regex extension = new("^\\.[ch](?:pp|xx)?$");
        IWYU task = new();
        CancelCallback cancelCallback;

        protected override Task InitializeCompletedAsync()
        {
            cancelCallback = new(() => { task.CancelAsync().FireAndForget(); });
            return Task.CompletedTask;
        }


        async Task CheckAsync()
        {
            Command.Visible = false;
            var items = await VS.Solutions.GetActiveItemsAsync();

            HashSet<Project> set = new();

            bool b = items.All(s =>
            {
                if (s.Type == SolutionItemType.Project)
                { _ = set.Add((Project)s); return true; }
                if (s.Type != SolutionItemType.PhysicalFile) return false;
                var parent = s.FindParent(SolutionItemType.Project);
                if (parent == null) return false;
                _ = set.Add((Project)parent);
                return extension.IsMatch(Path.GetExtension(s.Name));
            });

            if (!b) return;

            foreach (var item in set)
            {
                if (await item.ToVCProjectAsync() == null)
                    return;
            }
            Command.Visible = true;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Has to be synchronous")]
        protected override void BeforeQueryStatus(EventArgs e)
        {
            CheckAsync().Wait();
        }

        Dictionary<string, KeyValuePair<Project, List<string>>> SetTasks(IEnumerable<SolutionItem> items)
        {
            Dictionary<string, KeyValuePair<Project, List<string>>> set = new();
            foreach (var item in items)
            {
                if (item.Type == SolutionItemType.PhysicalFile)
                {
                    var parent = (Project)item.FindParent(SolutionItemType.Project);
                    var hash = parent.FullPath;
                    if (!set.ContainsKey(hash))
                        set[hash] = new KeyValuePair<Project, List<string>>(parent, new List<string>());
                    set[hash].Value.Add(item.Name);
                }
            }
            return set;
        }

        async Task MoveHeaderImplAsync(string file)
        {
            IWYU.MoveHeader(await VS.Documents.OpenAsync(file));
        }

        async Task MoveHeadersAsync(Dictionary<string, KeyValuePair<Project, List<string>>> dict)
        {
            List<Task> tasks = new();
            foreach (var v in dict)
                for (int c = 0; c < v.Value.Value.Count; c++)
                    tasks.Add(MoveHeaderImplAsync(v.Value.Value[c]));

            await Task.WhenAll(tasks);
            await VCUtil.SaveAllDocumentsAsync();
        }


        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var set = SetTasks(await VS.Solutions.GetActiveItemsAsync());
            var settings = await IWYUOptions.GetLiveInstanceAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dlg = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();

            if ((settings.Executable == "" || !File.Exists(settings.Executable) || await settings.DownloadRequiredAsync())
                && !await IWYUDownload.DownloadAsync(dlg, settings))
            {
                VS.MessageBox.ShowErrorAsync("IWYU Error", "No executable found, operation cannot be completed").FireAndForget();
                return;
            }

            if (settings.GetDirty()) task.BuildCommandLine(settings);

            _ = dlg.CreateInstance(out IVsThreadedWaitDialog2 xdialog);
            IVsThreadedWaitDialog4 dialog = xdialog as IVsThreadedWaitDialog4;

            await MoveHeadersAsync(set);

            try
            {
                dialog.StartWaitDialogWithCallback("Include Minimizer", "Running include-what-you-use", null, null, "Running include-what-you-use", true, 0, true, set.Count, 0, cancelCallback);

                // needs parallelization, but cancellation is doomed
                foreach (var v in set)
                {
                    for (int c = 0; c < v.Value.Value.Count;)
                    {
                        string f = v.Value.Value[c];
                        dialog.UpdateProgress("Working with project:",
                                              $"Running IWYU - Working with {v.Value.Key.Name}; File: {f}",
                                              $"Running IWYU - Working with {v.Value.Key.Name}",
                                              ++c,
                                              v.Value.Value.Count,
                                              false,
                                              out var cancelled);


                        bool result = await task.StartAsync(f, v.Value.Key, settings.AlwaysRebuid); // process cannot be rerun

                        if (cancelled || result == false) return;

                        await task.ApplyAsync(settings);
                    }
                }
            }
            catch (Exception ex)
            {
                VS.MessageBox.ShowErrorAsync(ex.Message).FireAndForget();
            }
            _ = dialog.EndWaitDialog();
        }
    }
}
