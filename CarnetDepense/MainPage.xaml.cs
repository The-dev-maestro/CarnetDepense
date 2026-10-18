using CarnetDepenses.ViewModels;

namespace CarnetDepense;

public partial class MainPage : ContentPage
{
    private readonly DepensesViewModel _vm; // injecte

    public MainPage(DepensesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ChargerAsync(); // recharge
    }
}
