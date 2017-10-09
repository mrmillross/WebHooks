﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// <para>
    /// An <see cref="IResourceFitler"/> to verify required HTTP headers and query parameters are present in a WebHook
    /// request. Uses <see cref="IWebHookBindingMetadata"/> services to determine the requirements for the requested
    /// WebHook receiver.
    /// </para>
    /// <para>
    /// Short-circuits the request if required values are missing. The response in that case will have a 400
    /// "Bad Request" status code.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <see cref="WebHookVerifyCodeFilter"/>, <see cref="Routing.WebHookEventMapperConstraint"/> and
    /// <see cref="Routing.WebHookEventMapperConstraint"/> subclasses also verify required HTTP headers and query
    /// parameters. But, none of those constraints and filters use <see cref="IWebHookBindingMetadata"/> information.
    /// </remarks>
    public class WebHookVerifyRequiredValueFilter : IResourceFilter
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyList<IWebHookBindingMetadata> _bindingMetadata;

        public WebHookVerifyRequiredValueFilter(ILoggerFactory loggerFactory, IEnumerable<IWebHookMetadata> metadata)
        {
            _logger = loggerFactory.CreateLogger<WebHookVerifyRequiredValueFilter>();
            _bindingMetadata = new List<IWebHookBindingMetadata>(metadata.OfType<IWebHookBindingMetadata>());
        }

        /// <summary>
        /// Gets the <see cref="IOrderedFilter.Order"/> recommended for all
        /// <see cref="WebHookVerifyRequiredValueFilter"/> instances. The recommended filter sequence is
        /// <list type="number">
        /// <item><description>
        /// Confirm signature or <c>code</c> query parameter (in a <see cref="WebHookSecurityFilter"/> subclass).
        /// </description></item>
        /// <item><description>
        /// Confirm required headers and query parameters are provided (in this filter).
        /// </description></item>
        /// <item><description>Short-circuit GET or HEAD requests, if receiver supports either.</description></item>
        /// <item><description>
        /// Confirm it's a POST request (in <see cref="WebHookVerifyMethodFilter"/>).
        /// </description></item>
        /// <item><description>Confirm body type (in <see cref="WebHookVerifyBodyTypeFilter"/>).</description></item>
        /// <item><description>
        /// Short-circuit ping requests, if not done in #3 for this receiver (in
        /// <see cref="WebHookPingResponseFilter"/>).
        /// </description></item>
        /// </list>
        /// </summary>
        public static int Order => WebHookVerifySignatureFilter.Order + 10;

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.RouteData.TryGetReceiverName(out var receiverName))
            {
                // Not a WebHook request.
                return;
            }

            var bindingMetadata = _bindingMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
            if (bindingMetadata == null)
            {
                // Receiver has no additional parameters.
                return;
            }

            var request = context.HttpContext.Request;
            var headers = request.Headers;
            var query = request.Query;
            for (var i = 0; i < bindingMetadata.Parameters.Count; i++)
            {
                var parameter = bindingMetadata.Parameters[i];
                if (parameter.IsRequired)
                {
                    var sourceName = parameter.SourceName;
                    var found = parameter.IsQueryParameter ?
                        VerifyQueryParameter(query, sourceName, receiverName, out var message) :
                        VerifyHeader(headers, sourceName, receiverName, out message);
                    if (!found)
                    {
                        context.Result = WebHookResultUtilities.CreateErrorResult(message);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        private bool VerifyHeader(
            IHeaderDictionary headers,
            string headerName,
            string receiverName,
            out string message)
        {
            if (headers.TryGetValue(headerName, out var values) && !StringValues.IsNullOrEmpty(values))
            {
                message = null;
                return true;
            }

            _logger.LogError(
                500,
                "A {ReceiverName} WebHook request must contain a '{HeaderName}' HTTP header.",
                receiverName,
                headerName);
            message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifyRequiredValue_NoHeader,
                receiverName,
                headerName);

            return false;
        }

        private bool VerifyQueryParameter(
            IQueryCollection query,
            string parameterName,
            string receiverName,
            out string message)
        {
            if (query.TryGetValue(parameterName, out var values) && !StringValues.IsNullOrEmpty(values))
            {
                message = null;
                return true;
            }

            _logger.LogError(
                501,
                "A {ReceiverName} WebHook request must contain a '{QueryParameterKey}' query parameter.",
                receiverName,
                parameterName);
            message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifyRequiredValue_NoQueryParameter,
                receiverName,
                parameterName);

            return false;
        }
    }
}
