// <copyright file="DefaultLineProtocolClient.cs" company="App Metrics Contributors">
// Copyright (c) App Metrics Contributors. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Logging;

namespace App.Metrics.Reporting.InfluxDb2.Client
{
    public class DefaultLineProtocolClient : ILineProtocolClient
    {
        private static readonly ILog Logger = LogProvider.For<DefaultLineProtocolClient>();

        private const long MIN_GZIP_LENGTH = 1024;

        private static long _backOffTicks;
        private static long _failureAttempts;
        private static long _failuresBeforeBackoff;
        private static TimeSpan _backOffPeriod;

        private readonly HttpClient _httpClient;
        private readonly InfluxDbOptions _influxDbOptions;

        public DefaultLineProtocolClient(
            InfluxDbOptions influxDbOptions,
            HttpPolicy httpPolicy,
            HttpClient httpClient)
        {
            _influxDbOptions = influxDbOptions ?? throw new ArgumentNullException(nameof(influxDbOptions));
            _httpClient = httpClient;
            _backOffPeriod = httpPolicy?.BackoffPeriod ?? throw new ArgumentNullException(nameof(httpPolicy));
            _failuresBeforeBackoff = httpPolicy.FailuresBeforeBackoff;
            _failureAttempts = 0;
        }

        public async Task<LineProtocolWriteResult> WriteAsync(
            Stream payload,
            CancellationToken cancellationToken = default
        ) {
            if (payload == null) {
                return new LineProtocolWriteResult(true);
            }

            bool useGzip = _influxDbOptions.EnableGzip;
            if (payload.CanSeek) {
                try {
                    var length = payload.Length - payload.Position;
                    if (length == 0) {
                        return new LineProtocolWriteResult(true);
                    }
                    if (useGzip && length < MIN_GZIP_LENGTH) {
                        useGzip = false;
                    }
                }
                catch (NotSupportedException) { }
            }

            if (NeedToBackoff()) {
                return new LineProtocolWriteResult(false, "Too many failures in writing to InfluxDB, Circuit Opened");
            }

            try {
                HttpContent content = new StreamContent(payload);
                if (useGzip) {
                    content = new GzipContent(content);
                }

                var response = await _httpClient.PostAsync(_influxDbOptions.Endpoint, content, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound && _influxDbOptions.CreateBucketIfNotExists) {
                    await TryCreateDatabase(cancellationToken);
                    response = await _httpClient.PostAsync(_influxDbOptions.Endpoint, content, cancellationToken);
                }

                if (!response.IsSuccessStatusCode) {
                    Interlocked.Increment(ref _failureAttempts);

                    var errorMessage = $"Failed to write to InfluxDB - Status: {(int)response.StatusCode} ({response.ReasonPhrase})";
                    Logger.Error(errorMessage);
                    //Logger.Error(await response.Content.ReadAsStringAsync());

                    return new LineProtocolWriteResult(false, errorMessage);
                }

                Interlocked.Exchange(ref _failureAttempts, 0);

                Logger.Trace("Successful write to InfluxDB");

                return new LineProtocolWriteResult(true);
            }
            catch (Exception ex) {
                Interlocked.Increment(ref _failureAttempts);
                Logger.Error(ex, "Failed to write to InfluxDB");
                return new LineProtocolWriteResult(false, ex.ToString());
            }
        }

        private async Task<LineProtocolWriteResult> TryCreateDatabase(CancellationToken cancellationToken = default)
        {
            try {
                Logger.Trace($"Attempting to create InfluxDB Bucket '{_influxDbOptions.Bucket}'");

                var lines = new List<string> {
                    $"\"name\":\"{_influxDbOptions.Bucket}\"",
                    $"\"orgID\":\"{_influxDbOptions.OrganizationId}\""
                };
                if (!string.IsNullOrEmpty(_influxDbOptions.BucketDescription)) {
                    lines.Add($"\"description\":\"{_influxDbOptions.BucketDescription}\"");
                }
                if (_influxDbOptions.BucketRetentionDuration.HasValue) {
                    lines.Add(
                        $"\"retentionRules\":[{{\"everySeconds\":{(long)_influxDbOptions.BucketRetentionDuration.Value.TotalSeconds}, \"type\":\"expire\"}}]"
                    );
                }

                var content = new StringContent($"{{{string.Join(',', lines)}}}", Encoding.UTF8);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);

                var response = await _httpClient.PostAsync("buckets", content, cancellationToken);

                if (!response.IsSuccessStatusCode) {
                    string responseString = null;
                    try {
                        responseString = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception) {
                        // ignore;
                    }

                    var errorMessage = 
                            $"Failed to create InfluxDB Bucket '{_influxDbOptions.Bucket}' - Status: {(int)response.StatusCode} ({response.ReasonPhrase})";
                    
                    if (!string.IsNullOrEmpty(responseString)) {
                        errorMessage += $" Response: {responseString}";
                    }

                    Logger.Error(errorMessage);

                    return new LineProtocolWriteResult(false, errorMessage);
                }

                Logger.Trace($"Successfully created InfluxDB Bucket '{_influxDbOptions.Bucket}'");

                return new LineProtocolWriteResult(true);
            }
            catch (Exception ex) {
                Logger.Error(ex, $"Failed to create InfluxDB Bucket '{_influxDbOptions.Bucket}'");
                return new LineProtocolWriteResult(false, ex.ToString());
            }
        }

        private bool NeedToBackoff()
        {
            if (Interlocked.Read(ref _failureAttempts) < _failuresBeforeBackoff) {
                return false;
            }

            Logger.Error($"InfluxDB write backoff for {_backOffPeriod.Seconds} secs");

            if (Interlocked.Read(ref _backOffTicks) == 0) {
                Interlocked.Exchange(ref _backOffTicks, DateTime.UtcNow.Add(_backOffPeriod).Ticks);
            }

            if (DateTime.UtcNow.Ticks <= Interlocked.Read(ref _backOffTicks)) {
                return true;
            }

            Interlocked.Exchange(ref _failureAttempts, 0);
            Interlocked.Exchange(ref _backOffTicks, 0);

            return false;
        }
    }
}
