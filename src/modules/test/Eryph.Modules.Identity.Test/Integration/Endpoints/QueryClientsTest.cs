using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dbosoft.Hosuto.Modules.Testing;
using Dbosoft.IdentityServer;
using Dbosoft.IdentityServer.Storage.Models;
using Eryph.IdentityDb;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using FluentAssertions;
using Moq;
using Xunit;
using Client = Eryph.Modules.Identity.Models.V1.Client;

namespace Eryph.Modules.Identity.Test.Integration.Endpoints;

public class QueryClientsTest : IClassFixture<IdentityModuleNoAuthFactory>
{
    private readonly WebModuleFactory<IdentityModule> _factory;

    public QueryClientsTest(IdentityModuleNoAuthFactory factory)
    {
        _factory = factory;
    }
     
    private WebModuleFactory<IdentityModule> SetupClients(Action<Mock<IIdentityServerClientService>> configure)
    {
        var serviceMock = new Mock<IIdentityServerClientService>();
        configure(serviceMock);


        var factory = _factory.WithModuleConfiguration(options =>
        {
            options.ConfigureContainer(container =>
            {
                container.Options.AllowOverridingRegistrations = true;
                container.RegisterInstance(serviceMock.Object);
            });
        });

        return factory;
    }

    [Fact]
    public async Task Query_Clients()
    {
        var factory = SetupClients(mock =>
        {
            mock.Setup(x => x.QueryClients()).Returns(new[]
            {
                new Dbosoft.IdentityServer.Storage.Models.Client()
                {
                    ClientId = "test1",
                    ClientSecrets = new List<Secret>(new[]
                    {
                        new Secret
                        {
                            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                            Value = TestClientData.CertificateString
                        }
                    }),
                },
                new Dbosoft.IdentityServer.Storage.Models.Client()
                {
                    ClientId = "test2",
                    ClientSecrets = new List<Secret>(new[]
                    {
                        new Secret
                        {
                            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                            Value = TestClientData.CertificateString
                        }
                    }),
                }
            }.AsQueryable());

        });
        var result = await factory.CreateDefaultClient().GetFromJsonAsync<ListResponse<Client>>("v1/clients");
        result.Should().NotBeNull();
        result.Value.Count().Should().Be(2);
        result.Value.First().Id.Should().Be("test1");
        result.Value.Last().Id.Should().Be("test2");
    }


}