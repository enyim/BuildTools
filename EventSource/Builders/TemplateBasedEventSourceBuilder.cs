using System;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Rewriters.EventSource
{
	internal abstract class TemplateBasedEventSourceBuilder
	{
		protected TemplateBasedEventSourceBuilder(ModuleDefinition module)
		{
			Module = module;
			TypeDefs = new GuessingTypeDefs(module);
		}

		protected abstract string GetTargetTypeName(TypeDefinition template);
		protected abstract TypeReference GetChildTemplate(TypeDefinition template, string nestedName);

		protected readonly ModuleDefinition Module;
		protected readonly GuessingTypeDefs TypeDefs;
		protected virtual bool EmitGuardedTracers => true;

		protected virtual TypeDefinition CreateTargetType(TypeDefinition template)
		{
			var retval = Module.NewType(template.Namespace,
											GetTargetTypeName(template),
											TypeDefs.BaseTypeRef,
											TypeAttributes.Public | TypeAttributes.Sealed);

			TryNestClass(template, retval, "Keywords");
			TryNestClass(template, retval, "Tasks");
			TryNestClass(template, retval, "Opcodes");

			return retval;
		}

		private void TryNestClass(TypeDefinition template, TypeDefinition target, string name)
		{
			var childTemplate = GetChildTemplate(template, name);
			if (childTemplate == null)
				return;

			var childDefinition = childTemplate.Resolve();
			childDefinition.Name = name;
			childDefinition.Attributes = TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

			Module.Types.Remove(childDefinition);

			template.NestedTypes.Remove(childDefinition);
			target.NestedTypes.Add(childDefinition);
		}

		protected virtual ImplementedEventSource DoImplement(TypeDefinition template)
		{
			var targetType = CreateTargetType(template);
			var implementer = new Implementer(Module, CreateEventSourceTemplate(template), targetType) { EmitGuardedTracers = EmitGuardedTracers };

			return new TemplateBasedEventSource
			{
				Old = template,
				New = targetType,
				Methods = implementer.Implement()
			};
		}

		protected virtual EventSourceTemplate CreateEventSourceTemplate(TypeDefinition template) => new EventSourceTemplate(template, TypeDefs);

		#region [ Implementer                  ]

		internal class Implementer : EventSourceImplementerBase
		{
			private readonly TypeDefinition target;

			public Implementer(ModuleDefinition module, EventSourceTemplate template, TypeDefinition target)
				: base(module, template)
			{
				this.target = target;
			}

			public bool EmitGuardedTracers { get; set; }

			protected override TypeDefinition MkNested(string name) => module.NewType(target, name, null, TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);
			protected override TypeDefinition GetNested(string name) => target.NestedTypes.Named(name);

			protected override MethodDefinition ImplementGuardMethod(GuardMethod metadata)
			{
				var source = metadata.Template;
				var newMethod = new MethodDefinition(source.Name, MethodAttributes.Public, module.TypeSystem.Boolean);

				source.CopyAttrsTo(newMethod);
				target.Methods.Add(newMethod);

				SetGuardMethodBody(newMethod, metadata.TraceTemplate.Level, metadata.TraceTemplate.Keywords);

				return newMethod;
			}

			protected override MethodDefinition ImplementTraceMethod(TraceMethod metadata)
			{
				var source = metadata.Method;
				var newMethod = new MethodDefinition(source.Name, MethodAttributes.Public, module.ImportReference(source.ReturnType));
				target.Methods.Add(newMethod);

				foreach (var p in source.Parameters)
					newMethod.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, module.ImportReference(p.ParameterType)));

				source.CopyAttrsTo(newMethod);

				SetTraceMethodBody(newMethod, metadata, EmitGuardedTracers);
				UpdateEventAttribute(newMethod, metadata);

				return newMethod;
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
