// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal partial class LineDirectiveMap
    {
        /// <summary>
        /// Enum that describes the state related to the #line or #externalsource directives at a position in source.
        /// </summary>
        public enum PositionState : byte
        {
            /// <summary>
            /// Used in VB when the position is not hidden, but it's not known yet that there is a (nonempty) #ExternalSource
            /// following.
            /// </summary>
            Unknown,

            /// <summary>
            /// Used in C# for spans outside of #line directives
            /// </summary>
            Unmapped,

            /// <summary>
            /// Used in C# for spans inside of "#line linenumber" directive
            /// </summary>
            Remapped,

            /// <summary>
            /// Used in VB for spans inside of a "#ExternalSource" directive that followed an unknown span
            /// </summary>
            RemappedAfterUnknown,

            /// <summary>
            /// Used in VB for spans inside of a "#ExternalSource" directive that followed a hidden span
            /// </summary>
            RemappedAfterHidden,

            /// <summary>
            /// Used in C# and VB for spans that are inside of #line hidden (C#) or outside of #ExternalSource (VB) 
            /// directives
            /// </summary>
            Hidden
        }

        // Struct that represents an entry in the line mapping table. Entries sort by the unmapped
        // line.
        public struct LineMappingEntry : IEquatable<LineMappingEntry>
        {
            public static readonly IComparer<LineMappingEntry> UnmappedLineComparer = new UnmappedLineComparerImplementation(); 

            // 0-based line in this tree
            public readonly int UnmappedLine;

            // 0-based line it maps to.
            public readonly int MappedLine;

            // raw value from #line or #ExternalDirective, may be null
            public readonly string MappedPathOpt;

            // the state of this line
            public readonly PositionState State;

            public LineMappingEntry(int unmappedLine)
            {
                this.UnmappedLine = unmappedLine;
                this.MappedLine = unmappedLine;
                this.MappedPathOpt = null;
                this.State = PositionState.Unmapped;
            }

            public LineMappingEntry(
                int unmappedLine,
                int mappedLine,
                string mappedPathOpt,
                PositionState state)
            {
                this.UnmappedLine = unmappedLine;
                this.MappedLine = mappedLine;
                this.MappedPathOpt = mappedPathOpt;
                this.State = state;
            }

            public override bool Equals(object obj)
            {
                return obj is LineMappingEntry && Equals((LineMappingEntry)obj);
            }

            public static bool operator==(LineMappingEntry left, LineMappingEntry right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(LineMappingEntry left, LineMappingEntry right)
            {
                return !left.Equals(right);
            }

            public bool Equals(LineMappingEntry other)
            {
                return UnmappedLine == other.UnmappedLine &&
                    MappedLine == other.MappedLine &&
                    MappedPathOpt == other.MappedPathOpt &&
                    State == other.State;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(UnmappedLine, Hash.Combine(MappedLine, Hash.Combine(MappedPathOpt, (int)State)));
            }

            private sealed class UnmappedLineComparerImplementation : IComparer<LineMappingEntry>
            {
                public int Compare(LineMappingEntry x, LineMappingEntry y)
                {
                    return x.UnmappedLine.CompareTo(y.UnmappedLine);
                }
            }
        }
    }
}
