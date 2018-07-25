using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DoudizhuSharp.Extensions
{
    public static class ReflectionExtensions
    {
        public static bool IsAttributeDefined<T>(this MethodInfo info) where T : Attribute
        {
            return info.GetCustomAttribute<T>() != null;
        }

        public static bool IsAttributeDefined<T>(this ParameterInfo info) where T : Attribute
        {
            return info.GetCustomAttribute<T>() != null;
        }
    }
}
