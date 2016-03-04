using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace Enyim.Build
{
	public class BodyBuilder : IDisposable
	{
		private MethodBody body;
		private HashSet<Instruction> labels;

		public BodyBuilder(MethodDefinition method) : this(method.Body) { }

		public Collection<Instruction> Instructions { get; }

		public BodyBuilder(MethodBody body)
		{
			this.body = body;
			labels = new HashSet<Instruction>();
			Instructions = body.Instructions;

			// converts (amongst others) all short jumps into long ones
			// making sure that when a large amount of code is inserted the
			// jumps will still be valid
			body.SimplifyMacros();
			CollectLabels();
		}

		public void InsertBefore(Instruction where, Instruction what)
		{
			Instructions.Insert(Instructions.IndexOf(where), what);
		}

		public void InsertAfter(Instruction where, Instruction what)
		{
			Instructions.Insert(Instructions.IndexOf(where) + 1, what);
		}

		public void Dispose()
		{
			if (labels.Count > 0)
				RemoveLabels();

			// convert long form of operations into short form where possible
			body.OptimizeMacros();
		}

		private void RemoveLabels()
		{
			// jump (call, branch, switch etc) ops have an Instruction as Operand (the target)
			// retarget all jumps to the first non-Nop (and not label) operation
			// essentially removing the placeholder ops we inserted earlier
			foreach (var instruction in body.Instructions)
			{
				var target = instruction.Operand as Instruction;
				if (target != null)
				{
					if (labels.Contains(target))
						instruction.Operand = GetNextNonLabel(target);
				}
				else
				{
					var targets = instruction.Operand as Instruction[];
					if (targets != null)
					{
						for (var i = 0; i < targets.Length; i++)
						{
							var target2 = targets[i];
							if (labels.Contains(target2))
								targets[i] = GetNextNonLabel(target2);
						}
					}
				}
			}

			body.Instructions.Remove(labels);

			foreach (var instruction in body.Instructions.ToArray())
			{
				var target = instruction.Operand as Instruction;
				if (target != null)
				{
					if (labels.Contains(target))
						throw new InvalidOperationException();
				}
				else
				{
					var targets = instruction.Operand as Instruction[];
					if (targets != null)
					{
						for (var i = 0; i < targets.Length; i++)
						{
							var target2 = targets[i];
							if (labels.Contains(target2))
								throw new InvalidOperationException();
						}
					}
				}
			}

			labels.Clear();
		}

		private Instruction GetNextNonLabel(Instruction op)
		{
			while (op != null && labels.Contains(op))
				op = op.Next;

			return op;
		}

		public Instruction DefineLabel()
		{
			var nop = Instruction.Create(OpCodes.Nop);
			labels.Add(nop);

			return nop;
		}

		private Dictionary<string, VariableDefinition> localCache = new Dictionary<string, VariableDefinition>();

		public VariableDefinition DeclareLocal(TypeReference type, string name = null, bool reusable = false)
		{
			VariableDefinition retval;

			if (reusable && localCache.TryGetValue(type.FullName, out retval))
				return retval;

			retval = new VariableDefinition(name, type);
			body.Variables.Add(retval);

			if (reusable)
				localCache[type.FullName] = retval;

			return retval;
		}

		private void CollectLabels()
		{
			var ilp = body.GetILProcessor();
			Instruction nop;

			// jump (call, branch, switch etc) ops have an Instruction as Operand (the target)
			// insert a nop before target of each jump and update the instruction to point to this nop
			// to make inserting labels and op blocks easier without changing the flow of the method
			foreach (var op in body.Instructions.ToArray())
			{
				var target = op.Operand as Instruction;
				if (target != null)
				{
					nop = Instruction.Create(OpCodes.Nop);
					op.Operand = nop;
					ilp.InsertBefore(target, nop);
					labels.Add(nop);
				}
				else
				{
					var targets = op.Operand as Instruction[];
					if (targets != null)
					{
						for (var i = 0; i < targets.Length; i++)
						{
							nop = Instruction.Create(OpCodes.Nop);
							ilp.InsertBefore(targets[i], nop);
							targets[i] = nop;

							labels.Add(nop);
						}
					}
				}
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
