﻿using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Concurrent;

namespace Xylab.Management.VirtualFileSystem
{
    // https://github.com/Azure-App-Service/KuduLite
    public class MediaTypeMap
    {
        private static readonly MediaTypeMap _defaultInstance = new();
        private static readonly FileExtensionContentTypeProvider _typeProvider = new();
        private readonly ConcurrentDictionary<string, MediaTypeHeaderValue> _mediatypeMap = CreateMediaTypeMap();
        private readonly MediaTypeHeaderValue _defaultMediaType = MediaTypeHeaderValue.Parse("application/octet-stream");

        public static MediaTypeMap Default => _defaultInstance;

        public static readonly MediaTypeHeaderValue InodeDirectory = MediaTypeHeaderValue.Parse("inode/directory");

        // CORE TODO Double check this. We no longer have MimeMapping so I use FileExtensionContentTypeProvider
        // from the Microsoft.AspNetCore.StaticFiles package. I left in the ConcurrentDictionary usage and the
        // prepopulation of a couple of types (js, json, md) even though FECTP seems to already have them,
        // but I don't think the complexity of it is really needed.
        public MediaTypeHeaderValue GetMediaType(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }

            return _mediatypeMap.GetOrAdd(fileExtension, (extension) =>
            {
                try
                {
                    _typeProvider.TryGetContentType(fileExtension, out string mediaTypeValue);

                    if (mediaTypeValue != null &&
                        MediaTypeHeaderValue.TryParse(mediaTypeValue, out var mediaType))
                    {
                        return mediaType;
                    }

                    return _defaultMediaType;
                }
                catch
                {
                    return _defaultMediaType;
                }
            });
        }

        private static ConcurrentDictionary<string, MediaTypeHeaderValue> CreateMediaTypeMap()
        {
            var dictionary = new ConcurrentDictionary<string, MediaTypeHeaderValue>(StringComparer.OrdinalIgnoreCase);
            dictionary.TryAdd(".js", MediaTypeHeaderValue.Parse("application/javascript"));
            dictionary.TryAdd(".json", MediaTypeHeaderValue.Parse("application/json"));
            dictionary.TryAdd(".log", MediaTypeHeaderValue.Parse("text/plain"));

            // Add media type for markdown
            dictionary.TryAdd(".md", MediaTypeHeaderValue.Parse("text/plain"));

            return dictionary;
        }
    }
}
