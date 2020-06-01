using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using jaytwo.UrlHelper;

namespace jaytwo.FluentUri
{
    public static class UriExtensions
    {
        public static bool IsHttp(this Uri uri)
        {
            return uri?.Scheme == "http";
        }

        public static bool IsHttps(this Uri uri)
        {
            return uri?.Scheme == "https";
        }

        public static Uri WithHttp(this Uri uri)
        {
            return WithScheme(uri, "http");
        }

        public static Uri WithHttps(this Uri uri)
        {
            return WithScheme(uri, "https");
        }

        public static Uri WithScheme(this Uri uri, string scheme)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var builder = new UriBuilder(uri);
            builder.Scheme = scheme;

            if (uri.IsDefaultPort)
            {
                builder.Port = -1;
            }

            return builder.Uri;
        }

        public static Uri WithHost(this Uri uri, string host)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var builder = new UriBuilder(uri);
            builder.Host = host;

            return builder.Uri;
        }

        public static Uri WithPort(this Uri uri, int port)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var builder = new UriBuilder(uri);
            builder.Port = port;

            return builder.Uri;
        }

        public static Uri WithoutPort(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new Uri(uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped));
        }

        public static Uri WithPath(this Uri uri, string path)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(uri);

                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith("/"))
                    {
                        builder.Path = path;
                    }
                    else
                    {
                        builder.Path = builder.Path.TrimEnd('/') + "/" + path?.TrimStart('/');
                    }
                }

                return builder.Uri;
            }
            else
            {
                string resultUrl;
                if (path.StartsWith("/"))
                {
                    resultUrl = Url.SetPath(uri.OriginalString, path);
                }
                else
                {
                    resultUrl = Url.AppendPath(uri.OriginalString, path);
                }

                return new Uri(resultUrl, UriKind.Relative);
            }
        }

        public static Uri WithPath(this Uri uri, string pathFormat, params string[] formatArgs)
        {
            var escapedArgs = formatArgs?.Select(Uri.EscapeDataString).ToArray();
            var path = string.Format(pathFormat, escapedArgs);
            return WithPath(uri, path);
        }

        public static Uri WithPath(this Uri uri, string pathFormat, params object[] formatArgs)
        {
            var escapedArgs = formatArgs?.Select(x => Uri.EscapeDataString($"{x}")).ToArray();
            return WithPath(uri, pathFormat, escapedArgs);
        }

        public static Uri WithoutPath(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new Uri(uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Path, UriFormat.UriEscaped));
        }

        public static Uri WithoutPathAndQuery(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new Uri(uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.PathAndQuery, UriFormat.UriEscaped));
        }

        public static Uri WithQuery(this Uri uri, string query)
        {
            return WithQuery(uri, () => query);
        }

        public static Uri WithQuery(this Uri uri, object data)
        {
            return WithQuery(uri, () => QueryStringUtility.GetQueryString(data));
        }

        public static Uri WithQuery(this Uri uri, IDictionary<string, object> data)
        {
            return WithQuery(uri, () => QueryStringUtility.GetQueryString(data));
        }

        public static Uri WithQuery(this Uri uri, IDictionary<string, string[]> data)
        {
            return WithQuery(uri, () => QueryStringUtility.GetQueryString(data));
        }

        public static Uri WithQuery(this Uri uri, IDictionary<string, string> data)
        {
            return WithQuery(uri, () => QueryStringUtility.GetQueryString(data));
        }

#if NETFRAMEWORK || NETSTANDARD2
        public static Uri WithQuery(this Uri uri, NameValueCollection data)
        {
            var asDictionary = data.AllKeys.ToDictionary(x => x, x => data.GetValues(x));
            return WithQuery(uri, asDictionary);
        }
#endif

        public static Uri WithoutQuery(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.IsAbsoluteUri)
            {
                return new Uri(uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Query, UriFormat.UriEscaped));
            }
            else
            {
                return new Uri(Url.RemoveQuery(uri.OriginalString), UriKind.Relative);
            }
        }

        public static Uri WithQueryParameter(this Uri uri, string key, string value)
        {
            return WithQueryParameter(uri, key, new[] { value });
        }

        public static Uri WithQueryParameter(this Uri uri, string key, string[] values)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var data = QueryStringUtility.ParseQueryString(GetQuery(uri));

            if (data.ContainsKey(key))
            {
                var existingValues = data[key];
                var newValues = new List<string>(existingValues);
                newValues.AddRange(values);

                data[key] = newValues.ToArray();
            }
            else
            {
                data.Add(key, values);
            }

            return WithQuery(WithoutQuery(uri), data);
        }

        public static Uri WithQueryParameter(this Uri uri, string key, object value)
        {
            return WithQueryParameter(uri, key, $"{value}");
        }

        public static Uri WithoutQueryParameter(this Uri uri, string key)
        {
            var data = QueryStringUtility.ParseQueryString(GetQuery(uri));

            if (data.ContainsKey(key))
            {
                data.Remove(key);
            }

            return WithQuery(WithoutQuery(uri), data);
        }

        private static Uri WithQuery(this Uri uri, Func<string> getQueryStringDelegate)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var query = getQueryStringDelegate();

            if (uri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(uri);
                builder.Query = query;

                return builder.Uri;
            }
            else
            {
                return new Uri(Url.SetQuery(uri.OriginalString, query), UriKind.Relative);
            }
        }

        private static string GetQuery(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri.Query;
            }
            else
            {
                return Url.GetQuery(uri.OriginalString);
            }
        }
    }
}
