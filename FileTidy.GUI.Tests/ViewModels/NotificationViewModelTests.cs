using System.Threading.Tasks;
using FileTidy.GUI.ViewModels;
using Xunit;

namespace FileTidy.Gui.Tests.ViewModels;

public class NotificationViewModelTests
{
    [Fact]
    public void Show_SetsTitleMessageAndVisibility()
    {
        // Arrange
        var viewModel = new NotificationViewModel();

        // Act
        viewModel.Show("Upload Complete", "Your files have been sorted");

        // Assert
        Assert.True(viewModel.IsNotificationVisible);
        Assert.Equal("Upload Complete", viewModel.NotificationTitle);
        Assert.Equal("Your files have been sorted", viewModel.NotificationMessage);
    }

    [Fact]
    public async Task Show_HidesNotificationAfterDelay()
    {
        // Arrange
        var viewModel = new NotificationViewModel();

        // Act
        viewModel.Show("Reminder", "Don’t forget to back up");

        // Assert initial state
        Assert.True(viewModel.IsNotificationVisible);

        // Wait for the delay to complete (4s delay in Show method)
        await Task.Delay(4500); // give a small buffer

        // Assert final state
        Assert.False(viewModel.IsNotificationVisible);
    }
}
