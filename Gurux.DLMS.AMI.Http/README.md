# Gurux.DLMS.AMI.Http

Dependency-free HTTP helpers shared by AMI clients, agents and modules. 
This package does not reference the AMI Shared DTOs or Blazor.

```csharp
using Gurux.DLMS.AMI.Http;

await response.EnsureSuccessAsync(cancellationToken);
var reply = await client.GetAsJsonAsync<MyResponse>("api/example", cancellationToken);
await client.PostAsJson("api/example", request, cancellationToken);
```

`EnsureSuccessAsync` preserves AMI problem-details, validation and maintenance error handling. Maintenance responses throw `GXMaintenanceException`, including `EndTime` and `RetryAfter` when supplied. The caller owns responses passed to this method.

The JSON helpers validate the response, deserialize replies, and dispose their HTTP responses. JSON request serialization omits default values. Typed helpers reject a null JSON reply.

API errors throw `GXHttpException` (derived from `HttpRequestException`) with the HTTP
`StatusCode`, problem `Title` and `Detail`, diagnostic `TraceId`, and a read-only
snapshot of validation `Errors`. Unknown metadata does not replace the server's
explanation. HTTP 401 and 403 continue to throw `UnauthorizedAccessException`,
regardless of the problem title or response format. Maintenance responses continue
to throw `GXMaintenanceException`.

To include explicit zero and false values, pass custom request options:

```csharp
var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
await client.PostAsJson("api/example", request, cancellationToken, options);
var reply = await client.PostAsJson<MyResponse>("api/example", request, cancellationToken, options);
```

Custom options apply to request serialization only; response parsing retains web
JSON defaults. Existing overloads retain their original property names and omit
default values. Structured API errors now derive from `HttpRequestException`
instead of using plain `Exception`; validation messages also retain problem titles
and details when supplied.

## License

This software is available under either of the following licenses:

- **Open-source license:** GNU General Public License, version 2 only
  (GPL-2.0-only). See [LICENSE](../LICENSE) for the full license text.
- **Commercial license:** Available from Gurux Ltd under a separate
  written agreement. This option allows use under the terms of that
  agreement instead of GPL-2.0-only.

A valid commercial license is required to use the commercial licensing
option. See [Gurux Commercial License Notice](../LICENSES/LicenseRef-Gurux-Commercial.txt).

For commercial licensing inquiries, [contact Gurux Ltd](https://www.gurux.fi/).
