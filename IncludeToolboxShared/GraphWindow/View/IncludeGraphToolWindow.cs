using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Linq;
using IncludeToolbox;
using System.ComponentModel.Design;

namespace IncludeToolbox.GraphWindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid(IncludeGraphToolWindow.GUIDString)]
    public sealed class IncludeGraphToolWindow : ToolWindowPane
    {
        public const string GUIDString = "c87b586a-6c8b-4129-9b6d-56a761e0ac6d";

        private readonly IncludeGraphControl graphToolWindowControl;
        private const int ToolbarID = 0x1000;

        //private new IncludeToolboxPackage Package
        //{
        //    get { return (IncludeToolboxPackage)base.Package; }
        //}

        private static bool commandsInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeGraphToolWindow"/> class.
        /// </summary>
        public IncludeGraphToolWindow() : base()
        {
            this.Caption = "Include Graph";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            graphToolWindowControl = new IncludeGraphControl();
            Content = graphToolWindowControl;

            // Todo: Get rid of IncludeToolboxPackage.Instance singleton thing. Can't use base.Package here yet since it is not initialized yet.
            if (!commandsInitialized)
            {
                commandsInitialized = true;
                Commands.RefreshIncludeGraph.Initialize(IncludeToolboxPackage.Instance);
                Commands.RefreshIncludeGraph.Instance.SetViewModel(graphToolWindowControl.ViewModel);

                Commands.RefreshModeComboBox.Initialize(IncludeToolboxPackage.Instance);
                Commands.RefreshModeComboBox.Instance.SetViewModel(graphToolWindowControl.ViewModel);
                Commands.RefreshModeComboBoxOptions.Initialize(IncludeToolboxPackage.Instance);

                Commands.SaveDGML.Initialize(IncludeToolboxPackage.Instance);
                Commands.SaveDGML.Instance.SetViewModel(graphToolWindowControl.ViewModel);
            }

            ToolBar = new CommandID(IncludeToolbox.Commands.CommandSetGuids.GraphWindowToolbarCmdSet, ToolbarID);
        }
    }
}
