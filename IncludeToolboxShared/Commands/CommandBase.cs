
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    internal abstract class CommandBase<T> where T : CommandBase<T>, new()
    {
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");
            ThreadHelper.ThrowIfNotOnUIThread();

            Instance = new T();
            Instance.Package = package;
            Instance.SetupMenuCommand();
        }

        protected virtual void SetupMenuCommand()
        {
            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(commandService == null)
            {
                Output.Instance.WriteLine("Failed to retrieve MenuCommandService. No commands could be registered!");
                return;
            }

            EventHandler callback = async (sender, e) =>
            {
                try
                {
                    await this.MenuItemCallback(sender, e);
                }
                catch (Exception exception)
                {
                    await Output.Instance.ErrorMsg("Unexpected Error: {0}", exception.ToString());
                }
            };

            menuCommand = new OleMenuCommand(callback, CommandID);
            commandService.AddCommand(menuCommand);
        }


        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static T Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        protected Package Package { get; private set; }

        protected IServiceProvider ServiceProvider => Package;

        protected OleMenuCommand menuCommand;

        public abstract CommandID CommandID { get; }

        protected abstract Task MenuItemCallback(object sender, EventArgs e);
    }
}
