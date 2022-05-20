// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    //This class represents a claim in a simplified format
    public class StringClaim
    {
        [JsonConstructor]
        public StringClaim(string name, List<ClaimValue> values)
        {
            this.Name = name;
            this.Values = values;
        }

        public StringClaim(string name, string value)
        {
            this.Name = name;
            this.Values = new List<ClaimValue>();
            this.Values.Add(new ClaimValue(value));
        }

        /// <summary>
        ///     The Name of the claim (sometimes referred to as Type)
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     The Value of the claim represented by Name (multi-valued claims supported for compactness)
        /// </summary>
        [JsonProperty("values")]
        public List<ClaimValue> Values { get; set; }
    }

    public class ClaimValue
    {
        [JsonConstructor]
        public ClaimValue(string value)
        {
            this.Value = value;
        }

        /// <summary>
        ///     The value of the claim
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}