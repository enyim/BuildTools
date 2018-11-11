using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.ControlFlow;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.LogTo
{
	public class CallCollector
	{
		private static readonly List<IILTransform> ILTransforms = CSharpDecompiler.GetILTransforms();

		private readonly DecompilerTypeSystem typeSystem;
		private readonly ILReader ilReader;
		private readonly CSharpDecompiler decompiler;

		public CallCollector(Mono.Cecil.ModuleDefinition module)
		{
			typeSystem = new DecompilerTypeSystem(module);
			ilReader = new ILReader(typeSystem);
			decompiler = new CSharpDecompiler(typeSystem, new ICSharpCode.Decompiler.DecompilerSettings());
		}

		public static CallInfo[] For(System.Reflection.MethodInfo method, Func<string, bool> filter = null)
		{
			var module = Mono.Cecil.ModuleDefinition.ReadModule(method.DeclaringType.Assembly.Location, new Mono.Cecil.ReaderParameters { InMemory = true });
			var retval = new CallCollector(module);
			var md = module.FindType(method.DeclaringType.FullName).Resolve().FindMethod(method.Name, method.GetParameters().Select(p => module.ImportReference(p.ParameterType)).ToArray());

			return retval.Collect(md.Resolve(), i => filter(i.TargetMethod().FullName));
		}

		public CallInfo[] Collect(Mono.Cecil.MethodDefinition method, Func<Instruction, bool> filter = null)
		{
			//if (method.Name != "SimpleLogFromTryCatch"
			//	&& method.Name != "EnqueueNoOpIfNeeded") return new CallInfo[0];

			if (!method.HasBody) return new CallInfo[0];

			var body = method.Body;
			if (method.Body.Instructions.Any(i => i.Is(OpCodes.Call, OpCodes.Callvirt) && filter(i)))
			{
				var function = ilReader.ReadIL(body);
				var opsByOffset = body.Instructions.ToDictionary(instr => instr.Offset);

				var transformContext = decompiler.CreateILTransformContext(function);
				//foreach (var t in ILTransforms)
				//{
				//				t.Run(function, transformContext);
				//}

				var collector = new BlockVisitor(function, i =>
				{
					var range = i.ILRange;
					var op = opsByOffset[range.Start];
					while (!op.Is(OpCodes.Call, OpCodes.Callvirt))
					{
						op = op.Next;
						if (op.Offset > range.End) return false;
					}

					return filter(op);
				});

				function.AcceptVisitor(collector);

				if (collector.CallStarts.Count > 0)
				{
					var retval = new CallInfo[collector.CallStarts.Count];

					var i = 0;
					foreach (var (blockStart, callOffset) in collector.CallStarts)
					{
						retval[i++] = new CallInfo(opsByOffset[blockStart], opsByOffset[callOffset]);
					}

					return retval;
				}
			}

			return new CallInfo[0];
		}

		private class BlockVisitor : ILVisitor
		{
			private readonly Func<CallInstruction, bool> filter;
			public readonly List<(int blockStart, int callOffset)> CallStarts;
			private readonly ILFunction function;

			public BlockVisitor(ILFunction function, Func<CallInstruction, bool> filter)
			{
				this.filter = filter;
				CallStarts = new List<(int start, int end)>();
				this.function = function;
			}

			private void MapCall(CallInstruction call)
			{
				if (filter != null && !filter(call)) return;

				var args = call.Arguments;
				var callOffset = call.ILRange.Start;

				var blockStart = args.Count == 0
									? callOffset
									: WalkVariables(call)// call.Descendants
											.Where(d => !d.ILRange.IsEmpty)
											.Min(d => d.ILRange.Start);

				CallStarts.Add((blockStart, callOffset));
			}

			private IEnumerable<ILInstruction> WalkVariables(ILInstruction root)
			{
				var ops = new Queue<ILInstruction>();
				var visited = new HashSet<ILInstruction>();
				ops.Enqueue(root);

				while (ops.Count > 0)
				{
					var current = ops.Dequeue();
					if (visited.Add(current))
					{
						yield return current;

						if (current.MatchLdLoc(out var variable))
						{
							if (variable.Kind == VariableKind.StackSlot)
							{
								foreach (var st in variable.StoreInstructions)
									ops.Enqueue(st as ILInstruction);
							}
						}
						else
						{
							foreach (var c in current.Children)
							{
								ops.Enqueue(c);
							}
						}
					}
				}
			}

			protected override void Default(ILInstruction inst)
			{
				foreach (var child in inst.Children)
					child.AcceptVisitor(this);
			}

			protected override void VisitCall(Call inst) => MapCall(inst);
			protected override void VisitCallVirt(CallVirt inst) => MapCall(inst);

			protected override void VisitBlock(Block block)
			{
				base.VisitBlock(block);
			}

			protected override void VisitBlockContainer(BlockContainer container)
			{
				base.VisitBlockContainer(container);
			}
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
