//using Community.VisualStudio.Toolkit;
//using Microsoft.VisualStudio.Shell;
//using Task = System.Threading.Tasks.Task;

//namespace IncludeToolbox.Commands
//{
//    internal sealed class TrialAndErrorRemoval_CodeWindow : BaseCommand<RunIWYU>
//    {
//        TrialAndErrorRemoval impl = new();

//        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
//        {
//            var document = VCUtil.GetDTE().ActiveDocument;
//            if (document != null)
//                await impl.PerformTrialAndErrorIncludeRemovalAsync(document);
//        }
//    }
//}