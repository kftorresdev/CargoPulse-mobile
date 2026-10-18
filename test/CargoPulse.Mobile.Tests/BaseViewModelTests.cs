using CargoPulse.Mobile.ViewModels;
using FluentAssertions;

namespace CargoPulse.Mobile.Tests;

public class TestViewModel : BaseViewModel
{
    public void SetBusy(bool busy)
    {
        IsBusy = busy;
    }
}

public class BaseViewModelTests
{
    [Fact]
    public void BaseViewModel_InitialState_IsNotBusyIsTrue()
    {
        // Arrange & Act
        var vm = new TestViewModel();

        // Assert
        vm.IsBusy.Should().BeFalse();
        vm.IsNotBusy.Should().BeTrue();
        vm.Title.Should().BeEmpty();
    }

    [Fact]
    public void BaseViewModel_WhenBusyChanged_IsNotBusyUpdatesAccordingly()
    {
        // Arrange
        var vm = new TestViewModel();
        var isNotBusyNotified = false;
        vm.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(BaseViewModel.IsNotBusy))
            {
                isNotBusyNotified = true;
            }
        };

        // Act
        vm.SetBusy(true);

        // Assert
        vm.IsBusy.Should().BeTrue();
        vm.IsNotBusy.Should().BeFalse();
        isNotBusyNotified.Should().BeTrue();
    }
}
