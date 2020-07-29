// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.R4.Functions.Features.Routing
{
    public class FunctionsUrlResolver : IUrlResolver
    {
        public Uri ResolveMetadataUrl(bool includeSystemQueryString)
        {
            throw new NotImplementedException();
        }

        public Uri ResolveResourceUrl(ResourceElement resource, bool includeVersion = false)
        {
            throw new NotImplementedException();
        }

        public Uri ResolveRouteUrl(IEnumerable<Tuple<string, string>> unsupportedSearchParams = null, IReadOnlyList<(string parameterName, string reason)> unsupportedSortingParameters = null,  string continuationToken = null, bool removeTotalParameter = false)
        {
            throw new NotImplementedException();
        }

        public Uri ResolveRouteNameUrl(string routeName, IDictionary<string, object> routeValues)
        {
            throw new NotImplementedException();
        }

        public Uri ResolveOperationResultUrl(string operationName, string id)
        {
            throw new NotImplementedException();
        }
    }
}