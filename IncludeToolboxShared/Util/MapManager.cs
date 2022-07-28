using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    public class MapManager
    {
        readonly Regex regExp = new("^\\s*{\\s*include:\\s*\\[\\s*\\\"(?:(?:\\\\\")|<)([\\w\\/\\.]*)(?:(?:\\\\\")|>)\\\"\\s*,\\s*(?:public|private),\\s\\\"(?:(?:\\\\\")|<)([\\w\\/\\.]*)(?:(?:\\\\\")|>)\\\"\\s*,\\s*(?:public|private)");
        Dictionary<string, string> map = new();

        public Dictionary<string, string> Map { get { return map; } }

        public void Load(string map_path)
        {
            map.Clear();
            if(string.IsNullOrEmpty(map_path)||!File.Exists(map_path))return;

            var strings = File.ReadAllLines(map_path);

            foreach (string s in strings)
            {
                var m = regExp.Match(s);
                if (!m.Success) continue;
                if (!map.ContainsKey(m.Groups[2].Value))
                    map.Add(m.Groups[2].Value, "");
                map[m.Groups[2].Value] += s + "\n";
            }
        }
        public async Task WriteMapAsync(MapperOptions settings)
        {
            FileStream file = File.OpenWrite(settings.MapFile);
            List<Task> array = new();

            await file.WriteAsync(Encoding.ASCII.GetBytes("[\n"), 0, 2);

            foreach (var f in map)
            {
                var c = "#" + f.Key + "\n";
                array.Append(file.WriteAsync(Encoding.ASCII.GetBytes(c), 0, c.Length));
                array.Append(file.WriteAsync(Encoding.ASCII.GetBytes(f.Value), 0, f.Value.Length));
            }

            foreach (var f in array)
                await f;

            await file.WriteAsync(Encoding.ASCII.GetBytes("]\n"), 0, 2);

            file.Close();
        }
        public void TryRemoveValue(string key)
        {
            map.Remove(key);
        }
    }
}
