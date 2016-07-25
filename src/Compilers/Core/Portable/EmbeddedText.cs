// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents text to be embedded in a PDB
    /// </summary>
    public struct EmbeddedText : IEquatable<EmbeddedText>
    {
        /// <summary>
        /// The path to the file to embed.
        /// </summary>
        /// <remarks>See remarks of <see cref="SyntaxTree.FilePath"/> </remarks>
        public string FilePath { get; }

        /// <summary>
        /// The text to embed.
        /// </summary>
        public SourceText Text { get; }

        /// <summary>
        /// Indicates if this instance has a default, uninitialized value.
        /// </summary>
        public bool IsDefault => FilePath == null;

        public EmbeddedText(string filePath, SourceText text)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentException(nameof(filePath), CodeAnalysisResources.ArgumentCannotBeEmpty);
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Encoding == null)
            {
                throw new ArgumentException(nameof(text), CodeAnalysisResources.EmbeddedTextMustHaveEncoding);
            }

            FilePath = filePath;
            Text = text;
        }

        public override int GetHashCode()
        {
            return IsDefault ? 0 : Hash.Combine(FilePath.GetHashCode(), Text.GetHashCode());
        }

        public bool Equals(EmbeddedText other)
        {
            return other.FilePath == FilePath && other.Text == Text;
        }

        public override bool Equals(object obj)
        {
            return obj is EmbeddedText && Equals((EmbeddedText)obj);
        }

        public static bool operator ==(EmbeddedText left, EmbeddedText right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EmbeddedText left, EmbeddedText right)
        {
            return !left.Equals(right);
        }

        internal Cci.DebugSourceInfo GetDebugSourceInfo()
        {
            Debug.Assert(!IsDefault);

            Guid guid;
            if (!Cci.DebugSourceDocument.TryGetAlgorithmGuid(Text.ChecksumAlgorithm, out guid))
            {
                throw ExceptionUtilities.Unreachable;
            }

            return new Cci.DebugSourceInfo(guid, Text.GetChecksum(), embeddedTextOpt: Text);
        }
    }
}
