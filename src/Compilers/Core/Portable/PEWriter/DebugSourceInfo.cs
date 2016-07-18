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
        /// The maximum number of characters in <see cref="EmbeddedTextOpt"/> to write out uncompressed.
        /// This prevents wasting resources on compressing tiny files with little to negative gain
        /// in PDB file size.
        ///
        /// Chosen as the point at which we start to see > 10% blob size reduction using all current
        /// source files in corefx and roslyn as sample data. 
        /// </summary>
        public const int CompressionThreshold = 200;
 
        /// <summary>
        /// The ID of the hash algorithm used.
        /// See <see cref="DebugSourceDocument.TryGetAlgorithmGuid(CodeAnalysis.Text.SourceHashAlgorithm, out Guid)"/>
        /// </summary>
        public readonly Guid AlgorithmId;

        /// <summary>
        /// The hash of the document content.
        /// </summary>
        public readonly ImmutableArray<byte> Checksum;

        /// <summary>
        /// The source text to embed in the PDB. (If any, otherwise null.)
        /// </summary>
        public readonly SourceText EmbeddedTextOpt;

        public DebugSourceInfo(Guid checksumAlgorithmId, ImmutableArray<byte> checksum, SourceText embeddedTextOpt = null)
        {
            Checksum = checksum;
            AlgorithmId = checksumAlgorithmId;
            EmbeddedTextOpt = embeddedTextOpt?.Length > 0 ? embeddedTextOpt : null;
        }

        public ushort WriteEmbeddedText(LargeBlobBuildingStream blobBuilder, bool writeFormatCode)
        {
            const ushort RawFormat = 0;
            const ushort GzipFormat = 1;

            Debug.Assert(blobBuilder != null);
            Debug.Assert(EmbeddedTextOpt != null); // Don't call unless EmbeddedText is non-null.
            Debug.Assert(EmbeddedTextOpt.Length > 0); // Should have been dropped in constructor
            Debug.Assert(EmbeddedTextOpt.Encoding != null); // We should have raised ERR_EncodinglessSyntaxTree.

            Stream target = blobBuilder;
            ushort format = EmbeddedTextOpt.Length > CompressionThreshold ? GzipFormat : RawFormat;

            blobBuilder.Clear();

            if (writeFormatCode)
            {
                blobBuilder.WriteUInt16(format);
            }

            if (format == GzipFormat)
            {
                target = new GZipStream(target, CompressionLevel.Optimal);
            }

            using (var writer = new StreamWriter(target, EmbeddedTextOpt.Encoding))
            {
                EmbeddedTextOpt.Write(writer);
            }

            return format;
        }
    }
}