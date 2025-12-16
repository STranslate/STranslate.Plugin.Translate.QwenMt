using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace STranslate.Plugin.Translate.QwenMt.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;
    private bool _isUpdating = false;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        // 初始化绑定数据
        Url = settings.Url;
        ApiKey = settings.ApiKey;
        Model = settings.Model;
        Models = [.. settings.Models];
        IsThinkingEnabled = settings.IsThinkingEnabled;
        IsThinkingVisible = settings.IsThinkingVisible;

        // 监听属性变化以自动保存
        PropertyChanged += OnPropertyChanged;
        Models.CollectionChanged += OnModelsCollectionChanged;
    }

    /// <summary>
    /// 当 Models 集合发生变化时保存配置
    /// </summary>
    private void OnModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or
                       NotifyCollectionChangedAction.Remove or
                       NotifyCollectionChangedAction.Replace)
        {
            _settings.Models = [.. Models];
            _context.SaveSettingStorage<Settings>();
        }
    }

    /// <summary>
    /// 当单个属性发生变化时保存配置
    /// </summary>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Url):
                _settings.Url = Url;
                break;
            case nameof(ApiKey):
                _settings.ApiKey = ApiKey;
                break;
            case nameof(Model):
                _settings.Model = Model ?? string.Empty;
                break;
            case nameof(IsThinkingEnabled):
                _settings.IsThinkingEnabled = IsThinkingEnabled;
                break;
            case nameof(IsThinkingVisible):
                _settings.IsThinkingVisible = IsThinkingVisible;
                break;
            default:
                return;
        }
        _context.SaveSettingStorage<Settings>();
    }

    /// <summary>
    /// 添加新模型到列表
    /// </summary>
    [RelayCommand]
    private void AddModel(string model)
    {
        if (_isUpdating || string.IsNullOrWhiteSpace(model) || Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        Models.Add(model);
        Model = model;
    }

    /// <summary>
    /// 从列表删除模型
    /// </summary>
    [RelayCommand]
    private void DeleteModel(string model)
    {
        if (_isUpdating || !Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        if (Model == model)
            Model = Models.Count > 1 ? Models.First(m => m != model) : string.Empty;

        Models.Remove(model);
    }

    public void Dispose()
    {
        PropertyChanged -= OnPropertyChanged;
        Models.CollectionChanged -= OnModelsCollectionChanged;
    }

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

    // --- 属性定义 ---

    [ObservableProperty] 
    public partial string Url { get; set; }

    [ObservableProperty] 
    public partial string ApiKey { get; set; }

    [ObservableProperty] 
    public partial string Model { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> Models { get; set; }

    [ObservableProperty] 
    public partial bool IsThinkingEnabled { get; set; }

    [ObservableProperty] public partial bool IsThinkingVisible { get; set; }
}