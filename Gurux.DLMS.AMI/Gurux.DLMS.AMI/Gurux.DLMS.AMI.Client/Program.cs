using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Helpers.Toaster;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Headers;
using System.Reflection;

namespace Gurux.DLMS.AMI.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthenticationStateDeserialization();
            builder.Services.AddScoped<ModuleLoader>();
            builder.Services.AddScoped<IGXCookieStorage, GXCookieStorage>();
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
                DefaultRequestHeaders =
                {
                    Accept =
                    {
                        new MediaTypeWithQualityHeaderValue("application/json")
                    }
                }
            });
            builder.Services.AddScoped<ConfirmBase>();
            builder.Services.AddSingleton<IGXToasterService>(new GXToasterService());
            GXNotifierService n = new GXNotifierService(builder.Services.BuildServiceProvider());
            builder.Services.AddSingleton(x => n);
            builder.Services.AddSingleton<IGXNotifier>(x => n);
            builder.Services.AddSingleton<IGXNotifier2>(x => n);
            builder.Services.AddSingleton<IGXLocalStorage, GXLocalStorage>();
            builder.Services.AddSingleton<IGXResourceStorage, GXResourceStorage>();
            //Add crypto service
            builder.Services.AddTransient<IAmiCryptoService, GXCryptoService>();

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(GXModulePolicies.Add, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(GXModulePolicies.View, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(GXModulePolicies.Edit, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(GXModuleGroupPolicies.View, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(GXDeviceTemplatePolicies.Edit, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(GXDeviceTemplateGroupPolicies.Edit, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAuthenticatedUser();
                });
            });
            var app = builder.Build();
            await app.RunAsync();
        }

        static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
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

        static Assembly? CurrentDomain_TypeResolve(object? sender, ResolveEventArgs args)
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
    }
}
