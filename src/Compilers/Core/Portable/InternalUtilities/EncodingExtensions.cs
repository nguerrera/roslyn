// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    internal static class EncodingExtensions
    {
        /// <summary>
        /// Get maximum char count needed to decode the entire stream.
        /// </summary>
        /// <exception cref="IOException">Stream is so big that max char count can't fit in <see cref="int"/>.</exception> 
        internal static int GetMaxCharCountOrThrowIfHuge(this Encoding encoding, Stream stream)
        {
            Debug.Assert(stream.CanSeek);
            long length = stream.Length;

            if (length <= int.MaxValue)
            {
                try { return encoding.GetMaxCharCount((int)length); }
                catch (ArgumentOutOfRangeException) { }
            }

            throw new IOException(CodeAnalysisResources.StreamIsTooLong);
        }
    }

#if WORKSPACE_DESKTOP
    internal static partial class CodeAnalysisResources
    {
        public static string StreamIsTooLong => WorkspacesResources.Stream_is_too_long;
    }
#endif
}

