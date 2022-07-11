using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.GraphWindow.Commands
{
    class RefreshModeComboBox : IncludeToolbox.Commands.CommandBase<RefreshModeComboBox>
    {
        public override CommandID CommandID { get; } = new CommandID(IncludeToolbox.Commands.CommandSetGuids.GraphWindowToolbarCmdSet, 0x102);

        private IncludeGraphViewModel viewModel;

        public void SetViewModel(IncludeGraphViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        protected override Task MenuItemCallback(object sender, EventArgs e)
        {
            if ((null == e) || (e == EventArgs.Empty))
            {
                // We should never get here; EventArgs are required.
                throw new ArgumentException("Event args are required!");
            }

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object input = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero && input != null)
                {
                    throw new ArgumentException("Both in and out parameters are invalid!");
                }
                if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(IncludeGraphViewModel.RefreshModeNames[(int)viewModel.ActiveRefreshMode], vOut);
                }

                else if (input != null)
                {
                    int newChoice = -1;
                    if (!int.TryParse(input.ToString(), out newChoice))
                    {
                        // user typed a string argument in command window.
                        for (int i = 0; i < IncludeGraphViewModel.RefreshModeNames.Length; i++)
                        {
                            if (string.Compare(IncludeGraphViewModel.RefreshModeNames[i], input.ToString(), StringComparison.CurrentCultureIgnoreCase) == 0)
                            {
                                newChoice = i;
                                break;
                            }
                        }
                    }

                    // new value was selected or typed in
                    if (newChoice != -1)
                    {
                        viewModel.ActiveRefreshMode = (IncludeGraphViewModel.RefreshMode)newChoice;
                    }
                    else
                    {
                        throw new ArgumentException("Unknown combo box index or string");
                    }
                }
                else
                {
                    // We should never get here; EventArgs are required.
                    throw new ArgumentException("Event args are required!");
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw new ArgumentException("Event args are required!");
            }

            return Task.CompletedTask;
        }
    }
}
