namespace IncludeToolbox
{
    public enum Standard
    {
        None,
        cpp11,
        cpp14,
        cpp17,
        cpp20,
        cpp23,
        cpp26,
        cpp29,
    }

    public static class ExtensionMethods
    {
        public static string ToStdFlag(this Standard e)
        {
            return e switch
            {
                Standard.cpp11 => "-std=c++11",
                Standard.cpp14 => "-std=c++14",
                Standard.cpp17 => "-std=c++17",
                Standard.cpp20 => "-std=c++20",
                Standard.cpp23 => "-std=c++2b",
                Standard.cpp26 => "-std=c++2b",
                Standard.cpp29 => "-std=c++2b",
                _ => "-std=c++2b",
            };
        }
        public static Standard FromMSVCFlag(string s)
        {
            var e = s switch
            {
                "stdcpp20" => Standard.cpp20,
                "stdcpplatest" => Standard.cpp23,
                "Default" => Standard.cpp14,
                "stdcpp17" => Standard.cpp17,
                "stdcpp14" => Standard.cpp14,
                _ => Standard.cpp23,
            };
            return e;
        }
    }
}
