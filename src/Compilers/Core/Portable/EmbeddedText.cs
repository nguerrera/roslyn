// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents text to be embedded in a PDB.
    /// </summary>
    public sealed class EmbeddedText
    {
        /// <summary>
        /// The maximum number of bytes in to write out uncompressed.
        ///
        /// This prevents wasting resources on compressing tiny files with little to negative gain
        /// in PDB file size.
        ///
        /// Chosen as the point at which we start to see > 10% blob size reduction using all
        /// current source files in corefx and roslyn as sample data. 
        /// </summary>
        internal const int CompressionThreshold = 200;

        /// <summary>
        /// The path to the file to embed.
        /// </summary>
        /// <remarks>See remarks of <see cref="SyntaxTree.FilePath"/></remarks>
        public string FilePath { get; }

        /// <summary>
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm { get; }

        /// <summary>
        /// The <see cref="ChecksumAlgorithm"/> hash of the uncrompressed bytes
        /// that's saved to the PDB.
        /// </summary>
        /// <remarks>
        /// Internal for consistency with SourceText and maximum flexibility around
        /// when we compute the checksum.
        /// </remarks>
        internal ImmutableArray<byte> Checksum { get; }

        /// <summary>
        /// The content that will be written to the PDB.
        /// </summary>
        /// <remarks>
        /// Internal since this it is an implementation. The only public
        /// contract is that you can pass EmbeddedText instances to Emit.
        /// It just so happened that doing this up-front was most practical
        /// and efficient, but we don't want to be tied to it.
        /// 
        /// For efficiency, the format of this blob is exactly as it is written to the PDB,
        /// which prevents extra copies being maded during emit.
        ///
        /// The first 4 bytes (little endian int32) indicate the format:
        ///
        ///            0: data that follows is uncompressed
        ///     Positive: data that follows is deflate compressed is original, uncompressed size
        ///     Negative: invalid at this time, but reserved to mark a different format in the future.
        /// </remarks>
        internal ImmutableArray<byte> Blob { get; }

        /// <summary>
        /// Constructs a <see cref="EmbeddedText"/> for embedding the given <see cref="SourceText"/>.
        /// </summary>
        /// <param name="filePath">The file path (pre-normalization) to use in the PDB.</param>
        /// <param name="text">The source text to embed.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filePath"/> is null.
        /// <paramref name="text"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath"/> empty.
        /// <paramref name="text"/> cannot be embedded (see <see cref="SourceText.CanBeEmbedded"/>).
        /// <paramref name="text"/> is too long (maximum byte count for its encoding exceeds <see cref="int.MaxValue"/>).
        /// </exception>
        public static EmbeddedText FromSource(string filePath, SourceText text)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(filePath));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!text.CanBeEmbedded)
            {
                throw new ArgumentException(CodeAnalysisResources.SourceTextCannotBeEmbedded, nameof(text));
            }

            if (!text.PrecomputedEmbeddedTextBlob.IsDefault)
            {
                return new EmbeddedText(filePath, text.GetChecksum(), text.ChecksumAlgorithm, text.PrecomputedEmbeddedTextBlob);
            }

            return new EmbeddedText(filePath, text.GetChecksum(), text.ChecksumAlgorithm, CreateBlob(text));
        }

        /// <summary>
        /// Constructs an <see cref="EmbeddedText"/> from stream content.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="checksumAlgorithm">Hash algorithm to use to calculate checksum of the text that's saved to PDB.</param>
        /// <exception cref="ArgumentNullException">
        /// <param name="filePath" /> is null.
        /// <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath" /> is empty.
        /// <paramref name="stream"/> doesn't support reading or seeking.
        /// <paramref name="stream" /> is longer than <see cref="Int32.MaxValue" /> bytes.
        /// <paramref name="checksumAlgorithm"/> is not supported.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>Reads from the beginning of the stream. Leaves the stream open.</remarks>
        public static EmbeddedText FromStream(string filePath, Stream stream, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(filePath));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, nameof(stream));
            }

            if (stream.Length > int.MaxValue)
            {
                throw new IOException(CodeAnalysisResources.StreamIsTooLong);
            }

            SourceText.ValidateChecksumAlgorithm(checksumAlgorithm);

            return new EmbeddedText(
                filePath,
                SourceText.CalculateChecksum(stream, checksumAlgorithm),
                checksumAlgorithm,
                CreateBlob(stream));
        }

        public static EmbeddedText FromBytes(string filePath, byte[] buffer, int length, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(filePath));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (length < 0 || length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            SourceText.ValidateChecksumAlgorithm(checksumAlgorithm);

            return new EmbeddedText(
                filePath, 
                SourceText.CalculateChecksum(buffer, 0, length, checksumAlgorithm),
                checksumAlgorithm, 
                CreateBlob(buffer, length));
        }

        private EmbeddedText(string filePath, ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> blob)
        {
            Debug.Assert(filePath?.Length > 0);
            Debug.Assert(Cci.DebugSourceDocument.IsSupportedAlgorithm(checksumAlgorithm));
            Debug.Assert(!blob.IsDefault && blob.Length >= sizeof(int));

            FilePath = filePath;
            Checksum = checksum;
            ChecksumAlgorithm = checksumAlgorithm;
            Blob = blob;
        }

        /// <summary>
        /// Creates the blob to be saved to the PDB.
        /// </summary>
        internal static ImmutableArray<byte> CreateBlob(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            Debug.Assert(stream.CanSeek);;
            Debug.Assert(stream.Length <= int.MaxValue);

            stream.Seek(0, SeekOrigin.Begin);
            int length = (int)stream.Length;

            if (length < CompressionThreshold)
            {
                var builder = Cci.PooledBlobBuilder.GetInstance();
                builder.WriteInt32(0);

                int bytesWritten = builder.TryWriteBytes(stream, length);
                if (length != bytesWritten)
                {
                    throw new EndOfStreamException();
                }

                return builder.ToImmutableArrayAndFree();
            }
            else
            {
                Debug.Assert(length > 0);
                var builder = BlobBuildingStream.GetInstance();
                builder.WriteInt32(length);

                using (var deflater = new DeflateStream(builder, CompressionLevel.Optimal))
                {
                    stream.CopyTo(deflater);
                }

                return builder.ToImmutableArrayAndFree();
            }
        }

        internal static ImmutableArray<byte> CreateBlob(byte[] buffer, int length)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(length > 0);
            Debug.Assert(length <= buffer.Length);

            if (length < CompressionThreshold)
            {
                var builder = Cci.PooledBlobBuilder.GetInstance();
                builder.WriteInt32(0);
                builder.WriteBytes(buffer, 0, length);
                return builder.ToImmutableArrayAndFree();
            }
            else
            {
                Debug.Assert(length > 0);
                var builder = BlobBuildingStream.GetInstance();
                builder.WriteInt32(length);

                using (var deflater = new DeflateStream(builder, CompressionLevel.Optimal))
                {
                    deflater.Write(buffer, 0, length);
                }

                return builder.ToImmutableArrayAndFree();
            }
        }

        private static ImmutableArray<byte> CreateBlob(SourceText sourceText)
        {
            Debug.Assert(sourceText != null);
            Debug.Assert(sourceText.CanBeEmbedded);
            Debug.Assert(sourceText.Encoding != null);
            Debug.Assert(sourceText.PrecomputedEmbeddedTextBlob.IsDefault);

            int maxByteCount = int.MaxValue;
            try { maxByteCount = sourceText.Encoding.GetMaxByteCount(sourceText.Length);  }
            catch (ArgumentOutOfRangeException) { }

            var builder = BlobBuildingStream.GetInstance();
            builder.WriteInt32(0);

            var deflater = maxByteCount > CompressionThreshold ? new CountingDeflateStream(builder) : null;
            using (var writer = new StreamWriter((Stream)deflater ?? builder, sourceText.Encoding))
            {
                sourceText.Write(writer);
            }

            if (deflater != null)
            {
                builder.PatchLeadingInt32(deflater.BytesWritten);
            }

            return builder.ToImmutableArrayAndFree();
        }

        private sealed class CountingDeflateStream : DeflateStream
        {
            public CountingDeflateStream(Stream stream)
                : base(stream, CompressionLevel.Optimal)
            {
            }

            public int BytesWritten { get; private set; }

            public override void Write(byte[] array, int offset, int count)
            {
                base.Write(array, offset, count);

                // checked arithmetic is release-enabled quasi-assert. We start with at most 
                // int.MaxValue chars so compression or encoding would have to be abysmal for
                // this to overflow. We'd probably be lucky to even get this far but if we do
                // we should fail fast.
                checked { BytesWritten += count; }
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override void WriteByte(byte value)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal Cci.DebugSourceInfo GetDebugSourceInfo()
        {
            return new Cci.DebugSourceInfo(Checksum, ChecksumAlgorithm, Blob);
        }
    }
}
