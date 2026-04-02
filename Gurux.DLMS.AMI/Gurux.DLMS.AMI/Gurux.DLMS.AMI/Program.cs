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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Components.Account;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Internal;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Scheduler;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Server.Prune;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yarp.ReverseProxy.Configuration;

namespace Gurux.DLMS.AMI
{
    /// <summary>
    /// Main program.
    /// </summary>
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ServerSettings.ServerAddress = builder.Configuration?.GetSection("Server")?.Get<ServerOptions>()?.Address;
            builder.Services.AddOutputCache(options =>
            {
                //Add cache.
                options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(10)));
            });
            //Add connection manager.
            builder.Services.AddSingleton<IGXConnectionManager, GXConnectionManager>();

            //Add module service.
            builder.Services.AddSingleton<IGXModuleService, GXModuleService>();
            builder.Services.AddSingleton(PluginActionDescriptorChangeProvider.Instance);
            builder.Services.AddSingleton<IActionDescriptorChangeProvider>(sp =>
                sp.GetRequiredService<PluginActionDescriptorChangeProvider>());

            //Add YARP reverse proxy.
            builder.Services.AddReverseProxy();
            builder.Services.AddSingleton<GXDbProxyConfigProvider>();
            builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<GXDbProxyConfigProvider>());
            builder.Services.AddHostedService<GXProxyConfigReloader>();

            builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
            CryptoOptions? signingKey = builder.Configuration.GetSection("Crypto").Get<CryptoOptions>();
            builder.Services.AddScoped<IGXLocalStorage, GXLocalStorage>();
            builder.Services.AddSingleton<IGXResourceStorage, GXResourceStorage>();
            builder.Services.AddHttpContextAccessor();
            //Suppor to GXLanguageService.
            builder.Services.AddSingleton<GXLanguageService>();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents()
                .AddAuthenticationStateSerialization();

            builder.Services.AddScoped<ModuleLoader>();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "AppAuth";           // policy scheme
                    //External signing e.g. Google is used.
                    options.DefaultSignInScheme = "AppAuth";
                })
            .AddPolicyScheme("AppAuth", "AppAuth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    // Use JWT when Bearer-header.
                    if (!string.IsNullOrEmpty(authHeader) &&
                        authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return IdentityConstants.BearerScheme;
                    }
                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddIdentityCookies();
            if (signingKey != null)
            {
                builder.Services.AddAuthentication().AddJwtBearer(IdentityConstants.BearerScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = ServerSettings.ServerAddress,
                    ValidAudience = "Gurux.DLMS.AMI.ServerAPI",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(signingKey.Key!)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha256Signature],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    //Signal-R needs special handling to get token from query string.
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        var accessToken = context.Request.Query["access_token"];

                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        Console.WriteLine($"[OnMessageReceived] path={path}");
                        Console.WriteLine($"  access_token (query) = {accessToken}");
                        Console.WriteLine($"  Authorization header = {authHeader}");

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/guruxami"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("[JwtBearer] Authentication failed: " +
                                          context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
            }
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("NotBanned", policy => policy.RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == "banned" && c.Value == "true")));
                ServerRequirements.AddServerRequirements(options, ServerSettings.ServerAddress);
            });
            GXActionDescriptorChangeProvider adcp = new GXActionDescriptorChangeProvider();
            builder.Services.AddSingleton<IActionDescriptorChangeProvider>(adcp);
            builder.Services.AddSingleton(adcp);

            using var loggerFactory = LoggerFactory.Create(s =>
            {
                s = builder.Logging;
            });
            ILogger<GXConfiguration> logger = loggerFactory.CreateLogger<GXConfiguration>();
            IGXHost host = await ServerSettings.ConnectToDb(builder, logger);
            ClaimsPrincipal? adminUser = null;
            if (host != null)
            {
                adminUser = ServerSettings.GetDefaultAdminUser(host);
            }
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<ApplicationRole>()  // Enable role management
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddTransient<IGXAmiContextAccessor, GXAmiContextAccessor>();
            builder.Services.AddScoped<IGXCookieStorage, GXCookieStorage>();
            builder.Services.AddScoped<ConfirmBase>();
            builder.Services.AddSingleton<IGXToasterService>(new GXToasterService());
            //Add crypto service
            builder.Services.AddTransient<IAmiCryptoService, GXCryptoService>();
            // register the scope authorization handler
            builder.Services.AddSingleton<IAuthorizationHandler, ScopedAccessHandler>();
            builder.Services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            })
                .AddViewLocalization()
                 .AddDataAnnotationsLocalization(options =>
                 {
                     options.DataAnnotationLocalizerProvider = (type, factory) =>
                         factory.Create(typeof(Properties.Resources));
                 });
            builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                // Vain Admin-rooli voi avata /Account/Register
                options.Conventions.AuthorizeAreaPage(
                    areaName: "Identity",
                    pageName: "/Account/Register",

                    policy: "admin");
            }); ;
            // ForwardedHeaders for reverse-proxy (Nginx/Traefik)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Add trusted proxy addresses:
                // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
            });

            //Add SignalR.
            builder.Services.AddSignalR();
            //Add performance settings.
            builder.Services.AddSingleton<GXPerformanceSettings>();
            //Add cron.
            builder.Services.AddHostedService<IGXCronService, GXCronService>();
            builder.Services.AddScoped<IGXCronTask, GXUpdater>();
            //Add prune.
            builder.Services.AddHostedService<IGXPruneService, GXPruneService>();

            builder.Services.AddTransient<UserManager<ApplicationUser>, GXApplicationUserManager>();

            //Add task late bind service
            builder.Services.AddSingleton<ITaskLateBindHandler, GXTaskLateBindService>();
            //Add subtotal service
            builder.Services.AddHostedService<ISubtotal, GXSubtotalService>();
            //Add report service
            builder.Services.AddHostedService<IReport, GXReportService>();
            //Add events listener.
            builder.Services.AddEventService();
            //Add repositories.
            ServerSettings.AddRepositories(builder.Services);
            SchedulerOptions? s = builder.Configuration?.GetSection("Scheduler").Get<SchedulerOptions>();
            if (s != null)
            {
                if (!s.Disabled)
                {
                    builder.Services.Configure<SchedulerOptions>(conf => conf = s);
                    builder.Services.AddHostedService<IGXScheduleService, GXSchedulerService>();
                }
                else
                {
                    logger.LogInformation("Scheduler service is disabled.");
                }
            }
            else
            {
                logger.LogInformation("Scheduler service is disabled.");
            }
            GXNotifierService n = new GXNotifierService(builder.Services.BuildServiceProvider());
            builder.Services.AddSingleton(x => n);
            builder.Services.AddSingleton<IGXNotifier>(x => n);
            builder.Services.AddSingleton<IGXNotifier2>(x => n);
            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GXExceptionHandler>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Open", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(ServerSettings.ServerAddress)
            });
            builder.Services.AddHttpClient("Api", (sp, client) =>
            {
                var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is missing.");
                var req = ctx.Request;
                client.BaseAddress = new Uri($"{req.Scheme}://{req.Host}/");
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
            var app = builder.Build();
            //Logging must be before exception handler.
            app.UseRequestResponseLogging();
            app.UseExceptionHandler();            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //Swagger UI is used.
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Gurux.DLMS.AMI API v1");
            });
            //Add scalar API reference.
            app.MapScalarApiReference();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            //Black list is added after user is authenticated.
            app.UseMiddleware<GXBlackListMidleware>();
            app.MapHub<GuruxAMiHub>("/guruxami");
            app.UseRouting();
            app.UseCors("Open");
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapStaticAssets();
            var razor = app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            if (host != null && ServerSettings.GetDefaultAdminUser(host) != null)
            {
                app.UseMaintenanceModeHandler();
            }
            app.MapControllers();
            app.MapGet("/openapi/v1.json", async (HttpRequest req, CancellationToken ct) =>
            {
                return GXOpenApiSchemaFilter.MakeOpenApiDocument();
            });
            if (host != null && ServerSettings.GetDefaultAdminUser(host) != null &&
                signingKey?.Key == "47757275784C746447757275784C746447757275784C746447757275784C7464")
            {

                using (var scope = app.Services.CreateScope())
                {
                    var log = new GXSystemLog(TraceLevel.Warning)
                    {
                        CreationTime = DateTime.Now,
                        Message = "Change the default signing key to a custom one.",
                    };
                    var repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    await repository.AddAsync(TargetType.System, [log]);
                }
            }

            //Create localized resources.
            if (host != null && adminUser != null && !host.Connection.SelectAll<GXLocalizedResource>().Any())
            {
                using (var scope = app.Services.CreateScope())
                {
                    var admin = new GXUser() { Id = ClientHelpers.GetUserId(adminUser) };
                    var enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                    var languages = host.Connection.SelectAll<GXLanguage>();
                    await ServerHelpers.UpdateLanguageResourcesAsync(host,
                        enumTypeRepository, languages, admin);
                }
            }
            app.MapGet("/api/module/load", ([FromServices] IGXModuleService modules, string id) =>
            {
                /*
                var module = modules.Get(id);
                if (module == null ||
                    string.IsNullOrEmpty(module.LoadContext?.Path) ||
                    !System.IO.File.Exists(module.LoadContext.Path))
                {
                    return Results.NotFound();
                }
                return Results.File(File.ReadAllBytes(module.LoadContext.Path), "application/octet-stream", module.Name);
                */
            });


            app.MapGet("/debug/controllers", (ApplicationPartManager pm) =>
            {
                var feature = new ControllerFeature();
                pm.PopulateFeature(feature);
                return feature.Controllers.Select(t => t.FullName).OrderBy(x => x);
            });

            app.MapGet("/debug/apis", (IApiDescriptionGroupCollectionProvider api) =>
            {
                return api.ApiDescriptionGroups.Items
                    .SelectMany(g => g.Items)
                    .Select(d => $"{d.HttpMethod} {d.RelativePath} -> {d.ActionDescriptor.DisplayName}")
                    .OrderBy(x => x);
            });

            app.MapGet("/debug/endpoints", (IEnumerable<EndpointDataSource> sources) =>
            {
                return sources.SelectMany(s => s.Endpoints)
                    .OfType<RouteEndpoint>()
                    .Select(e => e.RoutePattern.RawText)
                    .Distinct()
                    .OrderBy(x => x);
            });

            // Register the reverse proxy routes
            app.MapReverseProxy();
            app.Run();
        }
    }
}
