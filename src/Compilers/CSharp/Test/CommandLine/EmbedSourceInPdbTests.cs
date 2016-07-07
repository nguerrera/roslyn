// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.PdbUtilities;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class EmbedSourceInPdbTests : CSharpTestBase
    {
        private const string Source1 = "class P { static void Main() { } }";
        private const string Source2 = "class C { public C() { } }";

        private void Run(Func<string, string, string[]> makeArgs, Action<string, string, int, string, XElement> assert)
        {
            foreach (string debugArg in new[] { "/debug+", "/debug:portable" })
            {
                var file1 = Temp.CreateFile().WriteAllText(Source1).Path;
                var file2 = Temp.CreateFile().WriteAllText(Source2).Path;

                var cmd = new MockCSharpCompiler(
                    responseFile: null,
                    baseDirectory: TempRoot.Root,
                    args: new[] {
                        "/nologo",
                        "/preferreduilang:en",
                        file1,
                        file2,
                        debugArg
                    }.Concat(makeArgs(file1, file2)).ToArray());

                var outputWriter = new StringWriter();
                int exitCode = cmd.Run(outputWriter);
                string output = outputWriter.ToString().Trim();
                XElement pdbXml = null;

                if (exitCode == 0)
                {
                    using (var peStream = File.OpenRead(Path.ChangeExtension(file1, "exe")))
                    using (var pdbStream = File.OpenRead(Path.ChangeExtension(file1, "pdb")))
                    {
                        pdbXml = XElement.Parse(PdbToXmlConverter.ToXml(pdbStream, peStream));
                    }
                }

                assert(file1, file2, exitCode, output, pdbXml);
                CleanupAllGeneratedFiles(file1);
                CleanupAllGeneratedFiles(file2);
            }
        }

        [Fact]
        public void EmbedAll() 
        {
            Run(
                (file1, file2) => new[] { "/embedsourceinpdb" },
                (file1, file2, exitCode, output, pdbXml) => {
                    Assert.Equal(0, exitCode);
                    Assert.Equal(2, pdbXml.Descendants().Count(n => n.Name == "embeddedSource"));
                    Assert.Empty(output);
                });
        }

        [Fact]
        public void EmbedOnlyFirst()
        {
            Run(
                (file1, file2) => new[] { $"/embedsourceinpdb:{file1}" },
                (file1, file2, exitCode, output, pdbXml) => {
                    Assert.Equal(0, exitCode);
                    Assert.Equal(Source1, pdbXml.Descendants().Single(n => n.Name == "embeddedSource").Value);
                    Assert.Empty(output);
                });
        }

        [Fact]
        public void EmbedOnlySecond()
        {
            Run(
                (file1, file2) => new[] { $"/embedsourceinpdb:{file2}" },
                (file1, file2, exitCode, output, pdbXml) => {
                    Assert.Equal(0, exitCode);
                    Assert.Equal(Source2, pdbXml.Descendants().Single(n => n.Name == "embeddedSource").Value);
                    Assert.Empty(output);
                });
        }

        [Fact]
        public void EmbedAllWithRedundancy()
        {
            Run(
                (file1, file2) => new[] { "/embedsourceinpdb", $"/embedsourceinpdb:{file1}" , $"/embedsourceinpdb:{file2}" },
                (file1, file2, exitCode, output, pdbXml) => {
                    Assert.Equal(0, exitCode);
                    Assert.Equal(2, pdbXml.Descendants().Count(n => n.Name == "embeddedSource"));
                    Assert.Equal("warning CS7103: Ignoring specific files to embed in PDB because all files will be embedded.", output);
                });
        }

        [Fact]
        public void EmbedFileNotInCompilation()
        {
            Run(
                (file1, file2) => new[] { "/embedsourceinpdb:nope.cs" },
                (file1, file2, exitCode, output, pdbXml) => {
                    Assert.NotEqual(0, exitCode);
                    Assert.Equal($"error CS7104: Source file '{Path.Combine(TempRoot.Root, "nope.cs")}' cannot be embedded in the PDB because it is not part of the compilation.", output);
                });
        }
    }
}