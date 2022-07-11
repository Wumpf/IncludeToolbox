using IncludeToolbox.Formatter;
using IncludeToolbox.Graph;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.GraphWindow
{
    public class IncludeGraphViewModel : PropertyChangedBase
    {
        public HierarchyIncludeTreeViewItem HierarchyIncludeTreeModel { get; set; } = new HierarchyIncludeTreeViewItem(new IncludeGraph.Include(), "");
        public FolderIncludeTreeViewItem_Root FolderGroupedIncludeTreeModel { get; set; } = new FolderIncludeTreeViewItem_Root(null, null);

        public IncludeGraph Graph { get; private set; }


        public enum RefreshMode
        {
            DirectParsing,
            ShowIncludes,
        }

        public static readonly string[] RefreshModeNames = new string[] { "Direct Parsing", "Compile /showIncludes" };

        public RefreshMode ActiveRefreshMode
        {
            get => activeRefreshMode;
            set
            {
                if (activeRefreshMode != value)
                {
                    activeRefreshMode = value;
                    OnNotifyPropertyChanged();
                    QueueUpdatingRefreshStatus();
                }
            }
        }
        RefreshMode activeRefreshMode = RefreshMode.DirectParsing;

        public IEnumerable<RefreshMode> PossibleRefreshModes => Enum.GetValues(typeof(RefreshMode)).Cast<RefreshMode>();

        public bool CanRefresh
        {
            get => canRefresh;
            private set { canRefresh = value; OnNotifyPropertyChanged(); }
        }
        private bool canRefresh = false;

        public string RefreshTooltip
        {
            get => refreshTooltip;
            set { refreshTooltip = value; OnNotifyPropertyChanged(); }
        }
        private string refreshTooltip = "";

        public bool RefreshInProgress
        {
            get => refreshInProgress;
            private set
            {
                refreshInProgress = value;
                QueueUpdatingRefreshStatus();
                OnNotifyPropertyChanged();
                OnNotifyPropertyChanged(nameof(CanSave));
            }
        }
        private bool refreshInProgress = false;

        public string GraphRootFilename
        {
            get => graphRootFilename;
            private set { graphRootFilename = value; OnNotifyPropertyChanged(); }
        }
        private string graphRootFilename = "<No File>";

        public int NumIncludes
        {
            get => (Graph?.GraphItems.Count ?? 1) - 1;
        }

        public bool CanSave
        {
            get => !refreshInProgress && Graph != null && Graph.GraphItems.Count > 0;
        }

        // Need to keep these guys alive.
        private EnvDTE.WindowEvents windowEvents;

        public IncludeGraphViewModel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // UI update on dte events.
            var dte = VSUtils.GetDTE();
            if (dte != null)
            {
                windowEvents = dte.Events.WindowEvents;
                windowEvents.WindowActivated += (x, y) => QueueUpdatingRefreshStatus();
            }

            QueueUpdatingRefreshStatus();
        }

        private void QueueUpdatingRefreshStatus()
        {
            _ = Task.Run(async () =>
            {
                var currentDocument = VSUtils.GetDTE()?.ActiveDocument;

                if (RefreshInProgress)
                {
                    CanRefresh = false;
                    RefreshTooltip = "Refresh in progress";
                }
                // Limiting to C++ document is a bit harsh though for the general case as we might not have this information depending on the project type.
                // This is why we just check for "having a document" here for now.
                else if (currentDocument == null)
                {
                    CanRefresh = false;
                    RefreshTooltip = "No open document";
                }
                else
                {
                    if (activeRefreshMode == RefreshMode.ShowIncludes)
                    {
                        var canPerformShowIncludeCompilation = await CompilationBasedGraphParser.CanPerformShowIncludeCompilation(currentDocument);
                        CanRefresh = canPerformShowIncludeCompilation.Result;
                        RefreshTooltip = canPerformShowIncludeCompilation.Reason;
                    }
                    else
                    {
                        CanRefresh = true;
                        RefreshTooltip = null;
                    }
                }
            });
        }

        public async Task RefreshIncludeGraph()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var currentDocument = VSUtils.GetDTE()?.ActiveDocument;
            GraphRootFilename = currentDocument.Name ?? "<No File>";
            if (currentDocument == null)
                return;

            var newGraph = new IncludeGraph();
            RefreshInProgress = true;

            try
            {
                switch (activeRefreshMode)
                {
                    case RefreshMode.ShowIncludes:
                        if (await newGraph.AddIncludesRecursively_ShowIncludesCompilation(currentDocument, OnNewTreeComputed))
                        {
                            ResetIncludeTreeModel(null);
                        }
                        break;

                    case RefreshMode.DirectParsing:
                        ResetIncludeTreeModel(null);
                        var settings = (ViewerOptionsPage)IncludeToolboxPackage.Instance.GetDialogPage(typeof(ViewerOptionsPage));
                        var includeDirectories = VSUtils.GetProjectIncludeDirectories(currentDocument.ProjectItem.ContainingProject);
                        var uiThreadDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        await Task.Run(
                            async () =>
                            {
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                newGraph.AddIncludesRecursively_ManualParsing(currentDocument.FullName, includeDirectories, settings.NoParsePaths);
                                await OnNewTreeComputed(newGraph, currentDocument, true);
                            });
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            catch(Exception e)
            {
                Output.Instance.WriteLine("Unexpected error when refreshing Include Graph: {0}", e);
                await OnNewTreeComputed(newGraph, currentDocument, false);
            }
        }

        private void ResetIncludeTreeModel(IncludeGraph.GraphItem root)
        {
            HierarchyIncludeTreeModel.Reset(new IncludeGraph.Include() { IncludedFile = root }, "<root>");
            OnNotifyPropertyChanged(nameof(HierarchyIncludeTreeModel));

            FolderGroupedIncludeTreeModel.Reset(Graph?.GraphItems, root);
            OnNotifyPropertyChanged(nameof(FolderGroupedIncludeTreeModel));

            OnNotifyPropertyChanged(nameof(CanSave));
        }

        /// <summary>
        /// Should be called after a tree was computed. Refreshes tree model.
        /// </summary>
        /// <param name="graph">The include tree</param>
        /// <param name="documentTreeComputedFor">This can be different from the active document at the time the refresh button was clicked.</param>
        /// <param name="success">Wheather the tree was created successfully</param>
        private async Task OnNewTreeComputed(IncludeGraph graph, EnvDTE.Document documentTreeComputedFor, bool success)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            RefreshInProgress = false;

            if (success)
            {
                this.Graph = graph;

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(documentTreeComputedFor.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(documentTreeComputedFor.Path) + Path.DirectorySeparatorChar);

                foreach (var item in Graph.GraphItems)
                    item.FormattedName = IncludeFormatter.FormatPath(item.AbsoluteFilename, FormatterOptionsPage.PathMode.Shortest_AvoidUpSteps, includeDirectories);

                ResetIncludeTreeModel(Graph.CreateOrGetItem(documentTreeComputedFor.FullName, out _));
            }

            OnNotifyPropertyChanged(nameof(NumIncludes));
        }
    }
}
