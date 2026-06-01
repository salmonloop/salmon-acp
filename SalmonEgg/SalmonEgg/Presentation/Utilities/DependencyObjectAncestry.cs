using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace SalmonEgg.Presentation.Utilities;

internal static class DependencyObjectAncestry
{
    public static T? FindDescendant<T>(DependencyObject? root, Func<T, bool> predicate)
        where T : DependencyObject
    {
        if (root is null)
        {
            return default;
        }

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T typed && predicate(typed))
            {
                return typed;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var index = 0; index < childCount; index++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, index));
            }
        }

        return default;
    }

    public static bool IsDescendantOf(DependencyObject? current, DependencyObject ancestor)
    {
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    public static T? FindAncestorOrSelf<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = GetParent(current);
        }

        return default;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        if (current is FrameworkElement frameworkElement && frameworkElement.Parent is DependencyObject parent)
        {
            return parent;
        }

        return VisualTreeHelper.GetParent(current);
    }
}
