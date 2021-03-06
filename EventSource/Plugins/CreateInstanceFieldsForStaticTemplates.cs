using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.EventSource
{
	internal class CreateInstanceFieldsForStaticTemplates : EventSourceRewriter
	{
		public CreateInstanceFieldsForStaticTemplates(IEnumerable<ImplementedEventSource> implementations) : base(implementations) { }

		public override void BeforeModule(ModuleDefinition module)
		{
			foreach (var impl in Implementations.OfType<StaticBasedEventSource>())
			{
				var newType = impl.New;

				impl.Meta["Instance"] = newType.DeclareStaticField(module, module.ImportReference(newType), "<>Instance", (field) => new[]
				{
					Instruction.Create(OpCodes.Newobj, newType.FindConstructor(new TypeReference[0])),
					Instruction.Create(OpCodes.Stsfld, field)
				}, FieldAttributes.Public);
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
