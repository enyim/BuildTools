using System;
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
			var body = new ILAstBuilder().StackAnalysis(method, 0 != 0, new DecompilerContext(module) { CurrentMethod = method });
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

						yield return new Call(ops[slot.Length == 0 ? byteCode.Offset : slot.SelectMany(s => s.Definitions).Min(def => def.Offset)], ops[byteCode.Offset]);
					}
				}
			}
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
			private readonly List<Call> calls;

			public IReadOnlyList<Call> Calls
			{
				get { return (IReadOnlyList<Call>)this.calls; }
			}

			public CallGroup()
			{
				this.calls = new List<Call>();
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
				Method = (MethodDefinition)end.Operand;
			}

			public Instruction Start { get; private set; }
			public MethodDefinition Method { get; private set; }
			public Instruction End { get; private set; }
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
