// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Health.Fhir.Core.Messages.Get;

namespace Microsoft.Health.Fhir.R4.Functions.Features.Functions
{
    public class FhirFunctions
    {
        private readonly IMediator _mediator;

        public FhirFunctions(IMediator mediator)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            _mediator = mediator;
        }

        [FunctionName("metadata")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Functions function")]
        public async Task<IActionResult> Metadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req)
        {
            EnsureArg.IsNotNull(req);

            var response = await _mediator.Send(new GetCapabilitiesRequest());

            return FunctionsFhirResult.Create(response.CapabilityStatement);
        }
    }
}