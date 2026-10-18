using CargoPulse.Mobile.ViewModels;

namespace CargoPulse.Mobile.Views;

public partial class InspectionPage : ContentPage
{
    private readonly InspectionViewModel _viewModel;

    public InspectionPage(InspectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnSaveSignatureClicked(object sender, EventArgs e)
    {
        try
        {
            var stream = await DrawingPad.GetImageStream(400, 200);
            if (stream != null)
            {
                await _viewModel.SaveSignatureCommand.ExecuteAsync(stream);
            }
        }
        catch
        {
            // Error is handled inside InspectionViewModel
        }
    }

    private void OnClearSignatureClicked(object sender, EventArgs e)
    {
        DrawingPad.Clear();
        _viewModel.ClearSignatureCommand.Execute(null);
    }
}
