using System;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class StaticEventSourceBuilder : TemplateBasedEventSourceBuilder
	{
		public StaticEventSourceBuilder(ModuleDefinition module) : base(module) { }

		protected override bool EmitGuardedLoggers { get { return false; } }

		public StaticBasedEventSource Implement(TypeDefinition template)
		{
			var previous = DoImplement(template);

			// rename the old class as the new one will have the same name
			previous.Old.Name += "_" + Guid.NewGuid().ToString("N");
			previous.Old.CopyAttrsTo(previous.New);

			var retval = new StaticBasedEventSource
			{
				Methods = previous.Methods,
				New = previous.New,
				Old = previous.Old
			};

			return retval;
		}

		protected override EventSourceTemplate CreateEventSourceTemplate(TypeDefinition template)
		{
			return new StaticTemplate(template, TypeDefs);
		}

		protected override string GetTargetTypeName(TypeDefinition template)
		{
			return template.Name;
		}

		protected override TypeReference GetChildTemplate(TypeDefinition template, string nestedName)
		{
			return template.NestedTypes.FirstOrDefault(t => t.Name == nestedName);
		}

		private class StaticTemplate : EventSourceTemplate
		{
			public StaticTemplate(TypeDefinition type, IEventSourceTypeDefs typeDefs) : base(type, typeDefs) { }

			protected override bool IsLogMethod(MethodDefinition m)
			{
				return base.IsLogMethod(m) && m.IsStatic;
			}

			protected override bool IsGuardMethod(MethodDefinition m)
			{
				return base.IsGuardMethod(m) && m.IsStatic;
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
