using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace IncludeToolbox
{
    internal static class IWYUApply
    {
        static readonly Regex pragma = new("#pragma\\s+once");
        static readonly Regex remove = new Regex(@"^-\s+(.+)\s+\/\/ lines (\d+)-(\d+)$");
        public struct AddTask
        {
            public AddTask(string Info, bool inc = true)
            {
                this.inc = inc;
                this.Info = Info;
            }
            public bool inc;
            public string Info;
        }
        public struct RemoveTask
        {
            public bool inc;
            public string Info;
            public Span LineRange;

            public RemoveTask(bool inc, string Info, Span LineRange) : this()
            {
                this.inc = inc;
                this.Info = Info;
                this.LineRange = LineRange;
            }
        }
        public struct ApplyTasks
        {
            public List<AddTask> add;
            public List<RemoveTask> rem;

            public ApplyTasks(List<AddTask> add, List<RemoveTask> rem)
            {
                this.add = add;
                this.rem = rem;
                rem.Sort((a, b) => { return b.LineRange.Start.CompareTo(a.LineRange.Start); });
            }

            public void Apply(ITextEdit edit)
            {
                int start_pos = 0;
                var lines = edit.Snapshot.Lines;
                foreach (var line in lines)
                {
                    string text = line.GetText();
                    var match = pragma.Match(text);
                    if (match.Success)
                        start_pos = match.Index + line.End;
                    var i = text.IndexOf("#include");
                    if (i >= 0)
                    {
                        start_pos = i + line.Start;
                        break;
                    }
                }

                foreach (var (r, l) in from r in rem
                                       let l = lines.Skip(r.LineRange.Start).Take(r.LineRange.Length + 1)
                                       select (r, l))
                {
                    var first = l.First();
                    if (r.inc)
                    {
                        int begin = first.Start + first.GetTextIncludingLineBreak().IndexOf("#include");
                        edit.Delete(begin, first.End - begin);
                    }
                    else
                        edit.Delete(first.Start, first.End - first.Start);
                }
                foreach (var a in add)
                {
                    // later namespace parse
                    edit.Insert(start_pos, a.Info + GetLineBreak(edit));
                }
            }
        }

        public static Span GetIncludeSpan(string text)
        {
            int[] line = new int[2];
            line[0] = text.IndexOf("#include"); //first
            line[1] = text.IndexOf("\n", text.LastIndexOf("#include")) - line[0]; //last
            return new Span(line[0], line[1]);
        }

        public static string GetLineBreak(ITextEdit edit)
        {
            return edit.Snapshot.Lines.ElementAt(0).GetLineBreakText();
        }
        public static void ApplyCheap(ITextEdit edit, string result, bool commentary)
        {
            if(!commentary)
            {
                var lb = edit.Snapshot.Lines.ElementAt(0).GetLineBreakText();
                lb = string.IsNullOrEmpty(lb) ? "\r\n" : lb;
                result = string.Join(lb, result.Split('\n')
                    .Select(s =>
                    {
                        var str = s.Trim();
                        var idx = str.IndexOf("//");
                        if (idx >= 0)
                            return str.Substring(0, idx).Trim();
                        return str;
                    }).ToArray());
            }
            var span = GetIncludeSpan(edit.Snapshot.GetText());
            edit.Replace(span, result);
        }

        public static async Task<string> PreformatAsync(string text, string file)
        {
            var include_directories = await VCUtil.GetIncludeDirsAsync();
            return Formatter.IncludeFormatter.FormatIncludes(text, file, include_directories, await FormatOptions.GetLiveInstanceAsync());
        }

        public static ApplyTasks ParseTasks(IEnumerable<string> tasks, bool commentary)
        {
            List<AddTask> add = new();
            List<RemoveTask> rem = new();
            for (int i = 0; i < tasks.Count();i++)
            {
                var task = tasks.ElementAt(i);
                if (task.EndsWith(" should add these lines:"))
                {
                    while (true)
                    {
                        task = tasks.ElementAt(++i);
                        if (string.IsNullOrEmpty(task)) break;
                        add.Add(new AddTask(commentary ? task : task.Substring(0, task.IndexOf("//")),
                                            inc: task.StartsWith("#include")));
                    }
                }
                if (task.EndsWith(" should remove these lines:"))
                {
                    while (true)
                    {
                        task = tasks.ElementAt(++i);
                        if (string.IsNullOrEmpty(task)) break;
                        var match = remove.Match(task);

                        rem.Add(new RemoveTask(inc: task.StartsWith("- #include"),
                            Info: match.Groups[1].Value,
                            LineRange: Span.FromBounds(start:int.Parse(match.Groups[2].Value) - 1, end:int.Parse(match.Groups[3].Value) - 1) //starts from 0
                            ));
                    }
                }
            }
            return new ApplyTasks(add, rem);
        }
    }
}
