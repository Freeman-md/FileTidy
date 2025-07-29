using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.Reporting;
using FileTidy.GUI.ViewModels;
using Moq;
using Xunit;

namespace FileTidy.Gui.Tests.ViewModels;

public class MainLayoutViewModelTests
{
    private readonly MainLayoutViewModel _viewModel;

    public MainLayoutViewModelTests()
    {
    
        _viewModel = new MainLayoutViewModel();
    }

    [Fact]
    public void AppVersion_ContainsVersionAndAuthor()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var expected = $"FileTidy v{version} | Built by Freemancodz";

        Assert.Equal(expected, _viewModel.AppVersion);
    }

}
