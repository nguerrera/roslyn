// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A write-only memory stream backed by a <see cref="BlobBuilder"/>.
    /// </summary>
    internal sealed class LargeBlobBuildingStream : Stream
    {
        /// <summary>
        /// The chunk size to be used by the underlying BlobBuilder.
        /// </summary>
        /// <remarks>
        /// The current single use case for this type is embedded sources in PDBs and we 
        /// have just one of these per compilation so we can afford relatively large chunks.
        ///
        /// 32 KB is:
        ///
        /// * Large enough to handle 99.6% all VB and C# files in Roslyn and CoreFX 
        ///   without allocating additional chunks.
        ///
        /// * Small enough to avoid the large object heap.
        ///
        /// * Large enough to handle the files in the 0.4% case without allocating tons
        ///   of small chunks. Very large source files are often generated in build
        ///   (e.g. Syntax.xml.Generated.vb is 390KB compressed!) and those are actually
        ///   attractive candidates for embedding, so we don't want to discount the large
        ///   case too heavily.)
        /// </remarks>
        public const int ChunkSize = 32 * 1024;

        private BlobBuilder _builder;

        public override bool CanWrite => true;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override long Length => _builder?.Count ?? 0;

        public void Clear() => _builder?.Clear();

        private void LazyAllocateBuilder()
        {
            if (_builder == null)
            {
                // We don't pool this because we expect to rarely exceed 1 chunk
                // per compilation and our chunks are significantly larger than
                // the common blob building cases.
                _builder = new BlobBuilder(ChunkSize);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            LazyAllocateBuilder();
            _builder.WriteBytes(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            LazyAllocateBuilder();
            _builder.WriteByte(value);
        }

        public ImmutableArray<byte> ToImmutableArray()
        {
            return _builder?.ToImmutableArray() ?? ImmutableArray<byte>.Empty;
        }

        /// <summary>
        /// Gets the underlying mutable byte[] segment if it is contiguous.
        /// Otherwise, allocates a large enough array and copies the content
        /// there. The segment offset is always 0, but the count can be less
        /// than the length of the array.
        /// </summary>
        public ArraySegment<byte> GetBytes()
        {
            if (_builder == null)
            {
                return new ArraySegment<byte>(SpecializedCollections.EmptyArray<byte>(), 0, 0);
            }

            BlobBuilder.Blobs blobs = _builder.GetBlobs();
            bool atLeastOne = blobs.MoveNext();
            Debug.Assert(atLeastOne);

            Blob blob = blobs.Current;
            if (blobs.MoveNext())
            {
                // more than 1 chunk
                return new ArraySegment<byte>(_builder.ToArray(), 0, _builder.Count);
            }

            return blob.GetBytes();
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}
