//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Client.Helpers;
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