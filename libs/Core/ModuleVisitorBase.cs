using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build
{
	public abstract class ModuleVisitorBase : IModuleVisitor
	{
		protected ModuleVisitorBase() { }

		public bool Enabled { get; protected set; } = true;

		public virtual void BeforeModule(ModuleDefinition module) { }
		public virtual void AfterModule(ModuleDefinition module) { }

		public virtual TypeDefinition BeforeType(TypeDefinition type) => type;
		public virtual void AfterType(TypeDefinition type) { }

		public virtual MethodDefinition BeforeMethod(MethodDefinition method) => method;
		public virtual Instruction MethodInstruction(MethodDefinition owner, Instruction instruction) => instruction;
		public virtual void AfterMethod(MethodDefinition method) { }

		public virtual PropertyDefinition BeforeProperty(PropertyDefinition property) => property;
		public virtual void AfterProperty(PropertyDefinition property) { }

		public virtual FieldDefinition BeforeField(FieldDefinition field) => field;
		public virtual void AfterField(FieldDefinition field) { }
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
