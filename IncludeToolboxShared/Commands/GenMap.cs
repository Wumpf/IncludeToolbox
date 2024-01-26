﻿using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    [Command(PackageIds.GenMap)]
    internal sealed class GenMap : BaseCommand<GenMap>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var settings = await MapperOptions.GetLiveInstanceAsync();
            if (settings.MapFile == "") { VS.MessageBox.ShowErrorAsync("Map output error", "Map output file is empty, go to Tools->Options->Include Minimizer->General and set the output file!").FireAndForget(); return; }
            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            if (doc == null) return;

            var path = doc.FilePath;
            var relative_path = settings.Prefix != "" ? Utils.MakeRelative(settings.Prefix, path) : path;
            relative_path = relative_path.Replace('\\', '/');
            

            var snap = doc.TextBuffer.CurrentSnapshot;
            var text = snap.GetText();

            var sresult = Parser.ParseInclues(Utils.GetIncludeSpanRO(text), settings.Ignoreifdefs)
                .Distinct();

            string file_map = "";
            switch (settings.Preference)
            {
                case MappingPreference.Quotes:
                    file_map = string.Format("\t{{ include: [ \"<{0}>\", private, \"\\\"{0}\\\"\", public ] }},\n", relative_path);
                    break;
                case MappingPreference.AngleBrackets:
                    file_map = string.Format("\t{{ include: [ \"\\\"{0}\\\"\", private, \"<{0}>\", public ] }},\n", relative_path);
                    break;
                default:
                    break;
            }

            foreach (var match in sresult)
                switch (settings.Preference)
                {
                    case MappingPreference.Quotes:
                        file_map += string.Format("\t{{ include: [ \"{0}\", public, \"\\\"{1}\\\"\", public ] }},\n", match.FullFile.Replace('\\', '/'), relative_path);
                        break;
                    default:
                    case MappingPreference.AngleBrackets:
                        file_map += string.Format("\t{{ include: [ \"{0}\", public, \"<{1}>\", public ] }},\n", match.FullFile.Replace('\\', '/'), relative_path);
                        break;
                }
            settings.Map.Map[relative_path] = file_map;

            settings.Map.WriteMapAsync(settings).FireAndForget();
        }
    }
}
