using System;

namespace IncludeToolbox.Commands
{
    static class CommandSetGuids
    {
        /// <summary>
        /// Command menu group (command set GUID) for document menu.
        /// </summary>
        public static readonly Guid MenuGroup = new Guid("aef3a531-8af4-4b7b-800a-e32503dfc6e2");

        /// <summary>
        /// Command menu group (command set GUID) for tool menu.
        /// </summary>
        public static readonly Guid ToolGroup = new Guid("032eb795-1f1c-440d-af98-43cdc1de7a8b");

        /// <summary>
        /// Command menu group for commands in the project menu.
        /// </summary>
        public static readonly Guid ProjectGroup = new Guid("1970ECF3-6C03-4CCF-B422-8DD07F774ED8");

        /// <summary>
        /// Commandset for all toolbar elements in the include graph toolwindow.
        /// </summary>
        public static readonly Guid GraphWindowToolbarCmdSet = new Guid("0B242452-870A-489B-8336-88FD01AEF0C1");
    }
}
