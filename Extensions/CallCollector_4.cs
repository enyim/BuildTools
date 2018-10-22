/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build
{
	public class CallCollector
	{
		private static readonly List<IILTransform> ILTransforms = CSharpDecompiler.GetILTransforms();
		private readonly CSharpDecompiler decompiler;
		private readonly IDecompilerTypeSystem typeSystem;
		private readonly ILReader ilReader;

		public CallCollector(Mono.Cecil.ModuleDefinition module)
		{
			decompiler = new CSharpDecompiler(module.FileName, new ICSharpCode.Decompiler.DecompilerSettings());
			typeSystem = decompiler.TypeSystem;
			ilReader = new ILReader(typeSystem.MainModule);
		}

		public CallInfo[] Collect(Mono.Cecil.MethodDefinition method, Func<CallInstruction, bool> filter = null)
		{
			var body = method.Body;

			var declaringFullName = method.DeclaringType.FullName.Replace("/", "+");
			var icsType = typeSystem.FindType(new FullTypeName(declaringFullName)).GetDefinition();
			var expectedName = declaringFullName.Replace("+", ".") + "." + method.Name;
			var icsMethod = icsType.Methods.First(m => m.FullName == expectedName);

			var icsHandle = (MethodDefinitionHandle)icsMethod.MetadataToken;
			var icsDef = typeSystem.MainModule.PEFile.Metadata.GetMethodDefinition(icsHandle);
			var icsBody = typeSystem.MainModule.PEFile.Reader.GetMethodBody(icsDef.RelativeVirtualAddress);

			var function = ilReader.ReadIL(icsHandle, icsBody);
			var transformContext = new ILTransformContext(function, typeSystem, decompiler.DebugInfoProvider);

			foreach (var t in ILTransforms)
				t.Run(function, transformContext);

			var collector = new BlockVisitor(filter);
			function.AcceptVisitor(collector);

			if (collector.CallStarts.Count > 0)
			{
				var opsByOffset = body.Instructions.ToDictionary(instr => instr.Offset);
				var retval = new CallInfo[collector.CallStarts.Count];

				var i = 0;
				foreach (var (blockStart, callOffset) in collector.CallStarts)
				{
					retval[i++] = new CallInfo(opsByOffset[blockStart], opsByOffset[callOffset]);
				}

				return retval;
			}

			return new CallInfo[0];
		}

		private class BlockVisitor : ILVisitor
		{
			private readonly Func<CallInstruction, bool> filter;
			public readonly List<(int blockStart, int callOffset)> CallStarts;

			public BlockVisitor(Func<CallInstruction, bool> filter)
			{
				this.filter = filter;
				CallStarts = new List<(int start, int end)>();
			}

			private void MapCall(CallInstruction call)
			{
				if (filter != null && !filter(call)) return;

				var args = call.Arguments;
				var callOffset = call.ILRange.Start;
				var blockStart = args.Count == 0
									? callOffset
									: call.Descendants
											.Where(d => !d.ILRange.IsEmpty)
											.Min(d => d.ILRange.Start);

				CallStarts.Add((blockStart, callOffset));
			}

			protected override void Default(ILInstruction inst)
			{
				foreach (var child in inst.Children)
					child.AcceptVisitor(this);
			}

			protected override void VisitCall(Call inst) => MapCall(inst);
			protected override void VisitCallVirt(CallVirt inst) => MapCall(inst);
		}

		public struct CallInfo
		{
			public readonly Instruction StartsAt;
			public readonly Instruction Call;

			public CallInfo(Instruction startsAt, Instruction call)
			{
				StartsAt = startsAt;
				Call = call;
			}
		}
	}
}
*/
#region [ License information          ]

/* ************************************************************
 *
 *    Copyright (c) Attila Kiskó, enyim.com
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
