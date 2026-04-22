using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SSHClientAvalonia.ViewModels;

namespace SSHClientAvalonia;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var vmType = param.GetType();
        if (!vmType.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            return new TextBlock { Text = "Not a ViewModel: " + vmType.FullName };

        var viewShortName = vmType.Name[..^"ViewModel".Length];
        var assembly = vmType.Assembly;
        var viewType =
            assembly.GetType($"SSHClientAvalonia.Views.{viewShortName}View")
            ?? assembly.GetType($"SSHClientAvalonia.Views.{viewShortName}");

        if (viewType != null)
            return (Control)Activator.CreateInstance(viewType)!;

        return new TextBlock { Text = "Not Found: SSHClientAvalonia.Views." + viewShortName };
    }

    public bool Match(object? data) => false;
}
