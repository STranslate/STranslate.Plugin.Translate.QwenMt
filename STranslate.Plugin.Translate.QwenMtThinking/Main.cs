using STranslate.Plugin.Translate.QwenMt.View;
using STranslate.Plugin.Translate.QwenMt.ViewModel;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.QwenMt;

public class Main : TranslatePluginBase
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    /// 将语言枚举转换为英文名称，供 Prompt 使用
    /// </summary>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "Chinese Simplified",
        LangEnum.ChineseTraditional => "Chinese Traditional",
        LangEnum.Cantonese => "Cantonese",
        LangEnum.English => "English",
        LangEnum.Japanese => "Japanese",
        LangEnum.Korean => "Korean",
        LangEnum.French => "French",
        LangEnum.Spanish => "Spanish",
        LangEnum.Russian => "Russian",
        LangEnum.German => "German",
        LangEnum.Italian => "Italian",
        LangEnum.Turkish => "Turkish",
        LangEnum.PortuguesePortugal => "Portuguese (Portugal)",
        LangEnum.PortugueseBrazil => "Portuguese (Brazil)",
        LangEnum.Vietnamese => "Vietnamese",
        LangEnum.Indonesian => "Indonesian",
        LangEnum.Thai => "Thai",
        LangEnum.Malay => "Malay",
        LangEnum.Arabic => "Arabic",
        LangEnum.Hindi => "Hindi",
        LangEnum.Khmer => "Khmer",
        LangEnum.NorwegianBokmal => "Norwegian Bokmål",
        LangEnum.NorwegianNynorsk => "Norwegian Nynorsk",
        LangEnum.Persian => "Persian",
        LangEnum.Swedish => "Swedish",
        LangEnum.Polish => "Polish",
        LangEnum.Dutch => "Dutch",
        LangEnum.Ukrainian => "Ukrainian",
        _ => null
    };

    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "Chinese Simplified",
        LangEnum.ChineseTraditional => "Chinese Traditional",
        LangEnum.Cantonese => "Cantonese",
        LangEnum.English => "English",
        LangEnum.Japanese => "Japanese",
        LangEnum.Korean => "Korean",
        LangEnum.French => "French",
        LangEnum.Spanish => "Spanish",
        LangEnum.Russian => "Russian",
        LangEnum.German => "German",
        LangEnum.Italian => "Italian",
        LangEnum.Turkish => "Turkish",
        LangEnum.PortuguesePortugal => "Portuguese (Portugal)",
        LangEnum.PortugueseBrazil => "Portuguese (Brazil)",
        LangEnum.Vietnamese => "Vietnamese",
        LangEnum.Indonesian => "Indonesian",
        LangEnum.Thai => "Thai",
        LangEnum.Malay => "Malay",
        LangEnum.Arabic => "Arabic",
        LangEnum.Hindi => "Hindi",
        LangEnum.Khmer => "Khmer",
        LangEnum.NorwegianBokmal => "Norwegian Bokmål",
        LangEnum.NorwegianNynorsk => "Norwegian Nynorsk",
        LangEnum.Persian => "Persian",
        LangEnum.Swedish => "Swedish",
        LangEnum.Polish => "Polish",
        LangEnum.Dutch => "Dutch",
        LangEnum.Ukrainian => "Ukrainian",
        _ => null
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() => _viewModel?.Dispose();

    public override async Task TranslateAsync(TranslateRequest request, TranslateResult result, CancellationToken cancellationToken = default)
    {
        // 1. 验证配置
        var apiUrl = Settings.Url?.Trim();
        if (string.IsNullOrEmpty(apiUrl))
        {
            result.Fail("API URL is empty. Please configure it in settings.");
            return;
        }

        if (GetSourceLanguage(request.SourceLang) is not string sourceStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
            return;
        }
        if (GetTargetLanguage(request.TargetLang) is not string targetStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
            return;
        }

        // 2. 准备模型与Prompt
        var model = Settings.Model.Trim();
        model = string.IsNullOrEmpty(model) ? "doubao-1-5-pro-32k-250115" : model;

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = $"You are a professional translator. Translate the following content from {sourceStr} to {targetStr}. Only output the translation result."
            },
            new
            {
                role = "user",
                content = request.Text
            }
        };

        // 3. 构建请求体 
        var requestBody = new Dictionary<string, object>
        {
            { "model", model },
            { "messages", messages },
            { "stream", true }
        };

        // 处理深度思考开关
        if (Settings.IsThinkingEnabled)
        {
            requestBody.Add("thinking", new { type = "enabled" });
        }
        else
        {
            // 部分接口可能需要显示禁用，或者不传
            requestBody.Add("thinking", new { type = "disabled" });
        }

        // 4. 设置 Header
        var options = new Options
        {
            Headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {Settings.ApiKey}" }
            }
        };

        // 状态标记：用于控制标题输出，避免重复
        bool hasPrintedThinkingHeader = false;
        bool hasPrintedResultHeader = false;
        bool isThinkingPhase = false;

        // 5. 发送流式请求
        await Context.HttpService.StreamPostAsync(apiUrl, requestBody, msg =>
        {
            if (string.IsNullOrEmpty(msg) || msg.Trim() == "data: [DONE]") return;
            var cleanMsg = msg.Replace("data:", "").Trim();
            if (string.IsNullOrEmpty(cleanMsg)) return;

            try
            {
                var node = JsonNode.Parse(cleanMsg);
                var delta = node?["choices"]?[0]?["delta"];

                var reasoning = delta?["reasoning_content"]?.ToString();
                var content = delta?["content"]?.ToString();

                // 只有当 Settings.IsThinkingVisible 为 True 时，才处理 reasoning
                if (Settings.IsThinkingVisible && !string.IsNullOrEmpty(reasoning))
                {
                    if (!hasPrintedThinkingHeader)
                    {
                        result.Text += "🤔 [Deep Thinking]\n";
                        hasPrintedThinkingHeader = true;
                        isThinkingPhase = true;
                    }
                    result.Text += reasoning;
                }

                if (!string.IsNullOrEmpty(content))
                {
                    // 如果之前处于“显示思考”的模式，现在转正文了，需要加分割线
                    if (isThinkingPhase)
                    {
                        if (!hasPrintedResultHeader)
                        {
                            result.Text += "\n\n🚀 [Translation]\n";
                            hasPrintedResultHeader = true;
                        }
                        isThinkingPhase = false;
                    }
                    
                    // 正常追加正文
                    result.Text += content;
                }
            }
            catch { /* 忽略错误 */ }

        }, options, cancellationToken);
    }
}