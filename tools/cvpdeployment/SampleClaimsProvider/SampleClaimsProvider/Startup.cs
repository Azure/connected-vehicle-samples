using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(SampleClaimsProvider.Startup))]
namespace SampleClaimsProvider
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register the access token provider as a singleton
            string stsDiscoveryEndpoint = Environment.GetEnvironmentVariable("StsDiscoveryEndpoint");
            builder.Services.AddSingleton<ITokenValidator, McvpTokenValidator>(s => new McvpTokenValidator(stsDiscoveryEndpoint));
        }
    }
}
