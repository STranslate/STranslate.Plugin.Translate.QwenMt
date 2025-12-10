using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace STranslate.Plugin.Translate.QwenMt;

public partial class Settings : ObservableObject
{
    [ObservableProperty] public partial string ApiKey { get; set; } = string.Empty;

    [ObservableProperty] public partial string Model { get; set; } = "qwen-mt-turbo";

    [ObservableProperty]
    public partial ObservableCollection<string> Models { get; set; } =
    [
        "qwen-mt-turbo",
        "qwen-mt-plus"
    ];

    [ObservableProperty] public partial bool IsEnableTerms { get; set; }

    [ObservableProperty] public partial bool IsEnableDomains { get; set; }

    /// <summary>
    ///     术语列表
    /// </summary>
    [ObservableProperty] public partial ObservableCollection<Term> Terms { get; set; } = [];

    /// <summary>
    ///     领域提示
    /// </summary>
    [ObservableProperty] public partial string Domains { get; set; } = string.Empty;
}

public partial class Term : ObservableObject
{
    [ObservableProperty]
    public partial string SourceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TargetText { get; set; } = string.Empty;
}