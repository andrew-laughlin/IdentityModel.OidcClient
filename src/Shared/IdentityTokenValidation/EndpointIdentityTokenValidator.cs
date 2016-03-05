﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IdentityModel.OidcClient
{
    public class EndpointIdentityTokenValidator : IIdentityTokenValidator
    {
        private readonly string _clientId;
        private readonly string _endpoint;

        public EndpointIdentityTokenValidator(string authority, string clientId)
        {
            _endpoint = authority.EnsureTrailingSlash() + "connect/identitytokenvalidation";
            _clientId = clientId;
        }

        public async Task<IdentityTokenValidationResult> ValidateAsync(string identityToken)
        {
            var client = new HttpClient();

            var form = new Dictionary<string, string>
            {
                { "token", identityToken },
                { "client_id", _clientId }
            };

            var response = await client.PostAsync(
                new Uri(_endpoint),
                new FormUrlEncodedContent(form));

            if (!response.IsSuccessStatusCode)
            {
                return new IdentityTokenValidationResult
                {
                    Success = false,
                    Error = response.ReasonPhrase
                };
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            var claims = new Claims();

            foreach (var x in json)
            {
                var array = x.Value as JArray;

                if (array != null)
                {
                    foreach (var item in array)
                    {
                        claims.Add(new Claim(x.Key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(x.Key, x.Value.ToString()));
                }
            }

            return new IdentityTokenValidationResult
            {
                Success = true,
                Claims = claims,
                SignatureAlgorithm = "RS256"
            };
        }
    }
}