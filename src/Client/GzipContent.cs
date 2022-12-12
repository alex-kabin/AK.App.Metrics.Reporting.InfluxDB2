using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace App.Metrics.Reporting.InfluxDb2.Client
{
    public class GzipContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private readonly CompressionLevel _compressionLevel;

        public GzipContent(HttpContent content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            _originalContent = content ?? throw new ArgumentNullException(nameof(content));
            _compressionLevel = compressionLevel;

            foreach (KeyValuePair<string, IEnumerable<string>> header in _originalContent.Headers) {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Headers.ContentEncoding.Add("gzip");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
        
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var compressedStream = new GZipStream(stream, _compressionLevel, leaveOpen: true);
            await _originalContent.CopyToAsync(compressedStream);
        }
    }
}
