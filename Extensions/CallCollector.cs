﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build
{
	public class CallCollector
	{
		private readonly ModuleDefinition module;

		public CallCollector(ModuleDefinition module)
		{
			this.module = module;
		}

		public IEnumerable<Call> FindCalls(MethodDefinition method, Func<MethodReference, bool> filter = null)
		{
			var body = new ILAstBuilder().StackAnalysis(method, false, new DecompilerContext(module) { CurrentMethod = method });
			var ops = method.Body.Instructions.ToDictionary(i => i.Offset);
			var calls = new List<CallGroup>();

			foreach (var byteCode in body)
			{
				if (byteCode.Code == ILCode.Call || byteCode.Code == ILCode.Callvirt)
				{
					var methodReference = byteCode.Operand as MethodReference;
					if (methodReference != null && (filter == null || filter(methodReference)))
					{
						var slot = byteCode.StackBefore;
						yield return new Call(ops[GetStartOfCallBlock(method, byteCode)], ops[byteCode.Offset]);
					}
				}
			}
		}

		private static int GetStartOfCallBlock(MethodDefinition method, ILAstBuilder.ByteCode bc)
		{
			var slot = bc.StackBefore;

			if (slot == null)
				throw new InvalidOperationException();

			while (slot != null && slot.Length > 0)
			{
				var defs = slot.SelectMany(s => s.Definitions).ToArray();
				var ldx = Array.FindIndex(defs, d => d.Code == ILCode.Ldexception);
				if (ldx > -1)
				{
					var containedBy = method.Body.ExceptionHandlers
											.Where(eh => eh.HandlerStart.Offset <= bc.Offset && bc.Offset <= eh.HandlerEnd.Offset)
											.OrderBy(eh => eh.HandlerEnd.Offset - eh.HandlerStart.Offset)
											.First();

					return containedBy.HandlerStart.Offset;
					//if (ldx == defs.Length - 1) break;
					//return defs[ldx + 1].Offset;
				}

				bc = slot.SelectMany(s => s.Definitions).OrderBy(d => d.Offset).First();
				slot = bc.StackBefore;
			}

			return bc.Offset;
		}

		public CallGroup[] CollapseBlocks(IEnumerable<Call> calls, IEqualityComparer<Instruction> callChecker)
		{
			CallGroup current = null;
			var retval = new List<CallGroup>();

			foreach (var call in calls)
			{
				if (current != null)
				{
					var last = current.Calls.Last();
					var next = last.End.Next;

					while (next != null && next.OpCode == OpCodes.Nop)
						next = next.Next;

					if (next?.Offset == call.Start.Offset && callChecker.Equals(last.End, call.End))
					{
						current.Add(call);
						continue;
					}
				}

				current = new CallGroup(call);
				retval.Add(current);
			}

			return retval.ToArray();
		}

		public class CallGroup
		{
			private readonly List<Call> calls = new List<Call>();

			public IReadOnlyList<Call> Calls
			{
				get { return calls; }
			}

			public CallGroup(params Call[] calls)
			{
				this.calls = new List<Call>();
				foreach (Call c in calls)
					Add(c);
			}

			public Instruction Start { get; private set; }
			public Instruction End { get; private set; }

			public void Add(Call call)
			{
				if (calls.Count == 0)
					Start = call.Start;

				calls.Add(call);
				End = call.End;
			}
		}

		public class Call
		{
			public Call(Instruction start, Instruction end)
			{
				Start = start;
				End = end;
				Method = (MethodReference)end.Operand;
			}

			public Instruction Start { get; }
			public MethodReference Method { get; }
			public Instruction End { get; }
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
