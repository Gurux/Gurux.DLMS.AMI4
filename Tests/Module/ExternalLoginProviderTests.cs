using System.Text.Json;
using Gurux.DLMS.AMI.Module.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public sealed class ExternalLoginProviderTests
{
    [TestMethod]
    public void ExternalLoginContractsAndRegistrationAreAvailable()
    {
        var assembly = typeof(AmiModuleBuilder).Assembly;
        Assert.IsNotNull(assembly.GetType("Gurux.DLMS.AMI.Module.IGXAmiExternalLoginProvider"));
        Assert.IsNotNull(assembly.GetType("Gurux.DLMS.AMI.Module.AmiExternalLoginDefinition"));
        Assert.IsNotNull(assembly.GetType("Gurux.DLMS.AMI.Module.Enums.AmiExternalLoginProtocol"));
        Assert.IsNotNull(typeof(AmiModuleBuilder).GetMethod("AddExternalLoginProvider"));
    }

    [TestMethod]
    public async Task ProviderUsesModuleScopeDependenciesAndIsDisposedWithItsScope()
    {
        var builder = CreateBuilder();
        builder.Services.AddScoped<Dependency>();
        builder.AddExternalLoginProvider<Provider>();
        Assert.AreEqual(typeof(Provider), builder.ExternalLoginProviderType);
        Assert.IsNull(builder.AuthenticatorType);
        Assert.IsFalse(builder.Exports.Contains(typeof(IGXAmiExternalLoginProvider)));
        using var services = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        Provider first;
        using (var scope = services.CreateScope())
        {
            first = (Provider)scope.ServiceProvider.GetRequiredService(builder.ExternalLoginProviderType!);
            Assert.AreSame(first, scope.ServiceProvider.GetRequiredService<Provider>());
            Assert.AreSame(first.Dependency, scope.ServiceProvider.GetRequiredService<Dependency>());
            IGXAmiExternalLoginProvider contract = first;
            var definitions = await contract.GetProvidersAsync(CancellationToken.None);
            Assert.HasCount(2, definitions);
            Assert.AreEqual(AmiExternalLoginProtocol.OAuth2, definitions[0].Protocol);
            Assert.AreEqual(AmiExternalLoginProtocol.OpenIdConnect, definitions[1].Protocol);
            using var otherScope = services.CreateScope();
            var other = otherScope.ServiceProvider.GetRequiredService<Provider>();
            Assert.AreNotSame(first, other);
            Assert.AreNotSame(first.Dependency, other.Dependency);
            Assert.IsFalse(first.Disposed);
        }
        Assert.IsTrue(first.Disposed);
    }

    [TestMethod]
    public void ExistingScopedFactoryIsPreserved()
    {
        var builder = CreateBuilder();
        var instance = new Provider(new Dependency());
        builder.Services.AddScoped(_ => instance);
        var descriptor = builder.Services.Single();
        builder.AddExternalLoginProvider<Provider>();
        Assert.AreSame(descriptor, builder.Services.Single());
        using var services = builder.Services.BuildServiceProvider();
        using var scope = services.CreateScope();
        Assert.AreSame(instance, scope.ServiceProvider.GetRequiredService<Provider>());
    }

    [TestMethod]
    [DataRow(ServiceLifetime.Singleton)]
    [DataRow(ServiceLifetime.Transient)]
    public void WrongLifetimeIsRejectedWithoutPublishingTheProvider(ServiceLifetime lifetime)
    {
        var builder = CreateBuilder();
        builder.Services.Add(new ServiceDescriptor(typeof(Provider), typeof(Provider), lifetime));
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.AddExternalLoginProvider<Provider>());
        Assert.IsNull(builder.ExternalLoginProviderType);
        Assert.HasCount(1, builder.Services);
        builder.Services.Clear();
        builder.AddExternalLoginProvider<Provider>();
        Assert.AreEqual(typeof(Provider), builder.ExternalLoginProviderType);
    }

    [TestMethod]
    public void AbstractAndDuplicateProviderDeclarationsAreRejected()
    {
        var builder = CreateBuilder();
        Assert.ThrowsExactly<ArgumentException>(() => builder.AddExternalLoginProvider<AbstractProvider>());
        Assert.IsNull(builder.ExternalLoginProviderType);
        Assert.HasCount(0, builder.Services);
        builder.AddExternalLoginProvider<Provider>();
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.AddExternalLoginProvider<Provider>());
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.AddExternalLoginProvider<OtherProvider>());
        Assert.AreEqual(typeof(Provider), builder.ExternalLoginProviderType);
        Assert.IsFalse(builder.Services.Any(item => item.ServiceType == typeof(OtherProvider)));
    }

    [TestMethod]
    public void DefinitionDoesNotSerializeOrPrintItsClientSecret()
    {
        const string secret = "test-client-secret-do-not-publish";
        var definition = new AmiExternalLoginDefinition
        {
            Id = "oidc", DisplayName = "Company login", ClientId = "client-id", ClientSecret = secret,
            Authority = new Uri("https://identity.example/"), Scopes = ["openid", "profile"]
        };
        Assert.AreEqual(AmiExternalLoginProtocol.OpenIdConnect, definition.Protocol);
        var json = JsonSerializer.Serialize(definition);
        Assert.IsFalse(json.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(nameof(AmiExternalLoginDefinition.ClientSecret), StringComparison.Ordinal));
        Assert.IsFalse(definition.ToString()!.Contains(secret, StringComparison.Ordinal));
        var roundTrip = JsonSerializer.Deserialize<AmiExternalLoginDefinition>(json)!;
        Assert.AreEqual(definition.Id, roundTrip.Id);
        Assert.AreEqual(definition.Authority, roundTrip.Authority);
        Assert.HasCount(2, roundTrip.Scopes);
        Assert.IsNull(roundTrip.ClientSecret);
        Assert.IsNull(JsonSerializer.Deserialize<AmiExternalLoginDefinition>(
            "{\"Id\":\"oidc\",\"DisplayName\":\"Login\",\"ClientId\":\"client\",\"ClientSecret\":\"ignored\"}")!.ClientSecret);
    }

    private static AmiModuleBuilder CreateBuilder() => new("external-login-tests", new ConfigurationBuilder().Build(), new Snapshot());

    private sealed class Snapshot : IGXAmiModuleSettingsSnapshot
    {
        public string? Value => null;
        public T Read<T>() where T : new() => new();
    }

    public sealed class Dependency { }

    public sealed class Provider(Dependency dependency) : IGXAmiExternalLoginProvider, IDisposable
    {
        public Dependency Dependency { get; } = dependency;
        public bool Disposed { get; private set; }
        public Task<IReadOnlyList<AmiExternalLoginDefinition>> GetProvidersAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AmiExternalLoginDefinition>>([
                new AmiExternalLoginDefinition
                {
                    Id = "github", DisplayName = "GitHub", ClientId = "github-client", Protocol = AmiExternalLoginProtocol.OAuth2,
                    AuthorizationEndpoint = new Uri("https://github.com/login/oauth/authorize"),
                    TokenEndpoint = new Uri("https://github.com/login/oauth/access_token"),
                    UserInfoEndpoint = new Uri("https://api.github.com/user"), Scopes = ["read:user"]
                },
                new AmiExternalLoginDefinition
                {
                    Id = "company", DisplayName = "Company login", ClientId = "oidc-client",
                    Authority = new Uri("https://identity.example/"), Scopes = ["openid", "profile"]
                }
            ]);
        }
        public void Dispose()
        {
            Disposed = true;
        }
    }

    public abstract class AbstractProvider : IGXAmiExternalLoginProvider
    {
        public abstract Task<IReadOnlyList<AmiExternalLoginDefinition>> GetProvidersAsync(CancellationToken cancellationToken);
    }

    public sealed class OtherProvider : IGXAmiExternalLoginProvider
    {
        public Task<IReadOnlyList<AmiExternalLoginDefinition>> GetProvidersAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AmiExternalLoginDefinition>>([]);
    }
}
