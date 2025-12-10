using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;

namespace STranslate.Plugin.Translate.QwenMt.ViewModel;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPluginContext _context;
    private bool _isUpdating = false;

    public Settings Settings { get; }

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        Settings = settings;
        Settings.PropertyChanged += (s, e) =>
        {
            _context.SaveSettingStorage<Settings>();
        };
        Settings.Models.CollectionChanged += (s, e) =>
        {
            _context.SaveSettingStorage<Settings>();
        };
        Settings.Terms.CollectionChanged += (s, e) =>
        {
            _context.SaveSettingStorage<Settings>();
        };
        Settings.Terms.CollectionChanged += (s, e) =>
        {
            if (e.OldItems != null)
            {
                foreach (Term item in e.OldItems)
                {
                    item.PropertyChanged -= Term_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (Term item in e.NewItems)
                {
                    item.PropertyChanged += Term_PropertyChanged;
                }
            }
        };
    }

    private void Term_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _context.SaveSettingStorage<Settings>();
    }

    [RelayCommand]
    private void AddModel(string model)
    {
        if (_isUpdating || string.IsNullOrWhiteSpace(model) || Settings.Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        Settings.Models.Add(model);
        Settings.Model = model;
    }

    [RelayCommand]
    private void DeleteModel(string model)
    {
        if (_isUpdating || !Settings.Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        if (Settings.Model == model)
            Settings.Model = Settings.Models.Count > 1 ? Settings.Models.First(m => m != model) : string.Empty;

        Settings.Models.Remove(model);
    }

    [RelayCommand]
    private void TermsAdd()
    {
        Settings.Terms.Add(new Term
        {
            SourceText = string.Empty,
            TargetText = string.Empty
        });
    }

    [RelayCommand]
    private void TermsDelete(Term term)
    {
        if (term != null)
        {
            Settings.Terms.Remove(term);
        }
    }

    [RelayCommand]
    private void TermsClear()
    {
        if (Settings.Terms.Count == 0)
            return;

        Settings.Terms.Clear();
    }

    [RelayCommand]
    private void TermsExport()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "qwen_terms.json",
                DefaultExt = "json"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            var json = JsonSerializer.Serialize(Settings.Terms, options);

            File.WriteAllText(saveFileDialog.FileName, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, $"Failed to export terms: {ex.Message}");
        }
    }

    [RelayCommand]
    private void TermsImport()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var json = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
            var terms = JsonSerializer.Deserialize<ObservableCollection<Term>>(json);

            if (terms != null)
            {
                Settings.Terms.Clear();
                foreach (var term in terms)
                {
                    Settings.Terms.Add(term);
                }
            }
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, $"Failed to import terms: {ex.Message}");
        }
    }

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly struct UpdateGuard : IDisposable
    {
        private readonly SettingsViewModel _viewModel;

        public UpdateGuard(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel._isUpdating = true;
        }

        public void Dispose() => _viewModel._isUpdating = false;
    }
}
