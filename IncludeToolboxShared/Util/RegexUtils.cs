using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    public static class RegexUtils
    {
        public const string CurrentFileNameKey = "$(currentFilename)";

        /// <summary>
        /// Replaces special macros in a regex list.
        /// </summary>
        /// <param name="precedenceRegexes"></param>
        /// <param name="documentName">Name of the current document without extension.</param>
        /// <returns></returns>
        public static string[] FixupRegexes(string[] precedenceRegexes, string documentName)
        {
            string[] regexes = new string[precedenceRegexes.Length];
            for (int i = 0; i < precedenceRegexes.Length; ++i)
            {
                regexes[i] = precedenceRegexes[i].Replace(CurrentFileNameKey, documentName);
            }
            return regexes;
        }
    }
}
