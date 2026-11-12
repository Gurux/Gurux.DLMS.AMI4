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

using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Components.Account;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Update;
using Gurux.DLMS.AMI.Scheduler;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Prune;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Db;

namespace Gurux.DLMS.AMI
{
    /// <summary>
    /// Main program.
    /// </summary>
    internal class Program
    {
        // Framework and static files are public: route them before cookie validation
        // so parallel asset downloads cannot renew or clear the application session.
        private static void MapPublicAssets(IEndpointRouteBuilder app)
            => app.MapStaticAssets().ShortCircuit();
        private static async Task Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--health-check")
            {
                // The health-check process must not initialize or migrate the database again.
                try
                {
                    using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    string expected = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).FileVersion!;
                    string actual = await probe.GetStringAsync(Environment.GetEnvironmentVariable("AMI_HEALTH_URL") ?? "http://127.0.0.1:8080/api/Configuration/Version");
                    Environment.ExitCode = actual.Trim().Trim('"') == expected ? 0 : 1;
                }
                catch { Environment.ExitCode = 1; }
                return;
            }
            CliCommand cliCommand = GXSettings.GetCliCommand(args, out string[] hostArgs);
            var builder = WebApplication.CreateBuilder(hostArgs);
            // Installation secrets and persistent paths survive release-directory switches.
            string? deploymentSettings = Environment.GetEnvironmentVariable("AMI_SETTINGS_FILE");
            if (!string.IsNullOrWhiteSpace(deploymentSettings))
            {
                if (!Path.IsPathFullyQualified(deploymentSettings))
                {
                    throw new InvalidOperationException("AMI_SETTINGS_FILE must be absolute.");
                }
                builder.Configuration.AddJsonFile(deploymentSettings, optional: false, reloadOnChange: false);
                builder.Configuration.AddEnvironmentVariables();
                builder.Configuration.AddCommandLine(hostArgs);
            }
            if (builder.Environment.IsDevelopment())
            {
                string? developmentSettings = Environment.GetEnvironmentVariable("AMI_DEVELOPMENT_SETTINGS");
                builder.Configuration.AddJsonFile(developmentSettings ?? "appsettings.DevelopmentModules.json",
                    optional: developmentSettings == null, reloadOnChange: false);
                // Command-line overrides retain their usual highest precedence.
                builder.Configuration.AddCommandLine(hostArgs);
            }
            using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
            ILogger<GXConfiguration> logger = loggerFactory.CreateLogger<GXConfiguration>();
            GXDbConnection? connection = await ServerSettings.ConnectToDatabase(builder, logger);
            if (await GXSettings.TryHandleCliCommandAsync(connection, cliCommand, logger))
            {
                return;
            }

            ServerSettings.ServerAddress = builder.Configuration.GetSection("Server").Get<ServerOptions>()?.Address ?? throw new InvalidOperationException("The Server:Address setting is missing.");
            builder.Services.AddOutputCache(options =>
            {
                //Add cache.
                options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(10)));
            });
            //Add connection manager.
            builder.Services.AddSingleton<IGXConnectionManager, GXConnectionManager>();

            builder.Services.AddScoped<IGXAmiTemplateTokenProvider, Server.Token.GXSiteTemplateTokenProvider>();
            builder.Services.AddScoped<IGXAmiTemplateTokenProvider, Server.Token.GXMaintenanceTemplateTokenProvider>();
            builder.Services.AddScoped<IGXAmiTemplateTokenResolver, Server.Token.GXTemplateTokenResolver>();
            //Add module service.
            builder.Services.AddKeyedSingleton<Action<string>>(GXDeviceTemplateImportValidator.ServiceKey,
                (_, _) => GXDeviceTemplateImportValidator.Validate);
            builder.Services.AddSingleton<Server.Module.GXModulePendingUpdates>();
            builder.Services.AddSingleton<IGXModuleService, GXModuleService>();
            builder.Services.AddSingleton<Server.Module.GXDevelopmentModuleResolver>();
            builder.Services.AddSingleton<Server.Module.GXModuleRuntime>();
            builder.Services.AddSingleton<Server.Module.IGXModuleAuthenticationRuntime>(sp => sp.GetRequiredService<Server.Module.GXModuleRuntime>());
            builder.Services.AddScoped<Server.Module.IGXModulePrincipalValidator, Server.Module.GXModulePrincipalValidator>();
            builder.Services.AddSingleton<IGXAmiModuleServices>(sp => sp.GetRequiredService<Server.Module.GXModuleRuntime>());
            builder.Services.AddSingleton<IGXAmiEmailSender, Server.Module.GXModuleEmailSender>();
            builder.Services.AddSingleton<IGXNotification, Server.Module.GXModuleNotificationSender>();
            builder.Services.AddSingleton<Hubs.GXWebPushNotifier>();
            builder.Services.AddSingleton<Hubs.GXEmailNotifier>();
            builder.Services.AddSingleton<Server.Module.GXModuleEndpointDataSource>();
            builder.Services.AddHostedService<Server.Module.GXModuleStartupService>();

            builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
            CryptoOptions? signingKey = builder.Configuration.GetSection("Crypto").Get<CryptoOptions>();
            builder.Services.AddHttpContextAccessor();
            //Suppor to GXLanguageService.
            builder.Services.AddSingleton<GXLanguageService>();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents()
                .AddAuthenticationStateSerialization();

            builder.Services.AddAmiAppearance();
            builder.Services.AddScoped<ModuleLoader>();
            builder.Services.AddScoped<GXConfirmationService>();
            builder.Services.AddScoped<IGXAmiConfirmation>(sp => sp.GetRequiredService<GXConfirmationService>());
            builder.Services.AddSingleton<IGXAmiModuleEventPublisher, Server.Module.GXModuleEventPublisher>();
            builder.Services.AddScoped<Gurux.UI.Components.IGXProgress, Gurux.UI.Components.GXProgressService>();
            builder.Services.AddScoped<Gurux.UI.Components.IGXNotificationService, Gurux.UI.Components.GXNotificationService>();
            builder.Services.AddScoped<GXBackgroundTaskNotifications>();
            builder.Services.AddScoped<GXScriptCommunityNotifications>();
            builder.Services.AddScoped<Gurux.UI.Components.IGXMenuService, Gurux.UI.Components.GXMenuService>();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<SignInManager<ApplicationUser>, GXApplicationSignInManager>();

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "AppAuth";           // policy scheme
                    //External signing e.g. Google is used.
                    options.DefaultSignInScheme = "AppAuth";
                })
            .AddPolicyScheme("AppAuth", displayName: null, options =>
            {
                options.ForwardDefaultSelector = Server.Module.GXModuleAuthenticationHandler.SelectScheme;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Server.Module.GXModuleAuthenticationHandler>(
                Server.Module.GXModuleAuthenticationHandler.SchemeName, _ => { })
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
                    ValidateLifetime = true,
                    LifetimeValidator = GXPatValidator.ValidateLifetime
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = GXPatValidator.ReadHubTokenAsync,
                    OnTokenValidated = GXPatValidator.ValidateBearerAsync
                };
            });
            }
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("NotBanned", policy => policy.RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == "banned" && c.Value == "true")));
            });
            builder.Services.AddSingleton<IGXAmiPolicyRegistry, InMemoryPolicyRegistry>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, GXAuthorizationPolicyProvider>();
            builder.Services.AddScoped<IAuthorizationHandler, GXAuthorizationRequirementHandler>();

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddErrorDescriber<CustomIdentityErrorDescriber>()
                .AddRoles<ApplicationRole>()  // Enable role management
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager<GXApplicationSignInManager>()
                .AddDefaultTokenProviders();
            //Black list is added after user is authenticated.
            builder.Services.AddSingleton<GXCookieAuthenticationEvents>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<GXUserPrincipalCache>();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.EventsType = typeof(GXCookieAuthenticationEvents);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, GXUserClaimsPrincipalFactory>();
            builder.Services.AddScoped<ClientCacheService>();
            builder.Services.AddScoped<CachedHttpClient>();
            //Add crypto service
            builder.Services.AddTransient<IGXAmiCryptoService, GXCryptoService>();
            builder.Services.AddScoped<Client.Shared.KeyManagement.IKeyIdentityResolver, Server.Services.KeyManagement.GXKeyIdentityResolver>();
            builder.Services.AddScoped<Server.Services.KeyManagement.GXKeyAcquisitionService>();
            builder.Services.AddScoped<Server.Services.KeyManagement.GXDeviceSettingsService>();
            builder.Services.AddScoped<Server.Services.KeyManagement.GXPasswordChangeService>();
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
            builder.Services.AddRazorPages();
            // ForwardedHeaders for reverse-proxy (Nginx/Traefik)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Add trusted proxy addresses:
                // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
            });
            //Add SignalR.
            builder.Services.AddSingleton<GXPatConnections>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GXPatConnections>());
            builder.Services.AddSignalR().AddHubOptions<GuruxAMiHub>(options => options.AddFilter<GXPatConnections>());
            builder.Services.AddSingleton<NotificationCache>();
            builder.Services.Configure<Server.Services.ImageStorageOptions>(builder.Configuration.GetSection("ImageStorage"));
            builder.Services.AddSingleton<Server.Services.GXImageStorage>();
            builder.Services.AddHostedService<GXServerNotificationConfiguration>();
            builder.Services.AddHostedService<NotificationFlushService>();
            builder.Services.AddSingleton<Gurux.DLMS.AMI.Module.AmiEventHub>(sp =>
                new Gurux.DLMS.AMI.Module.AmiEventHub(error => sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ModuleEvents").LogError(error, "Module event subscriber failed.")));
            builder.Services.AddSingleton<Gurux.DLMS.AMI.Module.IGXAmiEvents>(sp =>
                sp.GetRequiredService<Gurux.DLMS.AMI.Module.AmiEventHub>());
            builder.Services.AddSingleton<Gurux.DLMS.AMI.Module.IGXAmiUserActivityStore, GXUserActivityStore>();
            builder.Services.AddSingleton<GXDiagnosticsService>();
            builder.Services.AddSingleton<Gurux.DLMS.AMI.Module.IGXAmiDiagnosticsSource>(sp => sp.GetRequiredService<GXDiagnosticsService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GXDiagnosticsService>());

            //Add cron.
            builder.Services.AddSingleton<GXBackgroundTaskRunner>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GXBackgroundTaskRunner>());
            builder.Services.AddHostedService<IGXBackgroundService, GXCronService>();
            builder.Services.Configure<CatalogUpdaterOptions>(builder.Configuration.GetSection("Updater"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CatalogUpdaterOptions>>().Value);
            builder.Services.AddScoped<GXCatalogStore>();
            builder.Services.AddScoped<GXScriptCatalogStore>();
            builder.Services.AddScoped<GXScriptCatalogService>();
            builder.Services.AddScoped<IGXScriptCommunityStore, GXScriptCommunityStore>();
            builder.Services.AddScoped<GXScriptCommunityService>();
            builder.Services.AddHostedService<GXScriptCommunityWorker>();
            builder.Services.AddHttpClient("ScriptCommunity", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false, UseCookies = false, UseDefaultCredentials = false, Credentials = null
            });
            builder.Services.AddScoped<GXUpdateStatusStore>();
            builder.Services.AddScoped<GXUpdateSettingsStore>();
            builder.Services.AddScoped<GXUpdateEmailNotifier>();
            builder.Services.AddScoped<GXCatalogSynchronizer>();
            builder.Services.AddHttpClient<GXLanguageCatalog>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.DLMS.AMI/4"));
            builder.Services.AddHttpClient<GXCatalogClient>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.DLMS.AMI/4"));
            builder.Services.AddHttpClient("TutorialVideos", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.DLMS.AMI/4");
            });
            builder.Services.AddHttpClient("ModulePackages");
            builder.Services.AddHttpClient("AmiManual", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.DLMS.AMI/4");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddSingleton(services => GXAmiManualHelpCatalog.CreateReader(services.GetRequiredService<IHttpClientFactory>()));
            builder.Services.AddHttpClient("ManufacturerUpdates");
            builder.Services.AddAmiHttpProxy(builder.Configuration.GetSection("HttpProxyLimits"));
            builder.Services.AddScoped<GXUpdater>();
            builder.Services.AddScoped<IGXBackgroundTask>(sp => sp.GetRequiredService<GXUpdater>());
            builder.Services.AddSingleton(new GXBackgroundTaskRegistration(GXBackgroundTaskIds.Update,
                sp => sp.GetRequiredService<GXUpdater>(), Scheduled: true));
            foreach (var taskId in new[] { GXBackgroundTaskIds.AgentUpdate, GXBackgroundTaskIds.ModuleUpdate, GXBackgroundTaskIds.ManufacturerUpdate })
            {
                builder.Services.AddSingleton(new GXBackgroundTaskRegistration(taskId,
                    sp => new GXUpdateTask(taskId, sp.GetRequiredService<GXUpdater>())));
            }
            //Add prune.
            builder.Services.AddHostedService<IGXPruneService, GXPruneService>();

            builder.Services.AddTransient<UserManager<ApplicationUser>, GXApplicationUserManager>();

            //Add task late bind service
            builder.Services.AddSingleton<GXTaskLateBindService>();
            builder.Services.AddSingleton<ITaskLateBindHandler>(sp => sp.GetRequiredService<GXTaskLateBindService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GXTaskLateBindService>());
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
            builder.Services.AddSingleton<IdentityNoOpEmailSender>();
            builder.Services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<IdentityNoOpEmailSender>());
            builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(sp => sp.GetRequiredService<IdentityNoOpEmailSender>());
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
            app.UseForwardedHeaders();
            app.MapGroup("/account").MapIdentityApi<ApplicationUser>()
                .Add(IdentityComponentsEndpointRouteBuilderExtensions.PreferIdentityConfirmationPage);
            app.UseRouting();
            // User action logging requires the authenticated request principal.
            app.UseAuthentication();
            app.UseMiddleware<Server.Module.GXModuleAuthenticationGuard>();
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
            app.UseAmiStatusCodePages();

            if (connection != null && (await ServerSettings.GetDefaultAdminUserAsync(connection)) != null)
            {
                app.UseMaintenanceModeHandler();
            }
            app.UseMiddleware<Server.Module.GXModuleMiddleware>();
            app.UseHttpsRedirection();
            app.MapHub<GuruxAMiHub>("/guruxami", options => options.CloseOnAuthenticationExpiration = true).RequireAuthorization();
            app.UseCors("Open");
            app.UseAuthorization();
            app.UseRateLimiter();
            app.UseAntiforgery();
            MapPublicAssets(app);
            ((IEndpointRouteBuilder)app).DataSources.Add(app.Services.GetRequiredService<Server.Module.GXModuleEndpointDataSource>());
            var razor = app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.MapControllers();
            // The deployment marker is added after publish, so it is absent from MapStaticAssets' build manifest.
            app.MapGet("/version.json", (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                string marker = Path.Combine(app.Environment.WebRootPath, "version.json");
                return System.IO.File.Exists(marker) ? (IResult)Results.File(marker, "application/json") : Results.NotFound();
            }).AllowAnonymous();
            await PolicyRepository.InitializeDefaultPolicies(app.Services.GetRequiredService<IDbConnectionFactory>(),
                app.Services.GetRequiredService<IGXAmiPolicyRegistry>());
            await using (var logMigration = await app.Services.GetRequiredService<IDbConnectionFactory>().GetConnectionAsync())
            {
                await GXLogStore.EnsureBuiltinTypesAsync(logMigration);
                await GXApplicationUpdates.ApplyAsync(logMigration, typeof(Program).Assembly.GetName().Version!);
            }
            // Register the reverse proxy routes
            app.Run();
        }
    }
}
