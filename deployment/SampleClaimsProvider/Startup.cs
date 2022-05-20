// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Net;
    using System.Security;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Versioning;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddApiVersioning(
                o =>
                {
                    o.ReportApiVersions = true;
                    o.DefaultApiVersion = Microsoft.AspNetCore.Mvc.ApiVersion.Parse(ClaimsProviderApiVersions.V20210519);
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.ApiVersionReader = new QueryStringApiVersionReader();
                });

            CosmosDBConnectionString connString = new CosmosDBConnectionString(this.Configuration["ConnectionStrings:CosmosDBConnectionString"]);
            Uri serviceEndpoint = connString.ServiceEndpoint;
            SecureString authKey = new NetworkCredential("", connString.AuthKey).SecurePassword;
            services.AddSingleton<DocumentClient>(s => new DocumentClient(serviceEndpoint, authKey));
            services.AddSingleton<IClaimsProviderStore, ClaimsProviderStore>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
