using System;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class AbstractEventSourceBuilder
	{
		private readonly ModuleDefinition module;

		public AbstractEventSourceBuilder(ModuleDefinition module) => this.module = module;

		internal ImplementedEventSource Implement(TypeDefinition type)
		{
			type.FindConstructor().Resolve().Attributes |= MethodAttributes.Public;
			type.Attributes = type.Attributes.Remove(TypeAttributes.Abstract).Add(TypeAttributes.Sealed);

			var methods = new Implementer(module, new EventSourceTemplate(type, new BaseClassTypeDefs(type))).Implement();

			return new AbstractBasedEventSource
			{
				Methods = methods,
				New = type,
				Old = type
			};
		}

		internal class Implementer : EventSourceImplementerBase
		{
			public Implementer(ModuleDefinition module, EventSourceTemplate template) : base(module, template) { }

			protected override TypeDefinition MkNested(string name) => module.NewType(template.Type, name, null, TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);
			protected override TypeDefinition GetNested(string name) => template.Type.NestedTypes.FirstOrDefault(t => t.Name == name);

			protected override MethodDefinition ImplementGuardMethod(GuardMethod metadata)
			{
				var retval = metadata.Template;

				if (metadata.IsTemplate)
				{
					retval.Attributes = (MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig);
					SetGuardMethodBody(retval, metadata.LoggerTemplate.Level, metadata.LoggerTemplate.Keywords);
				}

				return retval;
			}

			protected override MethodDefinition ImplementLogMethod(LogMethod metadata)
			{
				var retval = metadata.Method;

				if (metadata.IsEmpty)
				{
					retval.Attributes = (MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig);
					SetLogMethodBody(retval, metadata, true);
				}

				UpdateEventAttribute(retval, metadata);

				return retval;
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
