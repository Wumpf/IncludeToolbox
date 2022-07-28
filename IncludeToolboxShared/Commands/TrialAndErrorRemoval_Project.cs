using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Project = EnvDTE.Project;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TrialAndErrorRemoval_Project : BaseCommand<TrialAndErrorRemoval_Project>
    {
        private TrialAndErrorRemoval impl;
        private ProjectItems projectItems = null;
        private int numTotalRemovedIncludes = 0;
        private Queue<ProjectItem> projectFiles = new();

        protected override Task InitializeCompletedAsync()
        {
            impl = new TrialAndErrorRemoval();
            impl.OnFileFinished += OnDocumentIncludeRemovalFinished;
            return Task.CompletedTask;
        }


        private void OnDocumentIncludeRemovalFinished(int removedIncludes, bool canceled)
        {
            _ = Task.Run(async () =>
            {
                numTotalRemovedIncludes += removedIncludes;
                if (canceled || !await ProcessNextFile())
                {
                    _ = VS.MessageBox.ShowConfirmAsync(string.Format("Removed total of {0} #include directives from project.", numTotalRemovedIncludes));
                    numTotalRemovedIncludes = 0;
                }
            });
        }
        protected override void BeforeQueryStatus(EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var project = GetSelectedCppProject(out _);
            Command.Visible = project != null;
        }

        static Project GetSelectedCppProject(out string reasonForFailure)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            reasonForFailure = "";

            var selectedItems = VSUtils.GetDTE().SelectedItems;
            if (selectedItems.Count < 1)
            {
                reasonForFailure = "Selection is empty!";
                return null;
            }

            // Reading .Item(object) behaves weird, but iterating works.
            foreach (SelectedItem item in selectedItems)
            {
                Project vcProject = item?.Project;
                if (VSUtils.VCUtils.IsVCProject(vcProject))
                {
                    return vcProject;
                }
            }

            reasonForFailure = "Selection does not contain a C++ project!";
            return null;
        }

        private async Task<bool> ProcessNextFile()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            while (projectFiles.Count > 0)
            {
                ProjectItem projectItem = projectFiles.Dequeue();

                Document document = null;
                try
                {
                    document = projectItem.Open().Document;
                }
                catch (Exception)
                {
                }
                if (document == null)
                    continue;

                bool started = await impl.PerformTrialAndErrorIncludeRemovalAsync(document);
                if (started)
                    return true;
            }
            return false;
        }

        private static void RecursiveFindFilesInProject(ProjectItems items, ref Queue<ProjectItem> projectFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var e = items.GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current;
                if (item == null)
                    continue;
                var projectItem = item as ProjectItem;
                if (projectItem == null)
                    continue;

                Guid projectItemKind = new Guid(projectItem.Kind);
                if (projectItemKind == VSConstants.GUID_ItemType_VirtualFolder ||
                    projectItemKind == VSConstants.GUID_ItemType_PhysicalFolder)
                {
                    RecursiveFindFilesInProject(projectItem.ProjectItems, ref projectFiles);
                }
                else if (projectItemKind == VSConstants.GUID_ItemType_PhysicalFile)
                {
                    projectFiles.Enqueue(projectItem);
                }
                else
                {
                    _=Output.WriteLineAsync(string.Format("Unexpected Error: Unknown projectItem {0} of Kind {1}", projectItem.Name, projectItem.Kind));
                }
            }
        }

        private async Task PerformTrialAndErrorRemoval(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            projectItems = project.ProjectItems;

            projectFiles.Clear();
            RecursiveFindFilesInProject(projectItems, ref projectFiles);

            if (projectFiles.Count > 2)
            {
                if (!await VS.MessageBox.ShowConfirmAsync("Attention! Trial and error include removal on large projects make take up to several hours! In this time you will not be able to use Visual Studio. Are you sure you want to continue?"))
                {
                    return;
                }
            }

            numTotalRemovedIncludes = 0;
            await ProcessNextFile();
        }


        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (TrialAndErrorRemoval.WorkInProgress)
            {
                await VS.MessageBox.ShowErrorAsync("Trial and error include removal already in progress!");
                return;
            }

            try
            {
                Project project = GetSelectedCppProject(out string reasonForFailure);
                if (project == null)
                {
                    _=Output.WriteLineAsync(reasonForFailure);
                    return;
                }

                await PerformTrialAndErrorRemoval(project);
            }
            finally
            {
                projectItems = null;
            }
        }
    }
}
