using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.GraphWindow.Commands
{
    /// <summary>
    /// Options for the RefreshMode combo box.
    /// </summary>
    class RefreshModeComboBoxOptions : IncludeToolbox.Commands.CommandBase<RefreshModeComboBoxOptions>
    {
        public override CommandID CommandID { get; } = new CommandID(IncludeToolbox.Commands.CommandSetGuids.GraphWindowToolbarCmdSet, 0x103);

        protected override Task MenuItemCallback(object sender, EventArgs e)
        {
            if (e == EventArgs.Empty)
            {
                // We should never get here; EventArgs are required.
                throw new ArgumentException("Event args are required!");
            }

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null)
                {
                    throw new ArgumentException("In parameter may not be specified");
                }
                else if (vOut != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(IncludeGraphViewModel.RefreshModeNames, vOut);
                }
                else
                {
                    throw new ArgumentException("Out parameter can not be NULL");
                }
            }

            return Task.CompletedTask;
        }
    }
}
