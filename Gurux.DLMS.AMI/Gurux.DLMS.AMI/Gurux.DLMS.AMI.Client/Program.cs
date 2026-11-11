// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Module;
using Gurux.UI.Component.Toaster;
using Gurux.UI.Components;
using Gurux.UI.Components.Help;
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
            if (builder.HostEnvironment.Environment == "Static")
            {
                builder.RootComponents.Add<Routes>("#app");
                builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");
            }

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, GXSessionStateProvider>();
            builder.Services.AddScoped<ModuleLoader>();
            builder.Services.AddScoped<Gurux.UI.Components.TemplateToken.ITemplateTokenEditorService, GXTemplateTokenEditorService>();
            builder.Services.AddSingleton<Gurux.UI.Components.Tab.ITabRouteResolver, GXTabRouteResolver>();
            builder.Services.AddScoped<IGXCookieStorage, GXCookieStorage>();
            builder.Services.AddScoped<ClientCacheService>();
            builder.Services.AddScoped<CachedHttpClient>();
            builder.Services.AddScoped(sp => new HttpClient(new GXSessionExpiredHandler(
                sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>())
            {
                InnerHandler = new HttpClientHandler()
            })
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
            builder.Services.AddScoped<GXConfirmationService>();
            builder.Services.AddScoped<IGXAmiConfirmation>(sp => sp.GetRequiredService<GXConfirmationService>());
            builder.Services.AddScoped<IGXProgress, GXProgressService>();
            builder.Services.AddScoped<IGXNotificationService, GXNotificationService>();
            builder.Services.AddScoped<GXBackgroundTaskNotifications>();
            builder.Services.AddScoped<GXScriptCommunityNotifications>();
            builder.Services.AddScoped<IGXMenuService, GXMenuService>();
            builder.Services.AddSingleton<IGXToasterService>(new GXToasterService());
            builder.Services.AddScoped<GXAmiNotifierService>();
            builder.Services.AddScoped<Gurux.UI.Components.GXNotifierService>(services => services.GetRequiredService<GXAmiNotifierService>());
            builder.Services.AddScoped<IGXNotifier>(services => services.GetRequiredService<GXAmiNotifierService>());
            builder.Services.AddScoped<IGXNotifier2>(services => services.GetRequiredService<GXAmiNotifierService>());
            builder.Services.AddSingleton<GXLocalStorage>();
            builder.Services.AddSingleton<IGXLocalStorage, GXBrowserStorage>();
            builder.Services.AddAmiResourceStorage();
            //Add crypto service
            builder.Services.AddTransient<IGXAmiCryptoService, GXCryptoService>();

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<GXClientAuthorizationCache>();
            builder.Services.AddScoped<IGXAuthorizationRefreshSource>(sp => sp.GetRequiredService<GXClientAuthorizationCache>());
            builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, GXClientAuthorizationPolicyProvider>();
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, GXClientAuthorizationHandler>();
            builder.Services.AddAmiHelp();
            var app = builder.Build();
            await app.RunAsync();
        }

        static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name is string name && args.Name.StartsWith(name, StringComparison.Ordinal))
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
                    if (assembly.GetName().Name is string name && args.Name.StartsWith(name, StringComparison.Ordinal))
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
