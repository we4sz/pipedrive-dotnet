using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Pipedrive.Helpers;

namespace Pipedrive.Internal
{
    public class ReadOnlyPagedCollection<T> : ReadOnlyCollection<T>, IReadOnlyPagedCollection<T>
    {
        readonly AdditionalData _additionalData;
        private readonly Uri _uri;
        private readonly ApiOptions _options;
        readonly Func<Uri, Task<IApiResponse<JsonResponse<List<T>>>>> _nextPageFunc;

        public ReadOnlyPagedCollection(Uri uri, ApiOptions options, IApiResponse<JsonResponse<List<T>>> response, Func<Uri, Task<IApiResponse<JsonResponse<List<T>>>>> nextPageFunc)
            : base(response != null ? response.Body?.Data ?? new List<T>() : new List<T>())
        {
            Ensure.ArgumentNotNull(response, nameof(response));
            Ensure.ArgumentNotNull(nextPageFunc, nameof(nextPageFunc));

            _uri = uri;
            _options = options;
            _nextPageFunc = nextPageFunc;
            if (response != null)
            {
                _additionalData = response.Body?.AdditionalData;
            }
        }

        public async Task<IReadOnlyPagedCollection<T>> GetNextPage()
        {
            if (_additionalData?.Pagination?.NextStart == null) return null;

            var nextUri = _uri + "?" + string.Join("&",
                new Dictionary<string, string>()
                {
                    { "start", _additionalData.Pagination.NextStart.ToString() },
                    { "limit", (_options.PageCount ?? 100).ToString() }
                }.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            var maybeTask = _nextPageFunc(new Uri(nextUri, UriKind.Relative));

            if (maybeTask == null)
            {
                return null;
            }

            var response = await maybeTask.ConfigureAwait(false);
            return new ReadOnlyPagedCollection<T>(_uri, _options, response, _nextPageFunc);
        }
    }
}
