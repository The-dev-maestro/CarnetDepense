using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using CarnetDepenses.Models;
using CarnetDepenses.Services;

namespace CarnetDepenses.ViewModels;

public class DepensesViewModel : BaseViewModel
{
    private readonly IDepenseRepository _repo;
    private readonly IApiService _api;

    private string _libelle = "", _montantTexte = "";
    private string _categorieSelectionnee = "Alimentation";
    private bool _isBusy;
    private string _totalConverti = "", _message = "";

    public ObservableCollection<Depense> Depenses { get; } = new();
    public decimal Total => Depenses.Sum(d => d.Montant);
    public string NombreDepensesTexte => $"{Depenses.Count} dépense{(Depenses.Count > 1 ? "s" : "")}";

    public List<string> Categories { get; } = new()
    {
        "Alimentation", "Transport", "Factures", "Loisirs", "Santé", "Autre"
    };

    public string Libelle
    {
        get => _libelle;
        set { if (SetField(ref _libelle, value)) AjouterCommand.Rafraichir(); }
    }

    public string MontantTexte
    {
        get => _montantTexte;
        set { if (SetField(ref _montantTexte, value)) AjouterCommand.Rafraichir(); }
    }

    public string CategorieSelectionnee
    {
        get => _categorieSelectionnee;
        set => SetField(ref _categorieSelectionnee, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value))
                ConvertirCommand.Rafraichir();
        }
    }

    public string TotalConverti
    {
        get => _totalConverti;
        set => SetField(ref _totalConverti, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public bool SimulerPanne
    {
        get => FakeApiHandler.SimulerPanne;
        set
        {
            if (FakeApiHandler.SimulerPanne != value)
            {
                FakeApiHandler.SimulerPanne = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatutReseauTexte));
            }
        }
    }

    public string StatutReseauTexte => SimulerPanne ? "Hors-Ligne (Panne simulée)" : "En Ligne (API connectée)";

    public RelayCommand AjouterCommand { get; }
    public RelayCommandP SupprimerCommand { get; }
    public RelayCommand ConvertirCommand { get; }
    public RelayCommand ViderToutCommand { get; }

    public DepensesViewModel(IDepenseRepository repo, IApiService api)
    {
        _repo = repo;
        _api = api;

        AjouterCommand = new RelayCommand(
            async () => await AjouterAsync(),
            () => !string.IsNullOrWhiteSpace(Libelle)
               && !string.IsNullOrWhiteSpace(MontantTexte));

        SupprimerCommand = new RelayCommandP(
            async p => await SupprimerAsync((Depense)p!));

        ConvertirCommand = new RelayCommand(
            async () => await ConvertirAsync(),
            () => !IsBusy);

        ViderToutCommand = new RelayCommand(
            async () => await ViderToutAsync(),
            () => Depenses.Count > 0);
    }

    public async Task ChargerAsync()
    {
        Depenses.Clear();
        foreach (var d in await _repo.ListerAsync())
            Depenses.Add(d);
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(NombreDepensesTexte));
        ViderToutCommand.Rafraichir();
    }

    private async Task AjouterAsync()
    {
        if (!decimal.TryParse(MontantTexte, out decimal m))
            return;

        var d = new Depense
        {
            Libelle = Libelle,
            Montant = m,
            Date = DateTime.Today,
            Categorie = string.IsNullOrWhiteSpace(CategorieSelectionnee) ? "Général" : CategorieSelectionnee
        };

        await _repo.AjouterAsync(d);
        Depenses.Add(d);

        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(NombreDepensesTexte));
        ViderToutCommand.Rafraichir();

        Libelle = "";
        MontantTexte = "";
    }

    private async Task SupprimerAsync(Depense d)
    {
        await _repo.SupprimerAsync(d);
        Depenses.Remove(d);
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(NombreDepensesTexte));
        ViderToutCommand.Rafraichir();
    }

    private async Task ViderToutAsync()
    {
        var liste = Depenses.ToList();
        foreach (var d in liste)
        {
            await _repo.SupprimerAsync(d);
            Depenses.Remove(d);
        }
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(NombreDepensesTexte));
        TotalConverti = "";
        Message = "";
        ViderToutCommand.Rafraichir();
    }

    private async Task ConvertirAsync()
    {
        IsBusy = true;
        Message = "";
        try
        {
            var t = await _api.GetTauxAsync();
            Preferences.Set("taux_eur", (double)t.Taux["EUR"]);
            TotalConverti = $"{Total * t.Taux["EUR"]:F2} EUR";
            Message = "Conversion effectuée en direct via l'API.";
        }
        catch (HttpRequestException)
        {
            double c = Preferences.Get("taux_eur", 0.0);
            if (c == 0)
            {
                Message = "Hors ligne, pas de taux enregistré.";
            }
            else
            {
                TotalConverti = $"{Total * (decimal)c:F2} EUR (cache)";
                Message = "Hors ligne : conversion basée sur le dernier taux en cache.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
