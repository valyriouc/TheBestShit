using System.Text.Json;

namespace iteration1.Response;

public readonly struct AppResponseInfo<T>(string message, T content)
{
    public string Message { get; } = message;

    public string Content { get; } = JsonSerializer.Serialize(content);
}