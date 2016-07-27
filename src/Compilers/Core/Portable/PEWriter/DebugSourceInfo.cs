// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents the portion of a <see cref="DebugSourceDocument"/> that are derived
    /// from the source document content, and which are computed asynchronously.
    /// </summary>
    internal struct DebugSourceInfo
    {
        /// <summary>
        /// The ID of the hash algorithm used.
        /// </summary>
        public readonly Guid AlgorithmId;

        /// <summary>
        /// The hash of the document content.
        /// </summary>
        public readonly ImmutableArray<byte> Checksum;

        /// <summary>
        /// The source text to embed in the PDB. (If any, otherwise default.)
        /// </summary>
        public readonly ImmutableArray<byte> EmbeddedTextBlobOpt;

        public DebugSourceInfo(SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> checksum, ImmutableArray<byte> embeddedTextBlobOpt = default(ImmutableArray<byte>))
        {
            Debug.Assert(DebugSourceDocument.IsSupportedAlgorithm(checksumAlgorithm));

            AlgorithmId = DebugSourceDocument.GetAlgorithmGuid(checksumAlgorithm);
            Checksum = checksum;
            EmbeddedTextBlobOpt = embeddedTextBlobOpt;
        }
    }
}