using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using FileTidy.Core.Interfaces;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;
using FileTidy.GUI.Reporting;
using FileTidy.GUI.ViewModels;
using FileTidy.GUI.ViewModels.Layouts;
using Moq;
using Xunit;

namespace FileTidy.Gui.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Mock<IServiceProvider> _services;

    public MainWindowViewModelTests()
    {
        _services = new Mock<IServiceProvider>();
        _viewModel = new MainWindowViewModel(_services.Object);
    }

    [Fact]
    public void AppVersion_ContainsVersionAndAuthor()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var expected = $"FileTidy v{version} | Built by Freemancodz";

        Assert.Equal(expected, _viewModel.AppVersion);
    }

}
