using Community.VisualStudio.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace IncludeToolbox
{
    public interface IGraphModel
    {
        public Task<IncludeGraph.Item> TryEmplaceAsync(string absolute_filename, string name);
    }

    public class IncludeGraph : IGraphModel
    {
        public class Item
        {
            public Item(string absolute, string formatted, Include[] includes)
            {
                AbsoluteFilename = absolute;
                FormattedName = formatted;
                Includes = includes;
            }

            public string AbsoluteFilename { get; private set; }
            public string FormattedName { get; set; }

            public Include[] Includes { get; private set; }
        }

        public async Task<Item> InitializeAsync(string filename)
        {
            graphItems = new();
            return await TryEmplaceAsync(filename, Path.GetFileNameWithoutExtension(filename));
        }


        public async Task<Item> InitializeAsync(DocumentView document)
        {
            graphItems = new();
            return await TryEmplaceAsync(document);
        }
        public async Task<Item> TryEmplaceAsync(DocumentView document)
        {
            var absolute_filename = document.FilePath;
            bool is_new = !graphItems.TryGetValue(absolute_filename, out Item outItem);
            if (!is_new) return outItem;

            outItem = await ParseFileAsync(document);
            graphItems.Add(absolute_filename, outItem);
            return outItem;
        }
        private async Task<Item> ParseFileAsync(DocumentView document)
        {
            var incs = await VCUtil.GetIncludeDirsAsync();
            var path = document.FilePath;
            var doc_folder = Path.GetDirectoryName(path);

            var inc_arr = new string[] { 
                Microsoft.VisualStudio.PlatformUI.PathUtil.Normalize(doc_folder)
                + Path.DirectorySeparatorChar }
            .Concat(incs).ToArray();

            var text = document.TextBuffer.CurrentSnapshot.GetText();
            var includes = Parser.ParseInclues(text.AsSpan());

            return new Item(path, Path.GetFileName(path),
                includes.Select(s => new Include(s, inc_arr)).ToArray());
        }


        public async Task<Item> TryEmplaceAsync(string absolute_filename, string name)
        {
            bool is_new = !graphItems.TryGetValue(absolute_filename, out Item outItem);
            if (!is_new) return outItem;            

            outItem = await ParseFileAsync(absolute_filename, name);
            graphItems.Add(absolute_filename, outItem);
            return outItem;
        }

        private async Task<Item> ParseFileAsync(string absolute_filename, string name)
        {
            var doc = await VS.Documents.GetDocumentViewAsync(absolute_filename);
            doc ??= await VS.Documents.OpenViaProjectAsync(absolute_filename);

            var incs = await VCUtil.GetIncludeDirsAsync();
            var doc_folder = Path.GetDirectoryName(absolute_filename);

            var inc_arr = new string[] {
                Microsoft.VisualStudio.PlatformUI.PathUtil.Normalize(doc_folder)
                + Path.DirectorySeparatorChar }
            .Concat(incs).ToArray();

            var text = doc.TextBuffer.CurrentSnapshot.GetText();
            return new Item(absolute_filename, name, Parser.ParseInclues(text.AsSpan()).Select(s=>new Include(s, inc_arr)).ToArray());
        }

        private Dictionary<string, Item> graphItems = new();
    }
}
