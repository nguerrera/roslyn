// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using System.Text;
using System.IO.Compression;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EmbeddedTextTests
    {
        [Fact]
        public void FromBytes_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromBytes(null, default(ArraySegment<byte>)));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromBytes("", default(ArraySegment<byte>)));
            Assert.Throws<ArgumentNullException>("bytes", () => EmbeddedText.FromBytes("path", default(ArraySegment<byte>)));
            Assert.Throws<ArgumentException>("checksumAlgorithm", () => EmbeddedText.FromBytes("path", new ArraySegment<byte>(new byte[0], 0, 0), SourceHashAlgorithm.None));
        }

        [Fact]
        public void FromSource_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromSource(null, null));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromSource("", null));
            Assert.Throws<ArgumentNullException>("text", () => EmbeddedText.FromSource("path", null));

            // no encoding
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From("source")));

            // embedding not allowed
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From(new byte[0], 0, Encoding.UTF8, canBeEmbedded: false)));
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From(new MemoryStream(new byte[0]), Encoding.UTF8, canBeEmbedded: false)));
        }

        [Fact]
        public void FromStream_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromStream(null, null));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromStream("", null));
            Assert.Throws<ArgumentNullException>("stream", () => EmbeddedText.FromStream("path", null));
            Assert.Throws<ArgumentException>("stream", () => EmbeddedText.FromStream("path", new CannotReadStream()));
            Assert.Throws<ArgumentException>("stream", () => EmbeddedText.FromStream("path", new CannotSeekStream()));
            Assert.Throws<ArgumentException>("checksumAlgorithm", () => EmbeddedText.FromStream("path", new MemoryStream(), SourceHashAlgorithm.None));
        }

        [Fact]
        public void FromStream_IOErrors()
        {
            Assert.Throws<IOException>(() => EmbeddedText.FromStream("path", new HugeStream()));

            // TODO: File bug (pre-eixsting) ComputeHash throws TargetInvocationException via reflection instead of unwrapping I/O exception.
            //Assert.Throws<IOException>(() => EmbeddedText.FromStream("path", new ReadFailsStream()));
        }

        private const string SmallSource = @"class P {}";
        private const string LargeSource = @"
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
class Program 
{
    static void Main() {}
}
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
";

        [Fact]
        public void FromBytes_Empty()
        {
            var text = EmbeddedText.FromBytes("pathToEmpty", new ArraySegment<byte>(new byte[0], 0, 0), SourceHashAlgorithm.Sha1);
            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(text.ChecksumAlgorithm, SourceHashAlgorithm.Sha1);
            AssertEx.Equal(SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1), text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromStream_Empty()
        {
            var text = EmbeddedText.FromStream("pathToEmpty", new MemoryStream(new byte[0]), SourceHashAlgorithm.Sha1);
            var checksum = SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1);
            
            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(text.ChecksumAlgorithm, SourceHashAlgorithm.Sha1);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromText_Empty()
        {
            var source = SourceText.From("", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromSource("pathToEmpty", source);
            var checksum = SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1);

            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromBytes_Small()
        {
            var bytes = Encoding.UTF8.GetBytes(SmallSource);
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromBytes("pathToSmall", new ArraySegment<byte>(bytes, 0, bytes.Length));

            Assert.Equal("pathToSmall", text.FilePath);
            Assert.Equal(text.ChecksumAlgorithm, SourceHashAlgorithm.Sha1);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob.Take(4));
            AssertEx.Equal(bytes, text.Blob.Skip(4));
        }

        [Fact]
        public void FromBytes_Large()
        {
            var bytes = Encoding.Unicode.GetBytes(LargeSource);
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha256);
            var text = EmbeddedText.FromBytes("pathToLarge", new ArraySegment<byte>(bytes, 0, bytes.Length), SourceHashAlgorithm.Sha256);

            Assert.Equal("pathToLarge", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha256, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(ToInt32LE(bytes.Length), text.Blob.Take(4));
            AssertEx.Equal(bytes, Decompress(text.Blob.Skip(4)));
        }

        [Fact]
        public void FromBytes_SmallSpan()
        {
            var bytes = Encoding.UTF8.GetBytes(SmallSource);
            var padddedBytes = new byte[] { 0 }.Concat(bytes).Concat(new byte[] { 0 }).ToArray();
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromBytes("pathToSmall", new ArraySegment<byte>(padddedBytes, 1, bytes.Length));

            Assert.Equal("pathToSmall", text.FilePath);
            AssertEx.Equal(checksum, text.Checksum);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob.Take(4));
            AssertEx.Equal(bytes, text.Blob.Skip(4));
        }

        [Fact]
        public void FromBytes_LargeSpan()
        {
            var bytes = Encoding.Unicode.GetBytes(LargeSource);
            var paddedBytes = new byte[] { 0 }.Concat(bytes).Concat(new byte[] { 0 }).ToArray();
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha256);
            var text = EmbeddedText.FromBytes("pathToLarge", new ArraySegment<byte>(paddedBytes, 1, bytes.Length), SourceHashAlgorithm.Sha256);

            Assert.Equal("pathToLarge", text.FilePath);
            AssertEx.Equal(checksum, text.Checksum);
            Assert.Equal(SourceHashAlgorithm.Sha256, text.ChecksumAlgorithm);
            AssertEx.Equal(ToInt32LE(bytes.Length), text.Blob.Take(4));
            AssertEx.Equal(bytes, Decompress(text.Blob.Skip(4)));
        }

        private byte[] ToInt32LE(int length)
        {
            return new byte[]
            {
                (byte)((length >> 0) & 0xFF),
                (byte)((length >> 8) & 0xFF),
                (byte)((length >> 16) & 0xFF),
                (byte)((length >> 24) & 0xFF),
            };
        }

        private byte[] Decompress(IEnumerable<byte> bytes)
        {
            var destination = new MemoryStream();
            using (var source = new DeflateStream(new MemoryStream(bytes.ToArray()), CompressionMode.Decompress))
            {
                source.CopyTo(destination);
            }

            return destination.ToArray();
        }

        private sealed class CannotReadStream : MemoryStream
        {
            public override bool CanRead => false;
        }

        private sealed class CannotSeekStream : MemoryStream
        {
            public override bool CanSeek => false;
        }

        private sealed class HugeStream : MemoryStream
        {
            public override long Length => (long)int.MaxValue + 1;
        }

        private sealed class ReadFailsStream : MemoryStream
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException();
            }
        }
    }
}
