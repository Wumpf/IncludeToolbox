﻿//------------------------------------------------------------------------------
// <copyright file="Purge_Includes.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;


namespace IncludeFormatter.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PurgeIncludes
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        private MenuCommand command;

        /// <summary>
        /// Initializes a new instance of the <see cref="PurgeIncludes"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PurgeIncludes(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet.Guid, CommandId);
                command = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(command);

                EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                EnvDTE.Events events = (EnvDTE.Events)dte.Events;
                events.WindowEvents.WindowActivated += OnWindowActivated;
            }
        }

        private void OnWindowActivated(Window gotFocus, Window lostFocus)
        {
            //var compileCommand = gotFocus.DTE.Commands.Item("Build.Compile");
            //command.Enabled = compileCommand.IsAvailable;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PurgeIncludes Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new PurgeIncludes(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Waiting for an answer on http://stackoverflow.com/questions/35830092/visual-studio-extension-compile-file-with-vcfileconfiguration-fails
            /*     EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof (EnvDTE.DTE));
                 if (dte == null)
                     return;

                 var document = dte.ActiveDocument;
                 var project = document.ProjectItem.ContainingProject;
                 VCProject vcProject = project.Object as VCProject;
                 if (vcProject == null)
                 {
                     Output.Error("The given project is not a VC++ Project", "Failed to perform Include Purge");
                     return;
                 }
                 VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
                 var tools = activeConfiguration.Tools;
                 //VCCLCompilerTool compilerTool = tools["VCCLCompilerTool"];


                 VCFile vcFile = document.ProjectItem.Object as VCFile;
                 if (vcFile == null)
                 {
                     Output.Error("The given file is not a VC++ File", "Failed to perform Include Purge");
                     return;
                 }
                 IVCCollection fileConfigCollection = vcFile.FileConfigurations;
                 VCFileConfiguration fileConfig = fileConfigCollection.Item(activeConfiguration.Name);


                 // strng = fileconfig.Evaluate("$(TargetDir)")

                 if (fileConfig == null)
                 {

                     return;
                 }
                 try
                 {
                     fileConfig.Compile(false, true); // WaitOnBuild==true always fails.
                  //   Output.Info("Success.");
                 }
                 catch (Microsoft.VisualStudio.ProjectSystem.ProjectException exception)
                 {
                     Output.Error("Fail.");
                 }

                 //dte.ExecuteCommand("Build.Compile");

         */
        }
    }
}