﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexAndRangeTests : CSharpTestBase
    {
        private CompilationVerifier CompileAndVerifyWithIndexAndRange(string s, string expectedOutput = null)
        {
            var comp = CreateCompilationWithIndexAndRange(
                new[] { s, TestSources.GetSubArray, },
                expectedOutput is null ? TestOptions.ReleaseDll : TestOptions.ReleaseExe);
            return CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void StringAndSpanPatternRangeOpenEnd()
        {
            var src = @"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcd"";
        Console.WriteLine(s[..]);
        ReadOnlySpan<char> span = s;
        foreach (var c in span[..])
        {
            Console.Write(c);
        }
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
abcd
abcd");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       86 (0x56)
  .maxstack  4
  .locals init (int V_0,
                System.ReadOnlySpan<char> V_1,
                System.ReadOnlySpan<char> V_2,
                int V_3)
  IL_0000:  ldstr      ""abcd""
  IL_0005:  dup
  IL_0006:  dup
  IL_0007:  callvirt   ""int string.Length.get""
  IL_000c:  ldc.i4.0
  IL_000d:  sub
  IL_000e:  stloc.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   ""string string.Substring(int, int)""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_0020:  stloc.2
  IL_0021:  ldloca.s   V_2
  IL_0023:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0028:  ldc.i4.0
  IL_0029:  sub
  IL_002a:  stloc.3
  IL_002b:  ldloca.s   V_2
  IL_002d:  ldc.i4.0
  IL_002e:  ldloc.3
  IL_002f:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int, int)""
  IL_0034:  stloc.1
  IL_0035:  ldc.i4.0
  IL_0036:  stloc.0
  IL_0037:  br.s       IL_004b
  IL_0039:  ldloca.s   V_1
  IL_003b:  ldloc.0
  IL_003c:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0041:  ldind.u2
  IL_0042:  call       ""void System.Console.Write(char)""
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4.1
  IL_0049:  add
  IL_004a:  stloc.0
  IL_004b:  ldloc.0
  IL_004c:  ldloca.s   V_1
  IL_004e:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0053:  blt.s      IL_0039
  IL_0055:  ret
}");
        }

        [Fact]
        public void SpanTaskReturn()
        {
            var src = @"
using System;
using System.Threading.Tasks;
class C
{
    static void Throws(Action a)
    {
        try
        {
            a();
        }
        catch
        {
            Console.WriteLine(""throws"");
        }
    }

    public static void Main()
    {
        string s = ""abcd"";
        Throws(() => { var span = new Span<char>(s.ToCharArray())[0..10]; });
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "throws");
        }

        [Fact]
        public void PatternIndexSetter()
        {
            var src = @"
using System;
struct S
{
    public int F;
    public int Length => 1;
    public int this[int i]
    {
        get => F;
        set { F = value; }
    }
}
class C
{
    static void Main()
    {
        S s = new S();
        s.F = 0;
        Console.WriteLine(s[^1]);
        s[^1] = 2;
        Console.WriteLine(s[^1]);
        Console.WriteLine(s.F);
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"0
2
2");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  .locals init (S V_0, //s
                int V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  stfld      ""int S.F""
  IL_0010:  ldloca.s   V_0
  IL_0012:  dup
  IL_0013:  call       ""int S.Length.get""
  IL_0018:  ldc.i4.1
  IL_0019:  sub
  IL_001a:  stloc.1
  IL_001b:  ldloc.1
  IL_001c:  call       ""int S.this[int].get""
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  ldloca.s   V_0
  IL_0028:  dup
  IL_0029:  call       ""int S.Length.get""
  IL_002e:  ldc.i4.1
  IL_002f:  sub
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.2
  IL_0033:  call       ""void S.this[int].set""
  IL_0038:  ldloca.s   V_0
  IL_003a:  dup
  IL_003b:  call       ""int S.Length.get""
  IL_0040:  ldc.i4.1
  IL_0041:  sub
  IL_0042:  stloc.1
  IL_0043:  ldloc.1
  IL_0044:  call       ""int S.this[int].get""
  IL_0049:  call       ""void System.Console.WriteLine(int)""
  IL_004e:  ldloc.0
  IL_004f:  ldfld      ""int S.F""
  IL_0054:  call       ""void System.Console.WriteLine(int)""
  IL_0059:  ret
}");
        }

        [Fact]
        public void PatternIndexerRefReturn()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s = new int[] { 2, 4, 5, 6 };
        Console.WriteLine(s[^2]);
        ref int x = ref s[^2];
        Console.WriteLine(x);
        s[^2] = 9;
        Console.WriteLine(s[^2]);
        Console.WriteLine(x);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"5
5
9
9");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      120 (0x78)
  .maxstack  4
  .locals init (System.Span<int> V_0, //s
                int V_1,
                int V_2)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.D033850C1A3F6F1209A6CD84146E8561DDC73C79""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<int> System.Span<int>.op_Implicit(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  dup
  IL_001a:  call       ""int System.Span<int>.Length.get""
  IL_001f:  ldc.i4.2
  IL_0020:  sub
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  call       ""ref int System.Span<int>.this[int].get""
  IL_0028:  ldind.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldloca.s   V_0
  IL_0030:  dup
  IL_0031:  call       ""int System.Span<int>.Length.get""
  IL_0036:  ldc.i4.2
  IL_0037:  sub
  IL_0038:  stloc.1
  IL_0039:  ldloc.1
  IL_003a:  call       ""ref int System.Span<int>.this[int].get""
  IL_003f:  dup
  IL_0040:  ldind.i4
  IL_0041:  call       ""void System.Console.WriteLine(int)""
  IL_0046:  ldloca.s   V_0
  IL_0048:  dup
  IL_0049:  call       ""int System.Span<int>.Length.get""
  IL_004e:  ldc.i4.2
  IL_004f:  sub
  IL_0050:  stloc.2
  IL_0051:  ldloc.2
  IL_0052:  call       ""ref int System.Span<int>.this[int].get""
  IL_0057:  ldc.i4.s   9
  IL_0059:  stind.i4
  IL_005a:  ldloca.s   V_0
  IL_005c:  dup
  IL_005d:  call       ""int System.Span<int>.Length.get""
  IL_0062:  ldc.i4.2
  IL_0063:  sub
  IL_0064:  stloc.2
  IL_0065:  ldloc.2
  IL_0066:  call       ""ref int System.Span<int>.this[int].get""
  IL_006b:  ldind.i4
  IL_006c:  call       ""void System.Console.WriteLine(int)""
  IL_0071:  ldind.i4
  IL_0072:  call       ""void System.Console.WriteLine(int)""
  IL_0077:  ret
}");
        }

        [Fact]
        public void PatternIndexAndRangeSpanChar()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        ReadOnlySpan<char> s = ""abcdefg"";
        Console.WriteLine(s[^2]);
        var index = ^1;
        Console.WriteLine(s[index]);
        s = s[^2..];
        Console.WriteLine(s[0]);
        Console.WriteLine(s[1]);
    }
}", TestOptions.ReleaseExe); ;
            var verifier = CompileAndVerify(comp, expectedOutput: @"f
g
f
g");
            verifier.VerifyIL(@"C.Main", @"
{
  // Code size      129 (0x81)
  .maxstack  3
  .locals init (System.ReadOnlySpan<char> V_0, //s
                System.Index V_1, //index
                int V_2,
                int V_3,
                System.ReadOnlySpan<char> V_4)
  IL_0000:  ldstr      ""abcdefg""
  IL_0005:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  dup
  IL_000e:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0013:  ldc.i4.2
  IL_0014:  sub
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_001c:  ldind.u2
  IL_001d:  call       ""void System.Console.WriteLine(char)""
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.1
  IL_0026:  call       ""System.Index..ctor(int, bool)""
  IL_002b:  ldloca.s   V_0
  IL_002d:  dup
  IL_002e:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0033:  stloc.2
  IL_0034:  ldloca.s   V_1
  IL_0036:  ldloc.2
  IL_0037:  call       ""int System.Index.GetOffset(int)""
  IL_003c:  stloc.3
  IL_003d:  ldloc.3
  IL_003e:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0043:  ldind.u2
  IL_0044:  call       ""void System.Console.WriteLine(char)""
  IL_0049:  ldloc.0
  IL_004a:  stloc.s    V_4
  IL_004c:  ldloca.s   V_4
  IL_004e:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0053:  dup
  IL_0054:  ldc.i4.2
  IL_0055:  sub
  IL_0056:  stloc.3
  IL_0057:  ldloc.3
  IL_0058:  sub
  IL_0059:  stloc.2
  IL_005a:  ldloca.s   V_4
  IL_005c:  ldloc.3
  IL_005d:  ldloc.2
  IL_005e:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.Slice(int, int)""
  IL_0063:  stloc.0
  IL_0064:  ldloca.s   V_0
  IL_0066:  ldc.i4.0
  IL_0067:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_006c:  ldind.u2
  IL_006d:  call       ""void System.Console.WriteLine(char)""
  IL_0072:  ldloca.s   V_0
  IL_0074:  ldc.i4.1
  IL_0075:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_007a:  ldind.u2
  IL_007b:  call       ""void System.Console.WriteLine(char)""
  IL_0080:  ret
}");
        }

        [Fact]
        public void PatternIndexAndRangeSpanInt()
        {
            var comp = CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s = new int[] { 2, 4, 5, 6 };
        Console.WriteLine(s[^2]);
        var index = ^1;
        Console.WriteLine(s[index]);
        s = s[^2..];
        Console.WriteLine(s[0]);
        Console.WriteLine(s[1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"5
6
5
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size      141 (0x8d)
  .maxstack  3
  .locals init (System.Span<int> V_0, //s
                System.Index V_1, //index
                int V_2,
                int V_3,
                System.Span<int> V_4)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=16 <PrivateImplementationDetails>.D033850C1A3F6F1209A6CD84146E8561DDC73C79""
  IL_000c:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  call       ""System.Span<int> System.Span<int>.op_Implicit(int[])""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  dup
  IL_001a:  call       ""int System.Span<int>.Length.get""
  IL_001f:  ldc.i4.2
  IL_0020:  sub
  IL_0021:  stloc.2
  IL_0022:  ldloc.2
  IL_0023:  call       ""ref int System.Span<int>.this[int].get""
  IL_0028:  ldind.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  ldloca.s   V_1
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.1
  IL_0032:  call       ""System.Index..ctor(int, bool)""
  IL_0037:  ldloca.s   V_0
  IL_0039:  dup
  IL_003a:  call       ""int System.Span<int>.Length.get""
  IL_003f:  stloc.2
  IL_0040:  ldloca.s   V_1
  IL_0042:  ldloc.2
  IL_0043:  call       ""int System.Index.GetOffset(int)""
  IL_0048:  stloc.3
  IL_0049:  ldloc.3
  IL_004a:  call       ""ref int System.Span<int>.this[int].get""
  IL_004f:  ldind.i4
  IL_0050:  call       ""void System.Console.WriteLine(int)""
  IL_0055:  ldloc.0
  IL_0056:  stloc.s    V_4
  IL_0058:  ldloca.s   V_4
  IL_005a:  call       ""int System.Span<int>.Length.get""
  IL_005f:  dup
  IL_0060:  ldc.i4.2
  IL_0061:  sub
  IL_0062:  stloc.3
  IL_0063:  ldloc.3
  IL_0064:  sub
  IL_0065:  stloc.2
  IL_0066:  ldloca.s   V_4
  IL_0068:  ldloc.3
  IL_0069:  ldloc.2
  IL_006a:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_006f:  stloc.0
  IL_0070:  ldloca.s   V_0
  IL_0072:  ldc.i4.0
  IL_0073:  call       ""ref int System.Span<int>.this[int].get""
  IL_0078:  ldind.i4
  IL_0079:  call       ""void System.Console.WriteLine(int)""
  IL_007e:  ldloca.s   V_0
  IL_0080:  ldc.i4.1
  IL_0081:  call       ""ref int System.Span<int>.this[int].get""
  IL_0086:  ldind.i4
  IL_0087:  call       ""void System.Console.WriteLine(int)""
  IL_008c:  ret
}");
        }

        [Fact]
        public void RealIndexersPreferredToPattern()
        {
            var src = @"
using System;
class C
{
    public int Length => throw null;
    public int this[Index i, int j = 0] { get { Console.WriteLine(""Index""); return 0; } }
    public int this[int i] { get { Console.WriteLine(""int""); return 0; } }    
    public int Slice(int i, int j) => throw null;
    public int this[Range r, int j = 0] { get { Console.WriteLine(""Range""); return 0; } }

    static void Main()
    {
        var c = new C();
        _ = c[0];
        _ = c[^0];
        _ = c[0..];
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: @"
int
Index
Range");
        }

        [Fact]
        public void PatternIndexList()
        {
            var src = @"
using System;
using System.Collections.Generic;
class C
{
    private static List<int> list = new List<int>() { 2, 4, 5, 6 };
    static void Main()
    {
        Console.WriteLine(list[^2]);
        var index = ^1;
        Console.WriteLine(list[index]);
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, expectedOutput: @"5
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (System.Index V_0, //index
                int V_1,
                int V_2)
  IL_0000:  ldsfld     ""System.Collections.Generic.List<int> C.list""
  IL_0005:  dup
  IL_0006:  callvirt   ""int System.Collections.Generic.List<int>.Count.get""
  IL_000b:  ldc.i4.2
  IL_000c:  sub
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  callvirt   ""int System.Collections.Generic.List<int>.this[int].get""
  IL_0014:  call       ""void System.Console.WriteLine(int)""
  IL_0019:  ldloca.s   V_0
  IL_001b:  ldc.i4.1
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""System.Index..ctor(int, bool)""
  IL_0022:  ldsfld     ""System.Collections.Generic.List<int> C.list""
  IL_0027:  dup
  IL_0028:  callvirt   ""int System.Collections.Generic.List<int>.Count.get""
  IL_002d:  stloc.1
  IL_002e:  ldloca.s   V_0
  IL_0030:  ldloc.1
  IL_0031:  call       ""int System.Index.GetOffset(int)""
  IL_0036:  stloc.2
  IL_0037:  ldloc.2
  IL_0038:  callvirt   ""int System.Collections.Generic.List<int>.this[int].get""
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ret
}");
        }

        [Theory]
        [InlineData("Length")]
        [InlineData("Count")]
        public void PatternRangeIndexers(string propertyName)
        {
            var src = @"
using System;
class C
{
    private int[] _f = { 2, 4, 5, 6 };
    public int " + propertyName + @" => _f.Length;
    public int[] Slice(int start, int length) => _f[start..length];
    static void Main()
    {
        var c = new C();
        foreach (var x in c[1..])
        {
            Console.WriteLine(x);
        }
        foreach (var x in c[..^2])
        {
            Console.WriteLine(x);
        }
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, @"
4
5
2
4");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       95 (0x5f)
    .maxstack  3
    .locals init (C V_0, //c
                  int[] V_1,
                  int V_2,
                  int V_3,
                  int V_4)
    IL_0000:  newobj     ""C..ctor()""
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  dup
    IL_0008:  callvirt   ""int C." + propertyName + @".get""
    IL_000d:  ldc.i4.1
    IL_000e:  stloc.3
    IL_000f:  ldloc.3
    IL_0010:  sub
    IL_0011:  stloc.s    V_4
    IL_0013:  ldloc.3
    IL_0014:  ldloc.s    V_4
    IL_0016:  callvirt   ""int[] C.Slice(int, int)""
    IL_001b:  stloc.1
    IL_001c:  ldc.i4.0
    IL_001d:  stloc.2
    IL_001e:  br.s       IL_002c
    IL_0020:  ldloc.1
    IL_0021:  ldloc.2
    IL_0022:  ldelem.i4
    IL_0023:  call       ""void System.Console.WriteLine(int)""
    IL_0028:  ldloc.2
    IL_0029:  ldc.i4.1
    IL_002a:  add
    IL_002b:  stloc.2
    IL_002c:  ldloc.2
    IL_002d:  ldloc.1
    IL_002e:  ldlen
    IL_002f:  conv.i4
    IL_0030:  blt.s      IL_0020
    IL_0032:  ldloc.0
    IL_0033:  dup
    IL_0034:  callvirt   ""int C." + propertyName + @".get""
    IL_0039:  ldc.i4.2
    IL_003a:  sub
    IL_003b:  ldc.i4.0
    IL_003c:  sub
    IL_003d:  stloc.s    V_4
    IL_003f:  ldc.i4.0
    IL_0040:  ldloc.s    V_4
    IL_0042:  callvirt   ""int[] C.Slice(int, int)""
    IL_0047:  stloc.1
    IL_0048:  ldc.i4.0
    IL_0049:  stloc.2
    IL_004a:  br.s       IL_0058
    IL_004c:  ldloc.1
    IL_004d:  ldloc.2
    IL_004e:  ldelem.i4
    IL_004f:  call       ""void System.Console.WriteLine(int)""
    IL_0054:  ldloc.2
    IL_0055:  ldc.i4.1
    IL_0056:  add
    IL_0057:  stloc.2
    IL_0058:  ldloc.2
    IL_0059:  ldloc.1
    IL_005a:  ldlen
    IL_005b:  conv.i4
    IL_005c:  blt.s      IL_004c
    IL_005e:  ret
}
");
        }

        [Theory]
        [InlineData("Length")]
        [InlineData("Count")]
        public void PatternIndexIndexers(string propertyName)
        {
            var src = @"
using System;
class C
{
    private int[] _f = { 2, 4, 5, 6 };
    public int " + propertyName + @" => _f.Length;
    public int this[int x] => _f[x];
    static void Main()
    {
        var c = new C();
        Console.WriteLine(c[0]);
        Console.WriteLine(c[^1]);
    }
}";
            var verifier = CompileAndVerifyWithIndexAndRange(src, @"
2
6");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.0
  IL_0007:  callvirt   ""int C.this[int].get""
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  dup
  IL_0012:  callvirt   ""int C." + propertyName + @".get""
  IL_0017:  ldc.i4.1
  IL_0018:  sub
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""int C.this[int].get""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  ret
}");
        }

        [Fact]
        public void RefToArrayIndexIndexer()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        int[] x = { 0, 1, 2, 3 };
        M(x);
    }

    static void M(int[] x)
    {
        ref int r1 = ref x[2];
        Console.WriteLine(r1);
        ref int r2 = ref x[^2];
        Console.WriteLine(r2);
        r2 = 7;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
        r1 = 5;
        Console.WriteLine(r1);
        Console.WriteLine(r2);
    }
}", expectedOutput: @"2
2
7
7
5
5");
            verifier.VerifyIL("C.M", @"
{
  // Code size       82 (0x52)
  .maxstack  4
  .locals init (int& V_0, //r2
                int[] V_1,
                System.Index V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  ldelema    ""int""
  IL_0007:  dup
  IL_0008:  ldind.i4
  IL_0009:  call       ""void System.Console.WriteLine(int)""
  IL_000e:  ldarg.0
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.1
  IL_0013:  newobj     ""System.Index..ctor(int, bool)""
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  ldloc.1
  IL_001c:  ldlen
  IL_001d:  conv.i4
  IL_001e:  call       ""int System.Index.GetOffset(int)""
  IL_0023:  ldelema    ""int""
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ldind.i4
  IL_002b:  call       ""void System.Console.WriteLine(int)""
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.7
  IL_0032:  stind.i4
  IL_0033:  dup
  IL_0034:  ldind.i4
  IL_0035:  call       ""void System.Console.WriteLine(int)""
  IL_003a:  ldloc.0
  IL_003b:  ldind.i4
  IL_003c:  call       ""void System.Console.WriteLine(int)""
  IL_0041:  dup
  IL_0042:  ldc.i4.5
  IL_0043:  stind.i4
  IL_0044:  ldind.i4
  IL_0045:  call       ""void System.Console.WriteLine(int)""
  IL_004a:  ldloc.0
  IL_004b:  ldind.i4
  IL_004c:  call       ""void System.Console.WriteLine(int)""
  IL_0051:  ret
}");
        }

        [Fact]
        public void RangeIndexerStringIsFromEndStart()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcdef"";
        Console.WriteLine(s[^2..]);
    }
}", expectedOutput: "ef");
        }

        [Fact]
        public void FakeRangeIndexerStringBothIsFromEnd()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        string s = ""abcdef"";
        Console.WriteLine(s[^4..^1]);
    }
}", expectedOutput: "cde");
        }

        [Fact]
        public void IndexIndexerStringTwoArgs()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        M(s);
    }
    public static void M(string s)
    {
        Console.WriteLine(s[new Index(1, false)]);
        Console.WriteLine(s[new Index(1, false), ^1]);
    }
}");
            comp.VerifyDiagnostics(
                // (13,27): error CS1501: No overload for method 'this' takes 2 arguments
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_BadArgCount, "s[new Index(1, false), ^1]").WithArguments("this", "2").WithLocation(13, 27));
        }

        [Fact]
        public void IndexIndexerArrayTwoArgs()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var x = new int[1,1];
        M(x);
    }
    public static void M(int[,] s)
    {
        Console.WriteLine(s[new Index(1, false), ^1]);
    }
}");
            comp.VerifyDiagnostics(
                // (12,27): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s[new Index(1, false), ^1]").WithArguments("System.Index", "int").WithLocation(12, 27),
                // (12,27): error CS0029: Cannot implicitly convert type 'System.Index' to 'int'
                //         Console.WriteLine(s[new Index(1, false), ^1]);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s[new Index(1, false), ^1]").WithArguments("System.Index", "int").WithLocation(12, 27));
        }

        [Fact]
        public void FakeIndexIndexerString()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        Console.WriteLine(s[new Index(1, false)]);
        Console.WriteLine(s[(Index)2]);
        Console.WriteLine(s[^1]);
    }
}", expectedOutput: @"b
c
f");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       76 (0x4c)
    .maxstack  4
    .locals init (int V_0,
                  int V_1,
                  System.Index V_2)
    IL_0000:  ldstr      ""abcdef""
    IL_0005:  dup
    IL_0006:  dup
    IL_0007:  callvirt   ""int string.Length.get""
    IL_000c:  stloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  ldc.i4.0
    IL_000f:  newobj     ""System.Index..ctor(int, bool)""
    IL_0014:  stloc.2
    IL_0015:  ldloca.s   V_2
    IL_0017:  ldloc.0
    IL_0018:  call       ""int System.Index.GetOffset(int)""
    IL_001d:  stloc.1
    IL_001e:  ldloc.1
    IL_001f:  callvirt   ""char string.this[int].get""
    IL_0024:  call       ""void System.Console.WriteLine(char)""
    IL_0029:  dup
    IL_002a:  ldc.i4.2
    IL_002b:  stloc.1
    IL_002c:  ldloc.1
    IL_002d:  callvirt   ""char string.this[int].get""
    IL_0032:  call       ""void System.Console.WriteLine(char)""
    IL_0037:  dup
    IL_0038:  callvirt   ""int string.Length.get""
    IL_003d:  ldc.i4.1
    IL_003e:  sub
    IL_003f:  stloc.1
    IL_0040:  ldloc.1
    IL_0041:  callvirt   ""char string.this[int].get""
    IL_0046:  call       ""void System.Console.WriteLine(char)""
    IL_004b:  ret
}");
        }

        [Fact]
        public void FakeRangeIndexerString()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        Console.WriteLine(s[1..3]);
    }
}", expectedOutput: "bc");
            verifier.VerifyIL("C.Main", @"
{
    // Code size       24 (0x18)
    .maxstack  3
    .locals init (int V_0,
                  int V_1)
    IL_0000:  ldstr      ""abcdef""
    IL_0005:  ldc.i4.1
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.3
    IL_0008:  ldloc.0
    IL_0009:  sub
    IL_000a:  stloc.1
    IL_000b:  ldloc.0
    IL_000c:  ldloc.1
    IL_000d:  callvirt   ""string string.Substring(int, int)""
    IL_0012:  call       ""void System.Console.WriteLine(string)""
    IL_0017:  ret
}");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenEnd()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        var result = M(s);
        Console.WriteLine(result);
    }
    public static string M(string s) => s[1..];
}", expectedOutput: "bcdef");
        }

        [Fact]
        public void FakeRangeIndexerStringOpenStart()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var s = ""abcdef"";
        var result = M(s);
        Console.WriteLine(result);
    }
    public static string M(string s) => s[..^2];
}", expectedOutput: "abcd");
        }

        [Fact]
        public void FakeIndexIndexerArray()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
class C
{
    public static void Main()
    {
        var x = new[] { 1, 2, 3, 11 };
        M(x);
    }

    public static void M(int[] array)
    {
        Console.WriteLine(array[new Index(1, false)]);
        Console.WriteLine(array[^1]);
    }
}", TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: @"2
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (int[] V_0,
                System.Index V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.0
  IL_0005:  newobj     ""System.Index..ctor(int, bool)""
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldloc.0
  IL_000e:  ldlen
  IL_000f:  conv.i4
  IL_0010:  call       ""int System.Index.GetOffset(int)""
  IL_0015:  ldelem.i4
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  ldarg.0
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.1
  IL_0020:  newobj     ""System.Index..ctor(int, bool)""
  IL_0025:  stloc.1
  IL_0026:  ldloca.s   V_1
  IL_0028:  ldloc.0
  IL_0029:  ldlen
  IL_002a:  conv.i4
  IL_002b:  call       ""int System.Index.GetOffset(int)""
  IL_0030:  ldelem.i4
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void SuppressNullableWarning_FakeIndexIndexerArray()
        {
            string source = @"
using System;
class C
{
    public static void Main()
    {
        var x = new[] { 1, 2, 3, 11 };
        M(x);
    }

    public static void M(int[] array)
    {
        Console.Write(array[new Index(1, false)!]);
        Console.Write(array[(^1)!]);
    }
}";
            // cover case in ConvertToArrayIndex
            var comp = CreateCompilationWithIndex(source, WithNonNullTypesTrue(TestOptions.DebugExe));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "211");
        }

        [Fact]
        public void FakeRangeIndexerArray()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[1..3];
}", expectedOutput: @"2
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  ldc.i4.3
  IL_0008:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_000d:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0012:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0017:  ret
}
");
        }

        [Fact]
        public void FakeRangeStartIsFromEndIndexerArray()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[^2..];
}", expectedOutput: @"2
3
11");
        }

        [Fact]
        public void FakeRangeBothIsFromEndIndexerArray()
        {
            CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[^3..^1];
}", expectedOutput: @"2
2
3");
        }

        [Fact]
        public void FakeRangeToEndIndexerArray()
        {
            var verifier = CompileAndVerifyWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[1..];
}", expectedOutput: @"3
2
3
11");
            verifier.VerifyIL("C.M", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_000c:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void FakeRangeFromStartIndexerArray()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
class C
{
    public static void Main()
    {
        var arr = new[] { 1, 2, 3, 11 };
        var result = M(arr);
        Console.WriteLine(result.Length);
        foreach (var x in result)
        {
            Console.WriteLine(x);
        }
    }
    public static int[] M(int[] array) => array[..3];
}" + TestSources.GetSubArray, TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, verify: Verification.Passes, expectedOutput: @"3
1
2
3");
            verifier.VerifyIL("C.M", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  call       ""System.Index System.Index.op_Implicit(int)""
  IL_0007:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_000c:  call       ""int[] System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<int>(int[], System.Range)""
  IL_0011:  ret
}");
        }

        [Fact]
        public void LowerIndex_Int()
        {
            var compilation = CreateCompilationWithIndex(@"
using System;
public static class Util
{
    public static Index Convert(int a) => ^a;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Convert", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void LowerIndex_NullableInt()
        {
            var compilation = CreateCompilationWithIndex(@"
using System;
public static class Util
{
    public static Index? Convert(int? a) => ^a;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Convert", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int? V_0,
                System.Index? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool int?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Index?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""int int?.GetValueOrDefault()""
  IL_001c:  ldc.i4.1
  IL_001d:  newobj     ""System.Index..ctor(int, bool)""
  IL_0022:  newobj     ""System.Index?..ctor(System.Index)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void PrintIndexExpressions()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        int nonNullable = 1;
        int? nullableValue = 2;
        int? nullableDefault = default;

        Index a = nonNullable;
        Console.WriteLine(""a: "" + Print(a));

        Index b = ^nonNullable;
        Console.WriteLine(""b: "" + Print(b));

        // --------------------------------------------------------
        
        Index? c = nullableValue;
        Console.WriteLine(""c: "" + Print(c));

        Index? d = ^nullableValue;
        Console.WriteLine(""d: "" + Print(d));

        // --------------------------------------------------------
        
        Index? e = nullableDefault;
        Console.WriteLine(""e: "" + Print(e));

        Index? f = ^nullableDefault;
        Console.WriteLine(""f: "" + Print(f));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"
a: value: '1', fromEnd: 'False'
b: value: '1', fromEnd: 'True'
c: value: '2', fromEnd: 'False'
d: value: '2', fromEnd: 'True'
e: default
f: default");
        }

        [Fact]
        public void LowerRange_Create_Index_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range Create(Index start, Index end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_Index_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index start, Index? end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_1
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloca.s   V_2
  IL_000f:  initobj    ""System.Range?""
  IL_0015:  ldloc.2
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001f:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0024:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_NullableIndex_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index? start, Index end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (System.Index? V_0,
                System.Index V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  brtrue.s   IL_0017
  IL_000d:  ldloca.s   V_2
  IL_000f:  initobj    ""System.Range?""
  IL_0015:  ldloc.2
  IL_0016:  ret
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001e:  ldloc.1
  IL_001f:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0024:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0029:  ret
}");
        }

        [Fact]
        public void LowerRange_Create_NullableIndex_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? Create(Index? start, Index? end) => start..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index? V_0,
                System.Index? V_1,
                System.Range? V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldarg.1
  IL_0003:  stloc.1
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""bool System.Index?.HasValue.get""
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""bool System.Index?.HasValue.get""
  IL_0012:  and
  IL_0013:  brtrue.s   IL_001f
  IL_0015:  ldloca.s   V_2
  IL_0017:  initobj    ""System.Range?""
  IL_001d:  ldloc.2
  IL_001e:  ret
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_002d:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0032:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0037:  ret
}");
        }

        [Fact]
        public void LowerRange_ToEnd_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range ToEnd(Index end) => ..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.ToEnd", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void LowerRange_ToEnd_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? ToEnd(Index? end) => ..end;
}");

            CompileAndVerify(compilation).VerifyIL("Util.ToEnd", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.Index? V_0,
                System.Range? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.Index?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Range?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001c:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_0021:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void LowerRange_FromStart_Index()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range FromStart(Index start) => start..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.FromStart", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_0006:  ret
}");
        }

        [Fact]
        public void LowerRange_FromStart_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range? FromStart(Index? start) => start..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.FromStart", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (System.Index? V_0,
                System.Range? V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       ""bool System.Index?.HasValue.get""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""System.Range?""
  IL_0013:  ldloc.1
  IL_0014:  ret
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_001c:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_0021:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0026:  ret
}");
        }

        [Fact]
        public void LowerRange_All()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    public static Range All() => ..;
}");

            CompileAndVerify(compilation).VerifyIL("Util.All", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""System.Range System.Range.All.get""
  IL_0005:  ret
}");
        }

        [Fact]
        public void PrintRangeExpressions()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Index nonNullable = 1;
        Index? nullableValue = 2;
        Index? nullableDefault = default;

        Range a = nonNullable..nonNullable;
        Console.WriteLine(""a: "" + Print(a));

        Range? b = nonNullable..nullableValue;
        Console.WriteLine(""b: "" + Print(b));

        Range? c = nonNullable..nullableDefault;
        Console.WriteLine(""c: "" + Print(c));

        // --------------------------------------------------------

        Range? d = nullableValue..nonNullable;
        Console.WriteLine(""d: "" + Print(d));

        Range? e = nullableValue..nullableValue;
        Console.WriteLine(""e: "" + Print(e));

        Range? f = nullableValue..nullableDefault;
        Console.WriteLine(""f: "" + Print(f));

        // --------------------------------------------------------

        Range? g = nullableDefault..nonNullable;
        Console.WriteLine(""g: "" + Print(g));

        Range? h = nullableDefault..nullableValue;
        Console.WriteLine(""h: "" + Print(h));

        Range? i = nullableDefault..nullableDefault;
        Console.WriteLine(""i: "" + Print(i));

        // --------------------------------------------------------

        Range? j = ..nonNullable;
        Console.WriteLine(""j: "" + Print(j));

        Range? k = ..nullableValue;
        Console.WriteLine(""k: "" + Print(k));

        Range? l = ..nullableDefault;
        Console.WriteLine(""l: "" + Print(l));

        // --------------------------------------------------------

        Range? m = nonNullable..;
        Console.WriteLine(""m: "" + Print(m));

        Range? n = nullableValue..;
        Console.WriteLine(""n: "" + Print(n));

        Range? o = nullableDefault..;
        Console.WriteLine(""o: "" + Print(o));

        // --------------------------------------------------------

        Range? p = ..;
        Console.WriteLine(""p: "" + Print(p));

    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"
a: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
b: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
c: default
d: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
e: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
f: default
g: default
h: default
i: default
j: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'False''
k: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '2', fromEnd: 'False''
l: default
m: value: 'value: '1', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
n: value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
o: default
p: value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''");
        }

        [Fact]
        public void PassingAsArguments()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(^1));
        Console.WriteLine(Print(..));
        Console.WriteLine(Print(2..));
        Console.WriteLine(Print(..3));
        Console.WriteLine(Print(4..5));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: @"
value: '1', fromEnd: 'True'
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '2', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''
value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '3', fromEnd: 'False''
value: 'value: '4', fromEnd: 'False'', fromEnd: 'value: '5', fromEnd: 'False''");
        }

        [Fact]
        public void LowerRange_OrderOfEvaluation_Index_NullableIndex()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    static void Main()
    {
        var x = Create();
    }

    public static Range? Create()
    {
        return GetIndex1() .. GetIndex2();
    }

    static Index GetIndex1()
    {
        System.Console.WriteLine(""1"");
        return default;
    }

    static Index? GetIndex2()
    {
        System.Console.WriteLine(""2"");
        return new Index(1, true);
    }
}", options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: @"
1
2").VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2,
                System.Range? V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.Index Util.GetIndex1()""
  IL_0006:  stloc.0
  IL_0007:  call       ""System.Index? Util.GetIndex2()""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool System.Index?.HasValue.get""
  IL_0014:  brtrue.s   IL_0021
  IL_0016:  ldloca.s   V_2
  IL_0018:  initobj    ""System.Range?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_0033
  IL_0021:  ldloc.0
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0029:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0033:  stloc.3
  IL_0034:  br.s       IL_0036
  IL_0036:  ldloc.3
  IL_0037:  ret
}");
        }

        [Fact]
        public void LowerRange_OrderOfEvaluation_Index_Null()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
public static class Util
{
    static void Main()
    {
        var x = Create();
    }

    public static Range? Create()
    {
        return GetIndex1() .. GetIndex2();
    }

    static Index GetIndex1()
    {
        System.Console.WriteLine(""1"");
        return default;
    }

    static Index? GetIndex2()
    {
        System.Console.WriteLine(""2"");
        return null;
    }
}", options: TestOptions.DebugExe);

            CompileAndVerify(compilation, expectedOutput: @"
1
2").VerifyIL("Util.Create", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.Index V_0,
                System.Index? V_1,
                System.Range? V_2,
                System.Range? V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.Index Util.GetIndex1()""
  IL_0006:  stloc.0
  IL_0007:  call       ""System.Index? Util.GetIndex2()""
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       ""bool System.Index?.HasValue.get""
  IL_0014:  brtrue.s   IL_0021
  IL_0016:  ldloca.s   V_2
  IL_0018:  initobj    ""System.Range?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_0033
  IL_0021:  ldloc.0
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""System.Index System.Index?.GetValueOrDefault()""
  IL_0029:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_002e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0033:  stloc.3
  IL_0034:  br.s       IL_0036
  IL_0036:  ldloc.3
  IL_0037:  ret
}");
        }

        [Fact]
        public void Index_OperandConvertibleToInt()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        byte a = 3;
        Index b = ^a;
        Console.WriteLine(Print(b));
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe), expectedOutput: "value: '3', fromEnd: 'True'");
        }

        [Fact]
        public void Index_NullableAlwaysHasValue()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Index? Create()
    {
        // should be lowered into: new Nullable<Index>(new Index(5, fromEnd: true))
        return ^new Nullable<int>(5);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: '5', fromEnd: 'True'")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  newobj     ""System.Index?..ctor(System.Index)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Left()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^1)));
    }
    static Range? Create(Index arg)
    {
        // should be lowered into: new Nullable<Range>(Range.FromStart(arg))
        return new Nullable<Index>(arg)..;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '0', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_0006:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Right()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^1)));
    }
    static Range? Create(Index arg)
    {
        // should be lowered into: new Nullable<Range>(Range.ToEnd(arg))
        return ..new Nullable<Index>(arg);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '1', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_0006:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000b:  ret
}");
        }

        [Fact]
        public void Range_NullableAlwaysHasValue_Both()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(^2, ^1)));
    }
    static Range? Create(Index arg1, Index arg2)
    {
        // should be lowered into: new Nullable<Range>(Range.Create(arg1, arg2))
        return new Nullable<Index>(arg1)..new Nullable<Index>(arg2);
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '2', fromEnd: 'True'', fromEnd: 'value: '1', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0007:  newobj     ""System.Range?..ctor(System.Range)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Index_NullableNeverHasValue()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Index? Create()
    {
        // should be lowered into: new Nullable<Index>(new Index(default, fromEnd: true))
        return ^new Nullable<int>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe), expectedOutput: "value: '0', fromEnd: 'True'")
                .VerifyIL("Program.Create", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.Index..ctor(int, bool)""
  IL_0007:  newobj     ""System.Index?..ctor(System.Index)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverhasValue_Left()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.FromStart(default))
        return new Nullable<Index>()..;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'True''")
                .VerifyIL("Program.Create", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  call       ""System.Range System.Range.StartAt(System.Index)""
  IL_000e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverHasValue_Right()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.ToEnd(default))
        return ..new Nullable<Index>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'False''")
                .VerifyIL("Program.Create", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  call       ""System.Range System.Range.EndAt(System.Index)""
  IL_000e:  newobj     ""System.Range?..ctor(System.Range)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void Range_NullableNeverHasValue_Both()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create()));
    }
    static Range? Create()
    {
        // should be lowered into: new Nullable<Range>(Range.Create(default, default))
        return new Nullable<Index>()..new Nullable<Index>();
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '0', fromEnd: 'False'', fromEnd: 'value: '0', fromEnd: 'False''")
                .VerifyIL("Program.Create", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (System.Index V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Index""
  IL_0008:  ldloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""System.Index""
  IL_0011:  ldloc.0
  IL_0012:  newobj     ""System.Range..ctor(System.Index, System.Index)""
  IL_0017:  newobj     ""System.Range?..ctor(System.Range)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void Index_OnFunctionCall()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(^Create(5)));
    }
    static int Create(int x) => x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: "value: '5', fromEnd: 'True'");
        }

        [Fact]
        public void Range_OnFunctionCall()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(1)..Create(2)));
    }
    static Index Create(int x) => ^x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''");
        }

        [Fact]
        public void Index_OnAssignment()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        int x = default;
        Console.WriteLine(Print(^(x = Create(5))));
        Console.WriteLine(x);
    }
    static int Create(int x) => x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: @"
value: '5', fromEnd: 'True'
5");
        }

        [Fact]
        public void Range_OnAssignment()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Index x = default, y = default;
        Console.WriteLine(Print((x = Create(1))..(y = Create(2))));
        Console.WriteLine(Print(x));
        Console.WriteLine(Print(y));
    }
    static Index Create(int x) => ^x;
}" + PrintIndexesAndRangesCode,
                options: TestOptions.ReleaseExe),
                expectedOutput: @"
value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''
value: '1', fromEnd: 'True'
value: '2', fromEnd: 'True'");
        }

        [Fact]
        public void Range_OnVarOut()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        Console.WriteLine(Print(Create(1, out Index y)..y));
    }
    static Index Create(int x, out Index y)
    {
        y = ^2;
        return ^x;
    }
}" + PrintIndexesAndRangesCode, options: TestOptions.ReleaseExe),
                expectedOutput: "value: 'value: '1', fromEnd: 'True'', fromEnd: 'value: '2', fromEnd: 'True''");
        }

        [Fact]
        public void Range_EvaluationInCondition()
        {
            CompileAndVerify(CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        if ((Create(1, out int a)..Create(2, out int b)).Start.IsFromEnd && a < b)
        {
            Console.WriteLine(""YES"");
        }
        if ((Create(4, out int c)..Create(3, out int d)).Start.IsFromEnd && c < d)
        {
            Console.WriteLine(""NO"");
        }
    }
    static Index Create(int x, out int y)
    {
        y = x;
        return ^x;
    }
}", options: TestOptions.ReleaseExe), expectedOutput: "YES");
        }


        private const string PrintIndexesAndRangesCode = @"
partial class Program
{
    static string Print(Index arg)
    {
        return $""value: '{arg.Value}', fromEnd: '{arg.IsFromEnd}'"";
    }
    static string Print(Range arg)
    {
        return $""value: '{Print(arg.Start)}', fromEnd: '{Print(arg.End)}'"";
    }
    static string Print(Index? arg)
    {
        if (arg.HasValue)
        {
            return Print(arg.Value);
        }
        else
        {
            return ""default"";
        }
    }
    static string Print(Range? arg)
    {
        if (arg.HasValue)
        {
            return Print(arg.Value);
        }
        else
        {
            return ""default"";
        }
    }
}";
    }
}
