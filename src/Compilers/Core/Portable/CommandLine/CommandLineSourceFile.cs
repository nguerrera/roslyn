// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a source file specification stored on command line arguments.
    /// </summary>
    public struct CommandLineSourceFile
    {
        internal CommandLineSourceFile(string path, bool isScript, bool embedInPdb)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            Path = path;
            IsScript = isScript;
            EmbedInPdb = embedInPdb;
        }

        /// <summary>
        /// Resolved absolute path of the source file (does not contain wildcards).
        /// </summary>
        /// <remarks>
        /// Although this path is absolute it may not be normalized. That is, it may contain ".." and "." in the middle. 
        /// </remarks>
        public string Path { get; }

        /// <summary>
        /// True if the file should be treated as a script file.
        /// </summary>
        public bool IsScript { get; }

        /// <summary>
        /// True if the file should be embedded in the PDB.
        /// </summary>
        public bool EmbedInPdb { get; }
    }
}
