using System;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp.Extensions
{
    public static class ArrayExtensions
    {
        public static T SafeGet<T>(this T[] array, int position) where T : class
        {
            return position >= array.Length ? null : array[position];
        }

        public static (int count, string result) CountAndRemove(this string str, string target)
        {
            var count = str.Split(new []{ target }, StringSplitOptions.None).Length - 1;
            return (count, str.Replace(target, string.Empty));
        }
    }
}
