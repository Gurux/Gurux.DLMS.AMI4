using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Gurux.DLMS.AMI.Server.Internal
{
    public class GXActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        public CancellationTokenSource TokenSource { get; private set; } = new CancellationTokenSource();

        public IChangeToken GetChangeToken()
        {
            if (TokenSource.IsCancellationRequested)
            {
                TokenSource = new CancellationTokenSource();
            }
            return new CancellationChangeToken(TokenSource.Token);
        }
    }
}
