using System;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class InterfaceBasedEventSourceBuilder : TemplateBasedEventSourceBuilder
	{
		public InterfaceBasedEventSourceBuilder(ModuleDefinition module) : base(module) { }

		public ImplementedEventSource Implement(string name, string guid, TypeDefinition template)
		{
			var previous = DoImplement(template);
			var retval = new InterfaceBasedEventSource
			{
				Meta = previous.Meta,
				Methods = previous.Methods,
				New = previous.New,
				Old = previous.Old
			};

			retval.New.CustomAttributes.Add(CreateEventSourceAttribute(TypeDefs, name, guid));

			return retval;
		}

		protected override TypeReference GetChildTemplate(TypeDefinition template, string nestedName) => template.Module.FindType(template.Namespace + "." + GetTargetTypeName(template) + nestedName);

		protected override string GetTargetTypeName(TypeDefinition template) => template.Name.Substring(1);

		private CustomAttribute CreateEventSourceAttribute(IEventSourceTypeDefs typeDefs, string name, string guid)
		{
			var source = Module.NewAttr(typeDefs.EventSourceAttribute);
			if (!string.IsNullOrEmpty(name)) source.SetPropertyValue("Name", Module.TypeSystem.String, name);
			if (!string.IsNullOrEmpty(guid)) source.SetPropertyValue("Guid", Module.TypeSystem.String, guid);

			return source;
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
