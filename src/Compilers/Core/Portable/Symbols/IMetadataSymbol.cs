// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides the underlying handle for symbols raised from metadata. 
    /// 
    /// Implementations of ISymbol raised from metadata will implement this additional 
    /// interface that callers can query.
    /// </summary>
    public interface IMetadataSymbol
    {
        /// <summary>
        /// Returns the underlying handle to the metadata entity represented by this symbol.
        ///
        /// There are cases where a symbol will implement <see cref="IMetadataSymbol"/>
        /// but return a nil (indicated via <see cref="Handle.IsNil"/>) handle. For example,
        /// an unnamed parameter can have no corresponding <see cref="ParameterHandle"/>.
        ///
        /// The handle is unique only among symbols having the same <see cref="ISymbol.ContainingModule"/>.
        /// </summary>
        Handle MetadataHandle { get; }
    }
}
