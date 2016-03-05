﻿//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Internal
{
    internal class ClientKey
    {
        public ClientKey(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            this.ClientId = clientId;
            this.HasCredential = false;
        }

        public ClientKey(string clientId, ClientCredential clientCredential, Authenticator authenticator):this(clientId)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException("clientCredential");
            }

            this.Authenticator = authenticator;
            this.Credential = clientCredential;
            this.HasCredential = true;
        }

        public ClientKey(string clientId, ClientAssertion clientAssertion):this(clientId)
        {
            if (clientAssertion == null)
            {
                throw new ArgumentNullException("clientAssertion");
            }

            this.Assertion = clientAssertion;
            this.HasCredential = true;
        }

        public ClientCredential Credential { get; private set; }

        public ClientAssertion Assertion { get; private set; }

        public Authenticator Authenticator { get; private set; }

        public string ClientId { get; private set; }

        public bool HasCredential { get; private set; }

        public void AddToParameters(IDictionary<string, string> parameters)
        {
            if (this.ClientId != null)
            {
                parameters[OAuthParameter.ClientId] = this.ClientId;
            }

            if (this.Credential != null)
            {
                if (!string.IsNullOrEmpty(this.Credential.Secret))
                {
                    parameters[OAuthParameter.ClientSecret] = this.Credential.Secret;
                }
                else
                {
                    JsonWebToken jwtToken = new JsonWebToken(this.ClientId, this.Authenticator.SelfSignedJwtAudience);
                    ClientAssertion clientAssertion = this.Credential.ClientAssertion;

                    if (this.Credential.ValidTo != 0)
                    {

                        bool assertionNearExpiry = (this.Credential.ValidTo <=
                                                    JsonWebToken.ConvertToTimeT(DateTime.UtcNow +
                                                                                TimeSpan.FromMinutes(
                                                                                    Constant.ExpirationMarginInMinutes)));
                        if (assertionNearExpiry)
                        {
                            clientAssertion = jwtToken.Sign(this.Credential.Certificate);
                            this.Credential.ValidTo = jwtToken.Payload.ValidTo;
                            this.Credential.ClientAssertion = clientAssertion;
                        }
                    }

                    parameters[OAuthParameter.ClientAssertionType] = clientAssertion.AssertionType;
                    parameters[OAuthParameter.ClientAssertion] = clientAssertion.Assertion;
                }
            }

            else if (this.Assertion != null)
            {
                parameters[OAuthParameter.ClientAssertionType] = this.Assertion.AssertionType;
                parameters[OAuthParameter.ClientAssertion] = this.Assertion.Assertion;
            }
            
        }
    }
}