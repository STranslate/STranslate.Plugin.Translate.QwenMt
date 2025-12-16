using CommunityToolkit.Mvvm.ComponentModel;

namespace STranslate.Plugin.Translate.QwenMt;

public class Settings
{
    //自定义 API 地址，默认给一个通用的，可改
    public string Url { get; set; } = "https://ark.cn-beijing.volces.com/api/v3/chat/completions";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "doubao-1-5-pro-32k-250115";

    // 常用模型列表    public List<string> Models { get; set; } =
    [
        "doubao-1-5-pro-32k-250115",
        "deepseek-ai/DeepSeek-R1", 
        "deepseek-v3",
        "gpt-4o"
    ];

    public bool IsThinkingEnabled { get; set; } = false;
    public bool IsThinkingVisible { get; set; } = true;

}