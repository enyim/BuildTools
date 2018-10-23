using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build
{
	public interface IModuleVisitor
	{
		void BeforeModule(ModuleDefinition module);
		void AfterModule(ModuleDefinition module);

		TypeDefinition BeforeType(TypeDefinition type);
		void AfterType(TypeDefinition type);

		MethodDefinition BeforeMethod(MethodDefinition method);
		Instruction MethodInstruction(MethodDefinition owner, Instruction instruction);
		void AfterMethod(MethodDefinition method);

		PropertyDefinition BeforeProperty(PropertyDefinition property);
		void AfterProperty(PropertyDefinition property);

		FieldDefinition BeforeField(FieldDefinition field);
		void AfterField(FieldDefinition field);
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
