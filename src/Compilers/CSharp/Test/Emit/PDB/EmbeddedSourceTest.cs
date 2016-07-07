// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class EmbeddedSourceTest : CSharpTestBase
    {
        [Fact]
        public void EmbeddedSource()
        {
            var source1 = "public class C1 { public C1() { } }";
            var source2 = "public class C2 { public C2() { } }" + new string('/', 200); // comment padding to force compression

            var tree1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(source1, Encoding.UTF8, SourceHashAlgorithm.Sha1), path: "source1.cs");
            var tree2 = SyntaxFactory.ParseSyntaxTree(SourceText.From(source2, Encoding.Unicode, SourceHashAlgorithm.Sha1), path: "source2.cs");
            var compilation = CreateCompilationWithMscorlib(new[] { tree1, tree2 });

            string xml = $@"
<symbols>
  <files>
    <file id=""1"" name=""source1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""8E, 37, F3, 94, ED, 18, 24, 3F, 35, EC, 1B, 70, 25, 29, 42, 1C, B0, 84, 9B, C8, "">
      <embeddedSource compressed=""False"" encoding=""utf-8"" preamble=""EF, BB, BF, "">{source1}</embeddedSource>
    </file>
    <file id=""2"" name=""source2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""5C, B6, FB, 11, 77, D4, 66, 8D, 75, 97, F9, 6C, 2F, DE, 7A, 38, 6E, 13, FE, 7A, "">
      <embeddedSource compressed=""True"" encoding=""utf-16"" preamble=""FF, FE, "">{source2}</embeddedSource>
    </file>
  </files>
  <methods>
    <method containingType=""C1"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""30"" document=""1"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""33"" endLine=""1"" endColumn=""34"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C2"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""30"" document=""2"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""33"" endLine=""1"" endColumn=""34"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>";

            compilation.VerifyPdb(xml, embedSourceInPdb: t => true);
            compilation.VerifyPdb(PdbValidation.PruneEmbeddedSource(xml), embedSourceInPdb: null);
            compilation.VerifyPdb(PdbValidation.PruneEmbeddedSource(xml), embedSourceInPdb: t => false);
            compilation.VerifyPdb(PdbValidation.PruneEmbeddedSource(xml, tree1), embedSourceInPdb: t => t == tree1);
            compilation.VerifyPdb(PdbValidation.PruneEmbeddedSource(xml, tree2), embedSourceInPdb: t => t == tree2);
        }
    }
}
