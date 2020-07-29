// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Fhir.R4.Functions.Features.IoC
{
    public sealed class FunctionsScoped<T> : IScoped<T>
    {
        private IServiceScope _scope;

        public FunctionsScoped(IServiceProvider provider)
        {
            EnsureArg.IsNotNull(provider, nameof(provider));

            _scope = provider.CreateScope();
            Value = _scope.ServiceProvider.GetService<T>();
        }

        public T Value { get; }

        public void Dispose()
        {
            try
            {
                _scope?.Dispose();
                _scope = null;
            }
            catch (Exception)
            {
                // TODO: Why is this being called multiple times?
            }
        }
    }
}