// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Api.Configs;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Api.Modules;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Health.Fhir.Core.Registration;
using Microsoft.Health.Fhir.R4.Functions.Features.IoC;
using Microsoft.Health.Fhir.R4.Functions.Features.Routing;
using Microsoft.Health.Fhir.R4.Functions.Modules;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Microsoft.Health.Fhir.R4.Functions.Modules
{
    public class Startup : FunctionsStartup
    {
        private const string FhirServerConfigurationSectionName = "FhirServer";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            /* Hack to get IConfiguration */
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configurationRoot = serviceProvider.GetService<IConfiguration>();
            var env = serviceProvider.GetRequiredService<IHostEnvironment>();

            var appDirectory = serviceProvider.GetRequiredService<IOptions<ExecutionContextOptions>>().Value.AppDirectory;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(appDirectory)
                .AddJsonFile("appsettings.json");

            var customConfig = configBuilder.AddConfiguration(configurationRoot).Build();

            services.AddOptions();
            services.Add<IConfiguration>(x => customConfig).Singleton().ReplaceService<IConfiguration>();

            var fhirServerConfiguration = new FhirServerConfiguration();

            customConfig?.GetSection(FhirServerConfigurationSectionName).Bind(fhirServerConfiguration);

            services.AddSingleton(Options.Create(fhirServerConfiguration));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Security));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Features));
            services.AddSingleton(Options.Create(fhirServerConfiguration.CoreFeatures));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Cors));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Operations));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Operations.Export));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Operations.Reindex));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Audit));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Bundle));
            services.AddSingleton(Options.Create(fhirServerConfiguration.Throttling));

            new FhirModule(fhirServerConfiguration).Load(services);
            services.RemoveAll(typeof(IScoped<>));
            services.AddTransient(typeof(IScoped<>), typeof(FunctionsScoped<>));

            new MediationModule().Load(services);
            new PersistenceModule().Load(services);
            new SearchModule(fhirServerConfiguration).Load(services);
            new ValidationModule().Load(services);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Add<QueryStringParser>().Singleton().AsService<IQueryStringParser>();
            services.Add<FunctionsUrlResolver>().Singleton().ReplaceService<IUrlResolver>();

            services.AddHttpClient();

            services.Add<StartupExtension>().Transient().AsImplementedInterfaces();

            string dataStore = customConfig["DataStore"];
            if (dataStore.Equals("CosmosDb", StringComparison.InvariantCultureIgnoreCase))
            {
                var fhirBuilder = new FunctionsFhirServerBuilder(services);
                fhirBuilder.AddCosmosDbCore();
            }
        }

        private class FunctionsFhirServerBuilder : IFhirServerBuilder
        {
            public FunctionsFhirServerBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));

                Services = services;
            }

            public IServiceCollection Services { get; }

            public FunctionsFhirServerBuilder AddCosmosDbCore()
            {
                var registrations = typeof(FhirServerBuilderCosmosDbRegistrationExtensions);

                var cosmosCore = registrations.GetMethod("AddCosmosDbPersistence", BindingFlags.Static | BindingFlags.NonPublic);
                var cosmosSearch = registrations.GetMethod("AddCosmosDbSearch", BindingFlags.Static | BindingFlags.NonPublic);

                cosmosCore.Invoke(null, new object[] { this, null });
                cosmosSearch.Invoke(null, new object[] { this });

                return this;
            }
        }
    }
}