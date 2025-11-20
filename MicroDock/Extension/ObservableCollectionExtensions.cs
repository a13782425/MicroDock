using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Extension;

internal static class ObservableCollectionExtensions
{
    public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> keySelector)
    {
        var sorted = collection.ToList();
        sorted.Sort(keySelector);
        for (int i = 0; i < sorted.Count; i++)
        {
            var oldIndex = collection.IndexOf(sorted[i]);
            // 如果当前元素不在正确的位置，移动它
            if (oldIndex != i)
            {
                collection.Move(oldIndex, i);
            }
        }
    }
}
