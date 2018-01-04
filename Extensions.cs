using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Project
{
    internal static class Extensions
    {
        public static T Last<T>(this IEnumerable<T> _)
        {
            List<T> l = new List<T>(_);
            return l[l.Count - 1];
        } 
    }
}
