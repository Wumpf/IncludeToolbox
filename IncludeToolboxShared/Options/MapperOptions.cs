using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class MapperOptionsPage : BaseOptionPage<MapperOptions> { }
    }

    public class MapperOptions : BaseOptionModel<MapperOptions>
    {
        string map_path = "";
        private MapManager map = new();

        [Browsable(false)]
        public MapManager Map { get { return map; } }

        [Category("General")]
        [DisplayName("Mapping file")]
        [Description("File to write results to.")]
        [DefaultValue("")]
        public string MapFile { get => map_path; set
            {
                if (map_path == value) return;
                map_path = value;
                map.Load(map_path);
            }
        }



        [Category("General")]
        [DisplayName("Relative File Prefix")]
        [Description("Prefix for relative file path stored into map. e.g. C:\\users\\map\\a.h with prefix C:\\users will write <map/a.h> to the final map.")]
        [DefaultValue("")]
        public string Prefix { get; set; } = "";
        
        [Category("General")]
        [DisplayName("Mapping preference")]
        [Description("Choose to prefer one option of inclusion over other. In case other than default the other option is marked as private and will be replaced.")]
        [DefaultValue(MappingPreference.Default)]
        public MappingPreference Preference { get; set; } = MappingPreference.Default;
        
        [Category("General")]
        [DisplayName("Ignore #ifdefs")]
        [Description("If true, the headers inside ifdef blocks are treated as unavailable. May be removed with preprocessor introduction.")]
        [DefaultValue(false)]
        public bool Ignoreifdefs { get; set; } = false;

        public async Task<bool> IsInMapAsync()
        {
            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            var str = Utils.MakeRelative(Prefix, doc.FilePath).Replace('\\', '/');
            return map.Map.ContainsKey(str);
        }
    }

    public enum MappingPreference
    {
        Default,
        Quotes,
        AngleBrackets
    }
}
