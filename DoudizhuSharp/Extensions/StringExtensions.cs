using System;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp.Extensions
{
    public static class StringExtensions
    {
        public static string StringJoin(this IEnumerable<string> strings, string separator)
        {
            return string.Join(separator, strings);
        } 
    }
}
