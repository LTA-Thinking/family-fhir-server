// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Api.Features.Context;
using Microsoft.Health.Fhir.R4.Functions.Features.Functions;

public static class FhirFunctionsProxy
{
    private static ServiceCollection services;
    private static ServiceProvider serviceProvider;
    private static ApplicationBuilder appBuilder;
    private static Startup startup;
    private static RequestDelegate requestHandler;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    private static void Setup()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        var functionPath = Path.Combine(new FileInfo(typeof(FhirFunctionsProxy).Assembly.Location).Directory.FullName, "..");

        var configRoot = new ConfigurationBuilder()
            .SetBasePath(functionPath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        IFileProvider fileProvider = new PhysicalFileProvider(functionPath);

        var hostingEnvironment = new MyHostingEnvironment()
        {
            ContentRootPath = functionPath,
            WebRootPath = functionPath,
            ContentRootFileProvider = fileProvider,
            WebRootFileProvider = fileProvider,
        };

        var conttents = hostingEnvironment.ContentRootFileProvider.GetDirectoryContents(".");

        hostingEnvironment.WebRootFileProvider = hostingEnvironment.ContentRootFileProvider;

        /* Add required services into DI container */
        services = new ServiceCollection();
        var listener = new DiagnosticListener("Microsoft.AspNetCore");
        services.AddSingleton<DiagnosticSource>(listener);

        // services.AddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());
        // services.AddSingleton<IHostEnvironment>(hostingEnvironment);
        services.AddSingleton<IWebHostEnvironment>(hostingEnvironment);
        services.AddSingleton<IConfiguration>(configRoot);
        services.AddSingleton(listener);

        // services.AddSingleton<IConfiguration>(configRoot);

        /* Instantiate standard ASP.NET Core Startup class */
        startup = new Startup(configRoot);

        /* Add web app services into DI container */
        startup.ConfigureServices(services);

        /* Initialize DI container */
        serviceProvider = services.BuildServiceProvider();

        /* Initialize Application builder */
        appBuilder = new ApplicationBuilder(serviceProvider, new FeatureCollection());

        // appBuilder.UsePathBase(new PathString("/api"));

        appBuilder.UseRouting();

        appBuilder.Use(async (x, y) =>
        {
            try
            {
                var replace = x.Request.Path.ToString().Replace("/api", string.Empty, StringComparison.OrdinalIgnoreCase);
                x.Request.Path = new PathString(replace);
                await y();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        });

        appBuilder.UseFhirRequestContext();

        /* Configure the HTTP request pipeline */
        startup.Configure(appBuilder);

        /* Build request handling function */
        requestHandler = appBuilder.Build();

        foreach (var startable in serviceProvider.GetServices<IStartable>())
        {
            startable.Start();
        }

        foreach (var initializable in serviceProvider.GetServices<IRequireInitializationOnFirstRequest>())
        {
            initializable.EnsureInitialized().GetAwaiter().GetResult();
        }
    }

    [FunctionName("WebProxy")]
    public static async Task Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*any}")] HttpRequest req,
        ILogger log)
    {
        try
        {
            if (requestHandler == null)
            {
                Setup();
            }

            log.LogInformation("Handling FHIR Request.");

            /* Set DI container for HTTP Context */
            req.HttpContext.RequestServices = serviceProvider;

            /* Handle HTTP request */
            await requestHandler(req.HttpContext);
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
        }
    }
}