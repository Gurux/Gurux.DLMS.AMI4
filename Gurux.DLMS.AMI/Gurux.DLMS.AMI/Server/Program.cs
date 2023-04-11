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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Server.Data;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Scheduler;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
// builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Trace);
// builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Trace);

builder.Host.UseServiceProviderFactory(context => new GXServiceProviderFactory());
GXActionDescriptorChangeProvider adcp = new GXActionDescriptorChangeProvider();
builder.Services.AddSingleton<IActionDescriptorChangeProvider>(adcp);
builder.Services.AddSingleton(adcp);

builder.Services.AddDataProtection().UseCryptographicAlgorithms(
    new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    });

using var loggerFactory = LoggerFactory.Create(s =>
{
    s = builder.Logging;
});
ILogger<GXConfiguration> logger = loggerFactory.CreateLogger<GXConfiguration>();

IGXHost host = ServerSettings.ConnectToDb(builder, logger);
ServerSettings.ServerAddress = builder.Configuration.GetSection("Server").Get<ServerOptions>().Address;
SchedulerOptions s = builder.Configuration.GetSection("Scheduler").Get<SchedulerOptions>();
builder.Services.Configure<SchedulerOptions>(conf => conf = s);
if (!s.Disabled)
{
    builder.Services.AddHostedService<IGXScheduleService, GXSchedulerService>();
}
else
{
    logger.LogInformation("Scheduler service is disabled.");
}

//Add events listener.
builder.Services.AddEventService();

//Add performance settings.
builder.Services.AddSingleton<GXPerformanceSettings>();

//Add cron.
builder.Services.AddHostedService<IGXCronService, GXCronService>();
builder.Services.AddScoped<IGXCronTask, GXUpdater>();

builder.Services.AddTransient<UserManager<ApplicationUser>, GXApplicationUserManager>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
CryptoOptions c = builder.Configuration.GetSection("Crypto").Get<CryptoOptions>();
if (c.Key == "47757275784C746447757275784C7464")
{
    if (host != null)
    {
        host.Connection.Insert(GXInsertArgs.Insert(new GXSystemLog(TraceLevel.Warning)
        {
            CreationTime = DateTime.Now,
            Message = "Change default crypto key.",
        }));
    }
}
//Add module service
if (host != null)
{
    GXModulesService modules = new GXModulesService();
    builder.Services.AddSingleton(modules);
    modules.LoadModules(host, builder.Services);
    builder.Services.AddSingleton<IGXModuleService, GXModuleService>();
}

//Add crypto service
builder.Services.AddTransient<IGXCryproService, GXCryproService>();
//Add workflow service
builder.Services.AddSingleton<IWorkflowHandler, GXWorkflowService>();

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager<GXApplicationSignInManager>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>(options =>
    {
        const string OpenId = "openid";
        options.IdentityResources[OpenId].UserClaims.Add(ClaimTypes.GivenName);
        options.IdentityResources[OpenId].UserClaims.Add(JwtClaimTypes.Email);
        options.ApiResources.Single().UserClaims.Add(JwtClaimTypes.Email);
        options.IdentityResources[OpenId].UserClaims.Add(JwtClaimTypes.Role);
        options.ApiResources.Single().UserClaims.Add(JwtClaimTypes.Role);
    });
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("role");

var auth = builder.Services.AddAuthentication();
auth.AddIdentityServerJwt();
auth.AddJwtBearer();
//Add external authentication services.
if (host != null)
{
    AuthenticationSettings[]? authSettings = ServerSettings.GetServerSettings<AuthenticationSettings[]>(host, GXConfigurations.Authentication);
    if (authSettings != null)
    {
        foreach (AuthenticationSettings it in authSettings)
        {
            if (it.Disabled)
            {
                continue;
            }
            if (it.Name == "GitHub" && !string.IsNullOrEmpty(it.ClientId) &&
                !string.IsNullOrEmpty(it.ClientSecret))
            {
                auth.AddOAuth("OAuth", "GitHub", o =>
                {
                    o.ClientId = it.ClientId;
                    o.ClientSecret = it.ClientSecret;
                    o.CallbackPath = new PathString("/signin-github");
                    o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    o.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    o.UserInformationEndpoint = "https://api.github.com/user";
                    o.ClaimsIssuer = "OAuth2-Github";
                    o.SaveTokens = true;
                    // Retrieving user information is unique to each provider.
                    o.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            // Get the GitHub user
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();
                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                            ServerSettings.AddGitHubClaims(context, user);
                        }
                    };
                });
            }
        }
    }
}

//Get access token.
builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    ServerRequirements.AddServerRequirements(options, ServerSettings.ServerAddress);
});

// register the scope authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, ScopedAccessHandler>();
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
});

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

if (host != null)
{
    SecuritySettings? o = ServerSettings.GetServerSettings<SecuritySettings>(host, "Security");
    builder.Services.Configure<IdentityOptions>(options =>
    {
        if (o != null)
        {
            ServerSettings.UpdateSecuritySettings(options, o);
        }
    });
}

builder.Services.AddHttpClient("Gurux.DLMS.AMI.ServerAPI", client => client.BaseAddress = new Uri(ServerSettings.ServerAddress));
// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gurux.DLMS.AMI.ServerAPI"));
//Add support for Swagger.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gurux.DLMS.AMI",
        Version = "v1",

        Description = "REST interface for managing Gurux.DLMS.AMI.",
        Contact = new OpenApiContact
        {
            Name = "Contact",
            Url = new Uri("https://gurux.fi/contact")
        },
        License = new OpenApiLicense
        {
            Name = "This code is licensed under the GNU General Public License v2.",
            Url = new Uri("http://www.gnu.org/licenses/gpl-2.0.txt")
        }

    });
    c.SchemaFilter<SwaggerSchemaFilter>();
    //Add XML documantation.
    var filePath = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    filePath = Path.Combine(AppContext.BaseDirectory, filePath);
    c.IncludeXmlComments(filePath);


    // add JWT Authentication
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "JWT Authorization header using the Bearer scheme.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", // must be lower case
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {securityScheme, Array.Empty<string>() }
    });
});

if (host != null)
{
    if (host.Connection != null)
    {
        host.Connection.Insert(GXInsertArgs.Insert(new GXSystemLog(TraceLevel.Info)
        {
            CreationTime = DateTime.Now,
            Message = "Service started: " + ServerSettings.ServerAddress
        }));
    }
}

ServerSettings.AddRepositories(builder.Services);
var app = builder.Build();

if (host != null)
{
    //Get connected agents and change status to offline.
    GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(w => w.Removed == null && w.Status != AgentStatus.Offline);
    arg.Columns.Add<GXUser>(s => s.Id);
    arg.Joins.AddInnerJoin<GXAgent, GXUser>(j => j.Creator, j => j.Id);
    List<GXAgent> agents = host.Connection.Select<GXAgent>(arg);
    IAgentRepository? agentRepository = app.Services.GetService<IAgentRepository>();
    if (agentRepository != null)
    {
        foreach (GXAgent agent in agents)
        {
            await agentRepository.UpdateStatusAsync(ServerHelpers.CreateClaimsPrincipalFromUser(agent.Creator), agent.Id, AgentStatus.Offline);
        }
    }
}
//Hub.
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
    //Add support for Swagger.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gurux.DLMS.AMI v1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseMiddleware<GXAuthenticationMiddleware>();
app.UseMiddleware<GXBlackListMidleware>();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

if (host != null && host.Connection != null)
{
    //Add localization.
    List<GXLanguage> cultures = host.Connection.Select<GXLanguage>(GXSelectArgs.SelectAll<GXLanguage>(w => w.Active == true));
    string? defCulture = cultures.Where(w => w.Default == true).Select(w => w.Id).SingleOrDefault();
    string[] supportedCultures = cultures.Select(q => q.Id).ToArray();
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture(defCulture)
        .AddSupportedCultures(supportedCultures)
        .AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
        {
            string id = ServerHelpers.GetUserId(context.User, false);
            if (id == null)
            {
                //If user is not authenticated yet.
                return new ProviderCultureResult(defCulture);
            }
            string? language = host.Connection.SingleOrDefault<string>(GXSelectArgs.SelectById<GXUser>(id, c => c.Language));
            if (string.IsNullOrEmpty(language))
            {
                return new ProviderCultureResult(defCulture);
            }
            return new ProviderCultureResult(language);
        }))
        .AddSupportedUICultures(supportedCultures);
    app.UseRequestLocalization(localizationOptions);
}
//Add logging.
app.UseRequestResponseLogging();
//Add error handling.
app.UseErrorHandler();
app.UseMaintenanceModeHandler();

IHostApplicationLifetime appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
if (appLifetime != null)
{
    appLifetime.ApplicationStopping.Register(() =>
    {
        app.Logger.LogInformation("Gurux.DLMS.AMI is stopping...");
    });
    appLifetime.ApplicationStopped.Register(() =>
    {
        app.Logger.LogInformation("Gurux.DLMS.AMI stopped.");
    });
}
//Update start time.
IConfigurationRepository? configurationRepository = app.Services.GetService<IConfigurationRepository>();
if (configurationRepository != null && host != null && host.Connection != null)
{
    ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = "Status" } };
    var cp = ServerSettings.GetDefaultAdminUser(host);
    if (cp != null)
    {
        GXConfiguration[] confs = await configurationRepository.ListAsync(
            cp,
            req, null, CancellationToken.None);
        if (confs.Length == 1 && !string.IsNullOrEmpty(confs.First().Settings))
        {
            StatusSettings? status = JsonSerializer.Deserialize<StatusSettings>(confs.First().Settings);
            if (status != null)
            {
                status.StartTime = DateTime.Now;
                confs[0].Settings = JsonSerializer.Serialize(status);
                await configurationRepository.UpdateAsync(cp, confs, true);
            }
        }
    }
}

app.MapRazorPages();
app.MapHub<GuruxAMiHub>("/guruxami");
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
