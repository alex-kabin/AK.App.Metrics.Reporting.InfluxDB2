// <copyright file="InfluxDBOptions.cs" company="App Metrics Contributors">
// Copyright (c) App Metrics Contributors. All rights reserved.
// </copyright>

using System;

namespace App.Metrics.Reporting.InfluxDb2
{
    /// <summary>
    ///     Provides programmatic configuration for InfluxDB in the App Metrics framework.
    /// </summary>
    public class InfluxDbOptions
    {
        public InfluxDbOptions() { }

        /// <summary>
        ///     Gets formatted endpoint for writes to InfluxDB
        /// </summary>
        /// <value>
        ///     The InfluxDB endpoint for writes.
        /// </value>
        public string Endpoint
        {
            get {
                var endpoint = "write";
                endpoint += $"?bucket={Uri.EscapeDataString(Bucket)}";

                if (!string.IsNullOrEmpty(OrganizationId)) {
                    endpoint += $"&orgID={Uri.EscapeDataString(OrganizationId)}";
                } else if (!string.IsNullOrEmpty(Organization)) {
                    endpoint += $"&org={Uri.EscapeDataString(Organization)}";
                }

                //endpoint += $"&precision=ns";

                return endpoint;
            }
        }

        /// <summary>
        ///     Gets or sets the base URI of the InfluxDB API.
        /// </summary>
        /// <value>
        ///     The base URI of the InfluxDB API where metrics are flushed.
        /// </value>
        public Uri BaseUri { get; set; }

        /// <summary>
        ///     Gets or sets the InfluxDB bucket name used to report metrics.
        /// </summary>
        /// <value>
        ///     The InfluxDB bucket name where metrics are flushed.
        /// </value>
        public string Bucket { get; set; }

        /// <summary>
        ///     Gets or sets the InfluxDB Organization Id.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        ///     Gets or sets the InfluxDB Organization.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        ///     Gets or sets the InfluxDB user token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        ///     Enables or disables gzip compression
        /// </summary>
        public bool EnableGzip { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not to attempt to create the specified bucket if it does not exist
        /// </summary>
        /// <value>
        ///  The flag indicating whether or not to create the specified bucket if it does not exist
        /// </value>
        public bool CreateBucketIfNotExists { get; set; }

        /// <summary>
        /// Bucket description (optional)
        /// </summary>
        public string BucketDescription { get; set; }

        /// <summary>
        /// Duration for how long data will be kept in the bucket. Zero means infinite.
        /// </summary>
        public TimeSpan? BucketRetentionDuration { get; set; } = TimeSpan.Zero;
    }
}