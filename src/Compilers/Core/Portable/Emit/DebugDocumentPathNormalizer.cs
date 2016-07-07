// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct DebugDocumentPathNormalizer
    {
        private readonly ConcurrentCache<ValueTuple<string, string>, string> _cache;

        public static DebugDocumentPathNormalizer Create(int cacheSize = 16)
        {
           return new DebugDocumentPathNormalizer(
               new ConcurrentCache<ValueTuple<string, string>, string>(cacheSize));
        }

        private DebugDocumentPathNormalizer(ConcurrentCache<ValueTuple<string, string>, string> cache)
        {
            _cache = cache;
        }

        public bool IsDefault => _cache == null;

        public string Normalize(SourceReferenceResolver resolver, string path, string basePath)
        {
            if (resolver == null)
            {
                return path;
            }

            var key = ValueTuple.Create(path, basePath);
            string normalizedPath;
            if (!_cache.TryGetValue(key, out normalizedPath))
            {
                normalizedPath = resolver.NormalizePath(path, basePath) ?? path;
                _cache.TryAdd(key, normalizedPath);
            }

            return normalizedPath;
        }
    }
}
