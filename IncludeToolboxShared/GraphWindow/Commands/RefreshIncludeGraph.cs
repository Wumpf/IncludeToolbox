using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox.GraphWindow.Commands
{
    class RefreshIncludeGraph : IncludeToolbox.Commands.CommandBase<RefreshIncludeGraph>
    {
        public override CommandID CommandID { get; } = new CommandID(IncludeToolbox.Commands.CommandSetGuids.GraphWindowToolbarCmdSet, 0x101);

        private IncludeGraphViewModel viewModel;

        public void SetViewModel(IncludeGraphViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateFromViewModel();
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();
            menuCommand.BeforeQueryStatus += (a, b) => UpdateFromViewModel();
        }

        private void UpdateFromViewModel()
        {
            if (viewModel == null)
                return;

            menuCommand.Text = viewModel.RefreshTooltip;
            menuCommand.Enabled = viewModel.CanRefresh;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(IncludeGraphViewModel.RefreshTooltip))
            {
                menuCommand.Text = viewModel.RefreshTooltip;
            }
            else if(e.PropertyName == nameof(IncludeGraphViewModel.CanRefresh))
            {
                menuCommand.Enabled = viewModel.CanRefresh;
            }
        }

        protected override async Task MenuItemCallback(object sender, EventArgs e)
        {
            if (viewModel == null)
                return;

            await viewModel.RefreshIncludeGraph();
        }
    }
}
