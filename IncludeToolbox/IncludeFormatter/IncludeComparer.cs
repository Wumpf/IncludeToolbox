using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IncludeToolbox.IncludeFormatter
{
    public class IncludeComparer : IComparer<string>
    {
        public const string CurrentFileNameKey = "$(currentFilename)";

        public IncludeComparer(string[] precedenceRegexes, string documentName)
        {
            string currentFilename = documentName.Substring(0, documentName.LastIndexOf('.'));

            PrecedenceRegexes = new string[precedenceRegexes.Length];
            for (int i = 0; i < PrecedenceRegexes.Length; ++i)
            {
                PrecedenceRegexes[i] = precedenceRegexes[i].Replace(CurrentFileNameKey, currentFilename);
            }
        }

        public string[] PrecedenceRegexes { get; set; }

        public int Compare(string lineA, string lineB)
        {
            if (lineA == null)
            {
                if (lineB == null)
                    return 0;
                return -1;
            }
            else if (lineB == null)
            {
                return 1;
            }

            int precedenceA = 0;
            for (; precedenceA < PrecedenceRegexes.Length; ++precedenceA)
            {
                if (Regex.Match(lineA, PrecedenceRegexes[precedenceA]).Success)
                    break;
            }
            int precedenceB = 0;
            for (; precedenceB < PrecedenceRegexes.Length; ++precedenceB)
            {
                if (Regex.Match(lineB, PrecedenceRegexes[precedenceB]).Success)
                    break;
            }

            if (precedenceA == precedenceB)
                return lineA.CompareTo(lineB);
            else
                return precedenceA.CompareTo(precedenceB);
        }
    }
}
