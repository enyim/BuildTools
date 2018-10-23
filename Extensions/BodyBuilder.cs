using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace Enyim.Build
{
	public class BodyBuilder : IDisposable
	{
		private readonly MethodBody body;
		private readonly Collection<Instruction> instructions;

		private readonly HashSet<Instruction> labels;
		private readonly Dictionary<string, VariableDefinition> variables;

		public BodyBuilder(MethodDefinition method) : this(method.Body) { }

		public BodyBuilder(MethodBody body)
		{
			this.body = body;
			instructions = body.Instructions;

			labels = new HashSet<Instruction>();
			variables = new Dictionary<string, VariableDefinition>();

			// converts (amongst others) all short jumps into long ones
			// making sure that when a large amount of code is inserted the
			// jumps will still be valid
			body.SimplifyMacros();

			LabelizeJumps();
			LabelizeExceptionHandlers();
		}

		public void Dispose()
		{
			if (labels.Count > 0)
			{
				UnlabelizeJumps();
				UnlabelizeExceptionHandlers();

				body.Instructions.Remove(labels);
				labels.Clear();
			}

			// fix offsets
			var offset = 0;

			foreach (var instruction in body.Instructions)
			{
				instruction.Offset = offset;
				offset += instruction.GetSize();
			}

			// convert long form of operations into short form where possible
			body.OptimizeMacros();
		}

		public Instruction DefineLabel()
		{
			var nop = Instruction.Create(OpCodes.Nop);
			labels.Add(nop);

			return nop;
		}

		public void InsertBefore(Instruction where, Instruction what) => instructions.Insert(instructions.IndexOf(where), what);
		public void InsertAfter(Instruction where, Instruction what) => instructions.Insert(instructions.IndexOf(where) + 1, what);

		public VariableDefinition DeclareLocal(TypeReference type, bool reusable = false)
		{
			if (reusable && variables.TryGetValue(type.FullName, out var retval))
				return retval;

			retval = new VariableDefinition(type);
			body.Variables.Add(retval);

			if (reusable)
				variables[type.FullName] = retval;

			return retval;
		}

		private void UnlabelizeJumps()
		{
			// jump (call, branch, switch etc) ops have an Instruction as Operand (the target)
			// retarget all jumps to the first non-Nop (and not label) operation
			// essentially removing the placeholder ops we inserted earlier
			foreach (var instruction in body.Instructions)
			{
				switch (instruction.Operand)
				{
					case Instruction target:
						if (labels.Contains(target))
							instruction.Operand = GetNextNonLabel(target);
						break;

					case Instruction[] targets:
						for (var i = 0; i < targets.Length; i++)
						{
							var target2 = targets[i];
							if (labels.Contains(target2))
								targets[i] = GetNextNonLabel(target2);
						}
						break;
				}
			}
		}

		#region [ Label helpers                ]

		private Instruction GetNextNonLabel(Instruction op)
		{
			// gets the next instruction after a label ('nop')
			while (op != null && labels.Contains(op))
				op = op.Next;

			return op;
		}

		private Instruction GetMeALabel()
		{
			var nop = Instruction.Create(OpCodes.Nop);
			labels.Add(nop);

			return nop;
		}

		private void LabelizeJumps()
		{
			Instruction nop;

			// jump (call, branch, switch etc) ops have an Instruction as Operand (the target)
			// insert a nop before target of each jump and update the instruction to point to this nop
			// to make inserting labels and op blocks easier without changing the flow of the method
			foreach (var op in body.Instructions.ToArray())
			{
				switch (op.Operand)
				{
					case Instruction target:
						nop = GetMeALabel();
						op.Operand = nop;
						InsertBefore(target, nop);
						break;

					case Instruction[] targets:
						for (var i = 0; i < targets.Length; i++)
						{
							nop = GetMeALabel();
							InsertBefore(targets[i], nop);
							targets[i] = nop;
						}
						break;
				}
			}
		}

		private Instruction RetargetWithLabel(Instruction original)
		{
			var label = GetMeALabel();
			body.GetILProcessor().InsertBefore(original, label);

			return label;
		}

		private void LabelizeExceptionHandlers()
		{
			foreach (var h in body.ExceptionHandlers)
			{
				h.HandlerStart = RetargetWithLabel(h.HandlerStart);
				h.HandlerEnd = RetargetWithLabel(h.HandlerEnd);

				h.TryStart = RetargetWithLabel(h.TryStart);
				h.TryEnd = RetargetWithLabel(h.TryEnd);
			}
		}

		private void UnlabelizeExceptionHandlers()
		{
			foreach (var h in body.ExceptionHandlers)
			{
				h.HandlerStart = GetNextNonLabel(h.HandlerStart);
				h.HandlerEnd = GetNextNonLabel(h.HandlerEnd);

				h.TryStart = GetNextNonLabel(h.TryStart);
				h.TryEnd = GetNextNonLabel(h.TryEnd);
			}
		}

		#endregion
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
