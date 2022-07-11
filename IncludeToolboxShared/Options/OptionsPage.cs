using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    /// <summary>
    /// Base class for all option pages.
    /// </summary>
    public abstract class OptionsPage : DialogPage
    {
        /// <summary>
        /// Initializes either with a in place created TaskContext for tests or, if our VS package is acutally active with the standard context.
        /// </summary>
        public OptionsPage() : base(IncludeToolboxPackage.Instance == null ?
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            new Microsoft.VisualStudio.Threading.JoinableTaskContext() : ThreadHelper.JoinableTaskContext)
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        { }

        // In theory the whole save/load mechanism should be done automatically.
        // But *something* is or was broken there.
        // see http://stackoverflow.com/questions/32751040/store-array-in-options-using-dialogpage

        static protected WritableSettingsStore GetSettingsStore()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        abstract public override void SaveSettingsToStorage();
        abstract public override void LoadSettingsFromStorage();
    }
}
