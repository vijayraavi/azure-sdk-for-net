﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Core.Testing;
using Newtonsoft.Json.Linq;

namespace Azure.ApplicationModel.Configuration.Tests
{
    public class ConfigurationRecordMatcher : RecordMatcher
    {
        private readonly HashSet<string> _ignoredProperties = new HashSet<string>()
        {
            "last_modified",
            "etag"
        };

        public ConfigurationRecordMatcher(RecordedTestSanitizer sanitizer) : base(sanitizer)
        {
            ExcludeResponseHeaders.Add("Sync-Token");
        }

        protected override bool IsBodyEquivalent(RecordEntry record, RecordEntry otherRecord)
        {
            byte[] body = record.ResponseBody ?? Array.Empty<byte>();
            byte[] otherBody = record.ResponseBody ?? Array.Empty<byte>();

            if (body.SequenceEqual(otherBody)) return true;

            var bodyJson = JObject.Parse(Encoding.UTF8.GetString(body));
            var otherBodyJson = JObject.Parse(Encoding.UTF8.GetString(otherBody));

            Normalize(bodyJson);
            Normalize(otherBodyJson);

            return JToken.DeepEquals(bodyJson, otherBodyJson);
        }

        private void Normalize(JObject o)
        {
            foreach (var node in o.DescendantsAndSelf().ToList())
            {
                if (node is JProperty property)
                {
                    if (_ignoredProperties.Contains(property.Name) && property.Value is JValue)
                    {
                        property.Remove();
                    }
                }
            }

        }
    }
}
