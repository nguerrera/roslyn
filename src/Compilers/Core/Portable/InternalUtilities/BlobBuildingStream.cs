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
    internal sealed class BlobBuildingStream : Stream
    {
        private static ObjectPool<BlobBuildingStream> s_pool = new ObjectPool<BlobBuildingStream>(() => new BlobBuildingStream());
        private BlobBuilder _builder;

        /// <summary>
        /// The chunk size to be used by the underlying BlobBuilder.
        /// </summary>
        /// <remarks>
        /// The current single use case for this type is embedded sources in PDBs.
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
        ///
        /// * We pool the outer BlobBuildingStream but only retain the first allocated chunk.
        /// </remarks>
        public const int ChunkSize = 32 * 1024;

        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override long Length => _builder.Count;

        public static BlobBuildingStream GetInstance()
        {
            return s_pool.Allocate();
        }

        private BlobBuildingStream()
        {
            // NOTE: We pool the wrapping BlobBuildingStream, but not individual chunks.
            // The first chunk will be reused, but any further chunks will be freed when we're done building blob.
            _builder = new BlobBuilder(ChunkSize);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _builder.WriteBytes(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            _builder.WriteByte(value);
        }

        public void WriteInt32(int value)
        {
            _builder.WriteInt32(value);
        }

        public void PatchLeadingInt32(int value)
        {
            foreach (var blob in _builder.GetBlobs())
            {
                ArraySegment<byte> segment = blob.GetBytes();
                Debug.Assert(segment.Count > 4);

                byte[] array = segment.Array;
                array[0] = (byte)(value & 0xFF);
                array[1] = (byte)((value >> 8) & 0xFF);
                array[2] = (byte)((value >> 16) & 0xFF);
                array[3] = (byte)((value >> 24) & 0xFF); 

                return;
            }

            throw ExceptionUtilities.Unreachable;
        }

        public ImmutableArray<byte> ToImmutableArrayAndFree()
        {
            var blob = _builder.ToImmutableArray();
            _builder.Clear(); // frees all but one chunk.
            s_pool.Free(this);
            return blob;
        }

        public override void Flush()
        {
        }

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
