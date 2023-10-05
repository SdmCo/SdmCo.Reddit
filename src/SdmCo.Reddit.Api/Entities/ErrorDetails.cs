using System.Text.Json;

namespace SdmCo.Reddit.Api.Entities;

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = default!;

    public override string ToString() => JsonSerializer.Serialize(this);
}