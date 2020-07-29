// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Fhir.R4.Functions.Modules
{
    public class StartupExtension : IExtensionConfigProvider
    {
        private readonly IServiceProvider _provider;

        public StartupExtension(IServiceProvider provider)
        {
            _provider = provider;
            EnsureArg.IsNotNull(provider);
        }

        public void Initialize(ExtensionConfigContext context)
        {
            EnsureArg.IsNotNull(context);

            foreach (var startable in _provider.GetServices<IStartable>())
            {
                startable.Start();
            }

            var initTasks = _provider.GetServices<IRequireInitializationOnFirstRequest>()
                .Select(x => x.EnsureInitialized())
                .ToArray();

            Task.WhenAll(initTasks)
                .ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() => Trace.WriteLine("Functions initialization complete."));
        }
    }
}
