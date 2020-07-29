// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using EnsureThat;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Fhir.Api.Features.ActionResults;
using Microsoft.Health.Fhir.Api.Features.Formatters;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.R4.Functions.Features.Functions
{
    public class FunctionsFhirResult : FhirResult
    {
        private static readonly FhirJsonSerializer _serializer = new FhirJsonSerializer(SerializerSettings.CreateDefault());

        public FunctionsFhirResult()
        {
        }

        public FunctionsFhirResult(ResourceElement resource)
            : base(resource)
        {
        }

        public static new FunctionsFhirResult Create(ResourceElement resource, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            FunctionsFhirResult fhirResult = new FunctionsFhirResult(resource);
            fhirResult.StatusCode = statusCode;
            return fhirResult;
        }

        public static new FunctionsFhirResult Gone()
        {
            FunctionsFhirResult fhirResult = new FunctionsFhirResult();
            fhirResult.StatusCode = HttpStatusCode.Gone;
            return fhirResult;
        }

        public static new FunctionsFhirResult NotFound()
        {
            FunctionsFhirResult fhirResult = new FunctionsFhirResult();
            fhirResult.StatusCode = HttpStatusCode.NotFound;
            return fhirResult;
        }

        public static new FunctionsFhirResult NoContent()
        {
            FunctionsFhirResult fhirResult = new FunctionsFhirResult();
            fhirResult.StatusCode = HttpStatusCode.NoContent;
            return fhirResult;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            context.HttpContext.AllowSynchronousIO();

            HttpResponse response = context.HttpContext.Response;
            if (StatusCode.HasValue)
            {
                response.StatusCode = (int)StatusCode.Value;
            }

            /* Headers is not accessible :( we should make this "protected internal" or public */
            var headersProp = GetType().GetProperty("Headers", BindingFlags.Instance | BindingFlags.NonPublic);
            var headersValue = (IHeaderDictionary)headersProp.GetValue(this);

            foreach (KeyValuePair<string, StringValues> header in headersValue)
            {
                response.Headers.Add(header);
            }

            if (Result != null)
            {
                using var writer = new StreamWriter(context.HttpContext.Response.Body, leaveOpen: true);
                using var jsonWriter = new JsonTextWriter(writer);

                if (context.HttpContext.GetIsPretty())
                {
                    jsonWriter.Formatting = Formatting.Indented;
                }

                _serializer.Serialize(Result.ToPoco(), jsonWriter, context.HttpContext.GetSummaryType(NullLogger.Instance));

                await jsonWriter.FlushAsync();
            }
            else
            {
                await new EmptyResult().ExecuteResultAsync(context);
            }
        }
    }
}