using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Enyim.Build.Weavers.EventSource
{
	using MethodInfo = System.Reflection.MethodInfo;

	internal class UnsafeWriteEventBuilder
	{
		private static MethodInfo RuntimeHelpers_OffsetToStringData_Get = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetProperty("OffsetToStringData").GetGetMethod();
		private static MethodInfo IntPtr_Op_Explicit = typeof(System.IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) });
		private static MethodInfo String_Length_Get = typeof(String).GetProperty("Length").GetGetMethod();

		private readonly ModuleDefinition module;
		private readonly IEventSourceTypeDefs typeDefs;
		private readonly Dictionary<string, Func<BodyBuilder, VariableDefinition, ParameterDefinition, IEnumerable<Instruction>>> emitters;
		private readonly HashSet<string> supportedTypes;

		public UnsafeWriteEventBuilder(ModuleDefinition module, IEventSourceTypeDefs typeDefs)
		{
			this.module = module;
			this.typeDefs = typeDefs;

			var ts = module.TypeSystem;
			emitters = new Dictionary<string, Func<BodyBuilder, VariableDefinition, ParameterDefinition, IEnumerable<Instruction>>>()
			{
				[ts.String.FullName] = StoreStringInEventData,
				[ts.Int32.FullName] = StoreInt32InEventData,
				[ts.Boolean.FullName] = StoreBoolInEventData,
			};

			supportedTypes = new HashSet<string>(emitters.Keys);
		}

		public bool CanDo(MethodDefinition method)
		{
			return method.Parameters
						.Select(ResolveFullName)
						.All(p => supportedTypes.Contains(p));
		}

		private static string ResolveFullName(ParameterDefinition p)
		{
			var pt = p.ParameterType.Resolve();
			if (pt.IsEnum)
				return pt.GetEnumUnderlyingType().FullName;

			return pt.FullName;
		}

		public IEnumerable<Instruction> Emit(BodyBuilder builder, MethodDefinition method, LogMethod metadata)
		{
			Log.Warn(string.Format("Using WriteEventCore fallback for {0}", method.FullName));

			var parameters = method.Parameters;
			var count = parameters.Count;

			var data = builder.DeclareLocal(typeDefs.EventDataRef.MakePointerType(), "data");
			var item = builder.DeclareLocal(typeDefs.EventDataRef.MakePointerType(), "item");

			////> EventData* data = stackalloc EventData[4];
			yield return Instruction.Create(OpCodes.Ldc_I4, count);
			yield return Instruction.Create(OpCodes.Conv_U);
			yield return Instruction.Create(OpCodes.Sizeof, typeDefs.EventDataRef);
			yield return Instruction.Create(OpCodes.Mul_Ovf_Un);
			yield return Instruction.Create(OpCodes.Localloc);
			yield return Instruction.Create(OpCodes.Stloc, data);

			////> EventData* item = data
			yield return Instruction.Create(OpCodes.Ldloc, data);
			yield return Instruction.Create(OpCodes.Stloc, item);

			var first = true;

			foreach (var p in parameters)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					////> item ++;
					yield return Instruction.Create(OpCodes.Ldloc, item);
					yield return Instruction.Create(OpCodes.Sizeof, typeDefs.EventDataRef);
					yield return Instruction.Create(OpCodes.Add);
					yield return Instruction.Create(OpCodes.Stloc, item);
				}

				////> item->DataPointer = ...
				////> item->Size = ...
				var emitter = emitters[ResolveFullName(p)];

				foreach (var i in emitter(builder, item, p))
					yield return i;
			}

			yield return Instruction.Create(OpCodes.Ldarg_0);
			yield return Instruction.Create(OpCodes.Ldc_I4, metadata.Id);
			yield return Instruction.Create(OpCodes.Ldc_I4, count);
			yield return Instruction.Create(OpCodes.Ldloc, data);
			yield return Instruction.Create(OpCodes.Call, typeDefs.WriteEventCore);
		}

		private IEnumerable<Instruction> StoreStringInEventData(BodyBuilder builder, VariableDefinition item, ParameterDefinition param)
		{
			var pinned = builder.DeclareLocal(module.TypeSystem.String.MakePinnedType());
			var pointer = builder.DeclareLocal(module.TypeSystem.Char.MakePointerType());

			yield return Instruction.Create(OpCodes.Ldarg, param);
			yield return Instruction.Create(OpCodes.Stloc, pinned);

			yield return Instruction.Create(OpCodes.Ldloc, pinned);
			yield return Instruction.Create(OpCodes.Conv_I);
			yield return Instruction.Create(OpCodes.Stloc, pointer);

			var isnull = builder.DefineLabel();
			yield return Instruction.Create(OpCodes.Ldloc, pointer);
			yield return Instruction.Create(OpCodes.Brfalse, isnull);

			yield return Instruction.Create(OpCodes.Ldloc, pointer);
			yield return Instruction.Create(OpCodes.Call, module.Import(RuntimeHelpers_OffsetToStringData_Get));
			yield return Instruction.Create(OpCodes.Add);
			yield return Instruction.Create(OpCodes.Stloc, pointer);

			yield return isnull;

			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldloc, pointer);
			yield return Instruction.Create(OpCodes.Call, module.Import(IntPtr_Op_Explicit));
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetDataPointer);

			yield return Instruction.Create(OpCodes.Ldnull);
			yield return Instruction.Create(OpCodes.Stloc, pinned);

			// data[].Size = (SringArg.Length + 1) * 2
			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldarg, param);
			yield return Instruction.Create(OpCodes.Callvirt, module.Import(String_Length_Get));
			yield return Instruction.Create(OpCodes.Ldc_I4_1);
			yield return Instruction.Create(OpCodes.Add);
			yield return Instruction.Create(OpCodes.Ldc_I4_2);
			yield return Instruction.Create(OpCodes.Mul);
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetSize);
		}

		private IEnumerable<Instruction> StoreBoolInEventData(BodyBuilder builder, VariableDefinition item, ParameterDefinition param)
		{
			var temp = builder.DeclareLocal(module.TypeSystem.Int32, reusable: true);
			var @true = builder.DefineLabel();
			var @next = builder.DefineLabel();

			// var temp = param ? 1 : 0;
			yield return Instruction.Create(OpCodes.Ldarg, param);
			yield return Instruction.Create(OpCodes.Brtrue, @true);
			yield return Instruction.Create(OpCodes.Ldc_I4_0);
			yield return Instruction.Create(OpCodes.Br, @next);

			yield return @true;
			yield return Instruction.Create(OpCodes.Ldc_I4_1);
			yield return @next;
			yield return Instruction.Create(OpCodes.Stloc, temp);

			////> item->Data = &temp;
			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldloca, temp);
			yield return Instruction.Create(OpCodes.Conv_U);
			yield return Instruction.Create(OpCodes.Call, module.Import(IntPtr_Op_Explicit));
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetDataPointer);

			////> item->Size = 4;
			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldc_I4_4);
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetSize);
		}

		private IEnumerable<Instruction> StoreInt32InEventData(BodyBuilder builder, VariableDefinition item, ParameterDefinition param)
		{
			////> item->Data = &arg;
			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldarga, param);
			yield return Instruction.Create(OpCodes.Conv_U);
			yield return Instruction.Create(OpCodes.Call, module.Import(IntPtr_Op_Explicit));
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetDataPointer);

			////> item->Size = 4;
			yield return Instruction.Create(OpCodes.Ldloc, item);
			yield return Instruction.Create(OpCodes.Ldc_I4_4);
			yield return Instruction.Create(OpCodes.Call, typeDefs.EventDataSetSize);
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
