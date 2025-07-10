using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.ViewModels;

namespace FileTidy.GUI;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data == null) return null;

        var viewModelType = data.GetType();
        var viewName = viewModelType.FullName!.Replace("ViewModel", "View");
        var viewNameType = Type.GetType(viewName);

        if (data is IUseMainLayout)
        {
            var layout = new Views.MainLayout();
            layout.DataContext = new MainLayoutViewModel(new Views.HomeView { DataContext = data });
            return layout;
        }

        if (viewNameType != null)
        {
            var view = (Control)Activator.CreateInstance(viewNameType)!;
            view.DataContext = data;
            return view;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data) => data is ViewModels.ViewModelBase;
}