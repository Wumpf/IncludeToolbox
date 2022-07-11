using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidIncludeToolboxPackageString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class IncludeToolboxPackage : ToolkitPackage
    {

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
        }
    }

    //[ProvideBindingPath(SubPath = "")]   // Necessary to find packaged assemblies.

    //[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    //[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideMenuResource("Menus.ctmenu", 1)]

    //[ProvideOptionPage(typeof(FormatterOptionsPage), Options.Constants.Category, FormatterOptionsPage.SubCategory, 1000, 1001, true)]
    //[ProvideOptionPage(typeof(IncludeWhatYouUseOptionsPage), Options.Constants.Category, IncludeWhatYouUseOptionsPage.SubCategory, 1000, 1002, true)]
    //[ProvideOptionPage(typeof(TrialAndErrorRemovalOptionsPage), Options.Constants.Category, TrialAndErrorRemovalOptionsPage.SubCategory, 1000, 1003, true)]
    //[ProvideOptionPage(typeof(ViewerOptionsPage), Options.Constants.Category, ViewerOptionsPage.SubCategory, 1000, 1004, true)]

    //[ProvideToolWindow(typeof(GraphWindow.IncludeGraphToolWindow))]
    //[Guid(IncludeToolboxPackage.PackageGuidString)]
    //[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    //[InstalledProductRegistration("#110", "#112", "0.2", IconResourceID = 400)]
    //public sealed class IncludeToolboxPackage : AsyncPackage
    //{
    //    /// <summary>
    //    /// IncludeToolboxPackage GUID string.
    //    /// </summary>
    //    public const string PackageGuidString = "5c2743c4-1b3f-4edd-b6a0-4379f867d47f";

    //    static public Package Instance { get; private set; }

    //    public IncludeToolboxPackage()
    //    {
    //        // Inside this method you can place any initialization code that does not require
    //        // any Visual Studio service because at this point the package object is created but
    //        // not sited yet inside Visual Studio environment. The place to do all the other
    //        // initialization is the Initialize method.
    //        Instance = this;
    //    }

    //    #region Package Members

    //    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    //    {
    //        await base.InitializeAsync(cancellationToken, progress);

    //        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    //        Commands.IncludeGraphToolWindow.Initialize(this);
    //        Commands.FormatIncludes.Initialize(this);
    //        Commands.IncludeWhatYouUse.Initialize(this);
    //        Commands.TrialAndErrorRemoval_CodeWindow.Initialize(this);
    //        Commands.TrialAndErrorRemoval_Project.Initialize(this);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        base.Dispose(disposing);
    //    }

    //    #endregion
    //}
}
