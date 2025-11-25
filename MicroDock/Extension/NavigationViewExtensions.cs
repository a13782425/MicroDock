using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Extension;

internal static class NavigationViewExtensions
{
    private static Action<NavigationView>? _updatePaneLayoutAction;
    static NavigationViewExtensions()
    {
        var method = typeof(NavigationView).GetMethod("UpdatePaneLayout", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
        {
            _updatePaneLayoutAction = Delegate.CreateDelegate(typeof(Action<NavigationView>), null, method) as Action<NavigationView>;
        }
    }

    public static void UpdatePaneLayout(this NavigationView navigationView)
    {
        _updatePaneLayoutAction?.Invoke(navigationView);
    }
}
