using System.Collections.Generic;

namespace CountDown
{
    public static class Extensions
    {
        public static T Pop<T>(this List<T> list)
        {
            var last = list[^1];
            list.RemoveAt(list.Count - 1);
            return last;
        }
    }
}