// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) {Microsoft} Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace SampleClaimsProvider
{
    using System;
    using System.Data.Common;

    internal class CosmosDBConnectionString
    {
        public CosmosDBConnectionString(string connectionString)
        {
            // Use this generic builder to parse the connection string
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("AccountKey", out object key))
            {
                this.AuthKey = key.ToString();
            }

            if (builder.TryGetValue("AccountEndpoint", out object uri))
            {
                this.ServiceEndpoint = new Uri(uri.ToString());
            }
        }

        public Uri ServiceEndpoint { get; set; }

        public string AuthKey { get; set; }
    }
}
