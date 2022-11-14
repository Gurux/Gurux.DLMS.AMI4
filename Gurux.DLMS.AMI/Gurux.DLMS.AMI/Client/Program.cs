using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Helpers.ContextMenu;
using Gurux.DLMS.AMI.Client.Helpers.Toaster;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Reflection;

AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("Gurux.DLMS.AMI.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gurux.DLMS.AMI.ServerAPI"));
builder.Services.AddScoped<ConfirmBase>();
builder.Services.AddSingleton<GXModuleService>();
builder.Services.AddSingleton<IGXToasterService>(new GXToasterService());

GXNotifierService n = new GXNotifierService(builder.Services.BuildServiceProvider());
builder.Services.AddSingleton(x => n);
builder.Services.AddSingleton<IGXNotifier>(x => n);

builder.Services.AddApiAuthorization()
    .AddAccountClaimsPrincipalFactory<CustomUserFactory>();
builder.Services.AddApiAuthorization();
await builder.Build().RunAsync();

Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
{
    try
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (args.Name.StartsWith(assembly.GetName().Name))
            {
                return assembly;
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex.Message);
    }
    return null;
}

Assembly? CurrentDomain_TypeResolve(object? sender, ResolveEventArgs args)
{
    try
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (args.Name.StartsWith(assembly.GetName().Name))
            {
                return assembly;
            }
        }
        //Late binding...
        foreach (AssemblyName assembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
        {
            if (args.Name.StartsWith(assembly.Name))
            {
                return Assembly.Load(assembly);
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex.Message);
    }
    System.Diagnostics.Debug.WriteLine(args.Name);
    return null;

}