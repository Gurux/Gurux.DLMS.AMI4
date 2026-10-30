# Gurux.DLMS.AMI.Http

Dependency-free HTTP helpers shared by AMI clients, agents and modules. This package does not reference the AMI Shared DTOs or Blazor.

```csharp
using Gurux.DLMS.AMI.Http;

await response.EnsureSuccessAsync(cancellationToken);
var reply = await client.GetAsJsonAsync<MyResponse>("api/example", cancellationToken);
await client.PostAsJson("api/example", request, cancellationToken);
```

`EnsureSuccessAsync` preserves AMI problem-details, validation and maintenance error handling. Maintenance responses throw `GXMaintenanceException`, including `EndTime` and `RetryAfter` when supplied. The caller owns responses passed to this method.

The JSON helpers validate the response, deserialize replies, and dispose their HTTP responses. JSON request serialization omits default values. Typed helpers reject a null JSON reply.
