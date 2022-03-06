using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Xylab.Management.VirtualFileSystem
{
    internal static class UriHelper
    {
        private const string DisguisedHostHeaderName = "DISGUISED-HOST";

        public static bool UseDisguisedHost { get; set; }

        public static Uri GetBaseUri(HttpRequest request)
        {
            return new Uri(GetRequestUri(request).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));
        }

        public static Uri GetRequestUri(HttpRequestMessage request)
        {
            string disguisedHost = null;

            if (request.Headers.TryGetValues(DisguisedHostHeaderName, out IEnumerable<string> disguisedHostValues))
            {
                disguisedHost = disguisedHostValues.FirstOrDefault();
            }

            return GetRequestUriInternal(request.RequestUri, disguisedHost);
        }

        public static Uri GetRequestUri(HttpRequest request)
        {
            return GetRequestUriInternal(new Uri(request.GetDisplayUrl()), request.Headers[DisguisedHostHeaderName]);
        }

        private static Uri GetRequestUriInternal(Uri uri, string disguisedHostValue)
        {
            // On Linux, corrections to the request URI are needed due to the way the request is handled on the worker:
            // - Set scheme to https
            // - Set host to the value of DISGUISED-HOST
            // - Remove port value
            if (UseDisguisedHost && disguisedHostValue != null)
            {
                uri = new UriBuilder(uri) { Scheme = "https", Host = disguisedHostValue, Port = -1 }.Uri;
            }

            return uri;
        }

        public static Uri MakeRelative(Uri baseUri, string relativeUri)
        {
            // We don't care about the query string
            UriBuilder builder = new(baseUri) { Query = null };
            if (builder.Port == 80) builder.Port = -1;
            baseUri = new Uri(EnsureTrailingSlash(builder.ToString()));
            return new Uri(baseUri, relativeUri);
        }

        internal static string EnsureTrailingSlash(string url)
        {
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }
    }
}
