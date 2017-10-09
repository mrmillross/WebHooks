﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// An <see cref="IWebHookMetadata"/> service containing metadata about the Bitbucket receiver.
    /// </summary>
    public class BitbucketMetadata :
        WebHookMetadata,
        IWebHookEventMetadata,
        IWebHookRequestMetadataService,
        IWebHookSecurityMetadata
    {
        /// <summary>
        /// Instantiates a new <see cref="BitbucketMetadata"/> instance.
        /// </summary>
        public BitbucketMetadata()
            : base(BitbucketConstants.ReceiverName)
        {
        }

        // IWebHookEventMetadata...

        /// <inheritdoc />
        public string ConstantValue => null;

        /// <inheritdoc />
        public string HeaderName => BitbucketConstants.EventHeaderName;

        /// <inheritdoc />
        public string PingEventName => null;

        /// <inheritdoc />
        public string QueryParameterKey => null;

        // IWebHookRequestMetadataService...

        /// <inheritdoc />
        public WebHookBodyType BodyType => WebHookBodyType.Json;

        // IWebHookSecurityMetadata...

        /// <inheritdoc />
        public bool VerifyCodeParameter => true;
    }
}
