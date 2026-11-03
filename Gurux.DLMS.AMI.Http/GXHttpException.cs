using System.Net;
using System.Collections.ObjectModel;

namespace Gurux.DLMS.AMI.Http;

/// <summary>An HTTP error retaining structured API diagnostics.</summary>
public class GXHttpException : HttpRequestException
{
    /// <summary>Short problem description.</summary>
    public string? Title { get; }
    /// <summary>Explanation of this occurrence.</summary>
    public string? Detail { get; }
    /// <summary>Server trace identifier for diagnostics.</summary>
    public string? TraceId { get; }
    /// <summary>Validation messages grouped by field.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <summary>Creates an HTTP error with an immutable snapshot of validation messages.</summary>
    public GXHttpException(string message, HttpStatusCode statusCode, string? title = null,
        string? detail = null, string? traceId = null, IDictionary<string, string[]>? errors = null)
        : base(message, null, statusCode)
    {
        Title = title;
        Detail = detail;
        TraceId = traceId;
        Errors = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            (errors ?? new Dictionary<string, string[]>()).ToDictionary(p => p.Key,
                p => (IReadOnlyList<string>)Array.AsReadOnly(p.Value.ToArray()), StringComparer.Ordinal));
    }
}
