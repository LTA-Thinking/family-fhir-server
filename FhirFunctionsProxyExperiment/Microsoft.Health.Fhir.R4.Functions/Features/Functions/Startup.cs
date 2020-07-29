// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Fhir.Api.Controllers;
using Microsoft.Health.Fhir.Core.Registration;
using Newtonsoft.Json;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public virtual void ConfigureServices(IServiceCollection services)
    {
        IFhirServerBuilder fhirServerBuilder = services.AddFhirServer(Configuration);

        // .AddBackgroundWorkers()
        // .AddAzureExportDestinationClient()
        // .AddAzureExportClientInitializer(Configuration);

        string dataStore = Configuration["DataStore"];
        if (dataStore.Equals("CosmosDb", StringComparison.InvariantCultureIgnoreCase))
        {
            fhirServerBuilder.AddCosmosDb();
        }
        else if (dataStore.Equals("SqlServer", StringComparison.InvariantCultureIgnoreCase))
        {
            // fhirServerBuilder.AddSqlServer();
        }

        if (string.Equals(Configuration["ASPNETCORE_FORWARDEDHEADERS_ENABLED"], "true", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        services.AddMvc()
            .AddApplicationPart(typeof(FhirController).Assembly)
            .AddApplicationPart(typeof(AadSmartOnFhirProxyController).Assembly);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseMvc();
    }
}