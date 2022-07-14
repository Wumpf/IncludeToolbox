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
    [ProvideUIContextRule(PackageGuids.GOnlyVCString, "UIOnlyVC",
    expression: "one & two",
    termNames: new[] { "one", "two" },
    termValues: new[] { @"ActiveProjectCapability:VisualC", @"HierSingleSelectionName:.(h|hpp|hxx|cpp|c|cxx)$" }
)]
    public sealed class IncludeToolboxPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            await Output.InitializeAsync();
        }
    }
}
