#define ENABLE_UNSAFE
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Enyim.Build.Weavers.EventSource
{
	internal abstract class EventSourceImplementerBase
	{
		protected const string GuardPrefix = "Can";
		protected static readonly HashSet<string> SpecialMethods = new HashSet<string>("IsEnabled".Once());

		protected readonly ModuleDefinition module;
		protected readonly EventSourceTemplate template;
		protected readonly IEventSourceTypeDefs typeDefs;
		protected readonly Lazy<TypeDefinition> ensureTasks;
		protected readonly Lazy<TypeDefinition> ensureOpcodes;
#if ENABLE_UNSAFE
		protected readonly UnsafeWriteEventBuilder unsafeWriteEventBuilder;
#endif

		protected EventSourceImplementerBase(ModuleDefinition module, EventSourceTemplate template)
		{
			this.module = module;
			this.template = template;
			this.typeDefs = template.TypeDefs;
			this.ensureOpcodes = EnsureNestedBuilder("Opcodes");
			this.ensureTasks = EnsureNestedBuilder("Tasks");
#if ENABLE_UNSAFE
			this.unsafeWriteEventBuilder = new UnsafeWriteEventBuilder(module, typeDefs);
#endif
		}

		protected abstract MethodDefinition ImplementLogMethod(LogMethod metadata);
		protected abstract MethodDefinition ImplementGuardMethod(GuardMethod metadata);

		protected abstract TypeDefinition MkNested(string name);
		protected abstract TypeDefinition GetNested(string name);

		public virtual Implemented<MethodDefinition>[] Implement()
		{
			var retval = template
							.Loggers.Select(meta => Implemented.Create(meta.Method, this.ImplementLogMethod(meta)))
							.Concat(template.Guards.Select(meta => Implemented.Create(meta.Template, ImplementGuardMethod(meta))))
							.ToArray();

			return retval;
		}

		protected void UpdateEventAttribute(MethodDefinition target, LogMethod metadata)
		{
			var ea = metadata.EventAttribute;
			if (ea == null)
				target.CustomAttributes.Add(ea = module.NewAttr(typeDefs.EventAttribute, metadata.Id));
			else
				ea = target.CustomAttributes.Named("EventAttribute");

			if (metadata.Task != null)
			{
				if (!metadata.Task.Exists)
					AddConst<int>(ensureTasks.Value, module.Import(typeDefs.EventTask), metadata.Task);

				ea.SetPropertyValue("Task", typeDefs.EventTask, metadata.Task.Value);
			}

			if (metadata.Opcode != null)
			{
				if (!metadata.Opcode.Exists)
					AddConst<int>(ensureOpcodes.Value, module.Import(typeDefs.EventOpcode), metadata.Opcode);

				ea.SetPropertyValue("Opcode", typeDefs.EventOpcode, metadata.Opcode.Value);
			}
		}

		protected void AddConst<T>(TypeDefinition target, TypeReference type, NamedConst<T> c)
		{
			var field = new FieldDefinition(c.Name, FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, type)
			{
				Constant = c.Value
			};
			target.Fields.Add(field);
		}

		protected void SetLogMethodBody(MethodDefinition target, LogMethod metadata, bool implementGuard = true)
		{
			target.CustomAttributes.Add(module.NewAttr(typeof(CompilerGeneratedAttribute)));

			var body = target.Body.Instructions;
			body.Clear();

			using (var builder = new BodyBuilder(target))
			{
				var exit = builder.DefineLabel();
				if (implementGuard)
				{
					body.Add(EmitIsEnabledFallback());
					body.Add(Instruction.Create(OpCodes.Brfalse, exit));
				}

				body.Add(WriteEvent(builder, target, metadata).ToArray());

				body.Add(exit);
				body.Add(Instruction.Create(OpCodes.Ret));
			}
		}

		protected void SetGuardMethodBody(MethodDefinition method, EventLevel? level, EventKeywords? keywords)
		{
			method.CustomAttributes.Add(new CustomAttribute[]
			{
				module.NewAttr(typeDefs.NonEventAttribute),
				module.NewAttr(typeof(CompilerGeneratedAttribute))
			});

			var body = method.Body.Instructions;
			body.Clear();

			body.Add(EmitIsEnabled(level, keywords));
			body.Add(Instruction.Create(OpCodes.Ret));
		}

		protected IEnumerable<Instruction> EmitIsEnabled(EventLevel? level, EventKeywords? keywords)
		{
			return !level.HasValue || !keywords.HasValue
					? EmitIsEnabledFallback()
					: EmitSpecificIsEnabled(level.Value, keywords.Value);
		}

		private IEnumerable<Instruction> EmitIsEnabledFallback()
		{
			yield return Instruction.Create(OpCodes.Ldarg_0);
			yield return Instruction.Create(OpCodes.Call, typeDefs.IsEnabledFallback);
		}

		private IEnumerable<Instruction> EmitSpecificIsEnabled(EventLevel level, EventKeywords keywords)
		{
			yield return Instruction.Create(OpCodes.Ldarg_0);
			yield return Instruction.Create(OpCodes.Ldc_I4, (int)level);

			var kw = (long)keywords;
			if (kw >= Int32.MinValue && kw <= Int32.MaxValue)
			{
				yield return Instruction.Create(OpCodes.Ldc_I4, (int)kw);
				yield return Instruction.Create(OpCodes.Conv_I8);
			}
			else
			{
				yield return Instruction.Create(OpCodes.Ldc_I8, kw);
			}

			yield return Instruction.Create(OpCodes.Call, typeDefs.IsEnabledSpecific);
		}

		private IEnumerable<Instruction> WriteEvent(BodyBuilder builder, MethodDefinition method, LogMethod metadata)
		{
			var specificArgs = module.TypeSystem.Int32.Once()
									.Concat(method.Parameters
													.Select(p => ConvertToWriteEventParamType(p.ParameterType.Resolve())));

			var specific = typeDefs.BaseTypeImpl.FindMethod("WriteEvent", specificArgs);

			return specific != null
					? EmitSpecificWriteEvent(builder, method, specific, metadata)
#if ENABLE_UNSAFE
					: unsafeWriteEventBuilder.CanDo(method)
						? unsafeWriteEventBuilder.Emit(builder, method, metadata)
#endif
					: EmitWriteEventFallback(builder, method, metadata);
		}

		private IEnumerable<Instruction> EmitSpecificWriteEvent(BodyBuilder builder, MethodDefinition method, MethodReference writeEvent, LogMethod metadata)
		{
			yield return Instruction.Create(OpCodes.Ldarg_0);
			yield return Instruction.Create(OpCodes.Ldc_I4, metadata.Id);

			foreach (var p in method.Parameters)
			{
				yield return Instruction.Create(OpCodes.Ldarg, p);
				foreach (var i in EmitConvertCode(p.ParameterType.Resolve(), builder))
					yield return i;
			}

			yield return Instruction.Create(OpCodes.Call, module.Import(writeEvent));
		}

		private IEnumerable<Instruction> EmitWriteEventFallback(BodyBuilder builder, MethodDefinition method, LogMethod metadata)
		{
			Log.Warn(string.Format("Using WriteEvent fallback for {0}", method.FullName));

			yield return Instruction.Create(OpCodes.Ldarg_0);
			yield return Instruction.Create(OpCodes.Ldc_I4, metadata.Id);

			var parameters = method.Parameters;
			var count = parameters.Count;

			yield return Instruction.Create(OpCodes.Ldc_I4, count);
			yield return Instruction.Create(OpCodes.Newarr, module.TypeSystem.Object);

			for (var i = 0; i < count; i++)
			{
				yield return Instruction.Create(OpCodes.Dup);
				yield return Instruction.Create(OpCodes.Ldc_I4, i);
				yield return Instruction.Create(OpCodes.Ldarg, parameters[i]);

				if (parameters[i].ParameterType.IsValueType)
					yield return Instruction.Create(OpCodes.Box, parameters[i].ParameterType);

				yield return Instruction.Create(OpCodes.Stelem_Ref);
			}

			yield return Instruction.Create(OpCodes.Call, typeDefs.WriteEventFallback);
		}

		private TypeReference ConvertToWriteEventParamType(TypeDefinition type)
		{
			if (type.IsEnum)
				return type.GetEnumUnderlyingType();

			if (type.FullName == module.TypeSystem.Boolean.FullName)
				return module.TypeSystem.Int32;

			// TODO
			return type;
		}

		private IEnumerable<Instruction> EmitConvertCode(TypeReference sourceType, BodyBuilder builder)
		{
			if (sourceType.FullName == module.TypeSystem.Boolean.FullName)
				return EmitBoolConvertCode(builder);

			// TODO
			return Enumerable.Empty<Instruction>();
		}

		private IEnumerable<Instruction> EmitBoolConvertCode(BodyBuilder builder)
		{
			var @true = Instruction.Create(OpCodes.Ldc_I4, 1);
			var endOfBlock = builder.DefineLabel();

			yield return Instruction.Create(OpCodes.Brtrue, @true);
			yield return Instruction.Create(OpCodes.Ldc_I4, 0);

			yield return Instruction.Create(OpCodes.Br, endOfBlock);
			yield return @true;

			yield return endOfBlock;
		}

		private Lazy<TypeDefinition> EnsureNestedBuilder(string name)
		{
			return new Lazy<TypeDefinition>(() => GetNested(name) ?? this.MkNested(name));
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
