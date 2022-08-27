using Task = System.Threading.Tasks.Task;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace IncludeToolbox
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.IncludeToolboxString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.FormatterOptionsPage), "Include Toolbox", "Include Format", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.TrialAndErrorRemovalOptionsPage), "Include Toolbox", "Trial and Error", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.MapperOptionsPage), "Include Toolbox", "Mapper", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.IWYUOptionsPage), "Include Toolbox", "Include-What-You-Use", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.ViewerOptionsPage), "Include Toolbox", "Include Viewer", 0, 0, true, SupportsProfiles = true)]
    [ProvideUIContextRule(PackageGuids.GOnlyVCString, "UIOnlyVC",
    expression: "one & two",
    termNames: new[] { "one", "two" },
    termValues: new[] { @"ActiveProjectCapability:VisualC", @"HierSingleSelectionName:.(h|hpp|hxx|cpp|c|cxx)$" }
)]   
    [ProvideUIContextRule(PackageGuids.GHeaderOnlyString, "UIOnlyHead",
    expression: "one & two",
    termNames: new[] { "one", "two" },
    termValues: new[] { @"ActiveProjectCapability:VisualC", @"HierSingleSelectionName:.(h|hpp|hxx|inl)$" }
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
