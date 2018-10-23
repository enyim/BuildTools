using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.LogTo
{
	internal class LoggerDefinitionFactory
	{
		private readonly ModuleDefinition module;
		private readonly HashSet<TypeDefinition> notALogger;
		private readonly Dictionary<TypeDefinition, LoggerDefinition> isALogger;

		public LoggerDefinitionFactory(ModuleDefinition module)
		{
			this.module = module;
			notALogger = new HashSet<TypeDefinition>(TypeDefinitionComparer.Instance);
			isALogger = new Dictionary<TypeDefinition, LoggerDefinition>(TypeDefinitionComparer.Instance);
		}

		public bool TryGet(TypeDefinition template, out LoggerDefinition info)
		{
			info = null;

			if (notALogger.Contains(template)) return false;
			if (isALogger.TryGetValue(template, out info)) return true;

			var mapper = template.CustomAttributes.Named("MapLogToAttribute");
			if (mapper == null)
			{
				notALogger.Add(template);
				return false;
			}

			Debug.Assert(template.Module == module);

			var ilog = mapper.GetPropertyValue<TypeDefinition>("ILog");
			var logManager = mapper.GetPropertyValue<TypeDefinition>("LogManager");

			info = new LoggerDefinition(module, template, ilog, logManager);
			isALogger.Add(template, info);

			return true;
		}

		public bool IsLogger(Instruction i) => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
												&& TryGet(i.TargetMethod().DeclaringType.Resolve(), out _);
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