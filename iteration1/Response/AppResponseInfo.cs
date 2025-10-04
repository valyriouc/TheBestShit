using System.Net;
using System.Text.Json.Serialization;

namespace iteration1.Response;

public readonly struct AppResponseInfo<T>(
    HttpStatusCode statusCode,
    string message, 
    T? content = default)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    
    public string Message { get; } = message;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Content { get; } = content;
}