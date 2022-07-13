using System;
using System.Net.Http;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.SampleApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ClearBank.DeveloperTest.Tests.Shared
{
    public sealed class SystemUnderTest : IDisposable
    {
        public HttpClient Client { get; }

        public Mock<IAccountDataStore> AccountDataStoreMock { get; }
        
        public SystemUnderTest()
        {
            AccountDataStoreMock = new Mock<IAccountDataStore>();
            var server = new TestServer(AccountDataStoreMock.Object);
            Client = server.CreateClient();
        }

        public void Dispose()
        {
            Client?.Dispose();
        }

        private class TestServer : WebApplicationFactory<Startup>
        {
            private readonly IAccountDataStore _accountDataStore;

            public TestServer(IAccountDataStore accountDataStore)
            {
                _accountDataStore = accountDataStore;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureTestServices(sc =>
                {
                    sc.AddSingleton(_accountDataStore);
                });
            }
        }
    }
}