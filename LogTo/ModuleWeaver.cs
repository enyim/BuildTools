using System;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.LogTo
{
	public class ModuleWeaver
	{
		public Action<string> LogInfo { get; set; }
		public ModuleDefinition ModuleDefinition { get; set; }

		public void Execute()
		{
			var logDef = new LogDefinition(this.ModuleDefinition);
			var info = LogInfo ?? (_ => { });

			if (!logDef.IsValid)
			{
				info("Could not find any of ILog, LogTo, LogManager, - or - no logging code was found.");
			}
			else
			{
				var types = from t in ModuleDefinition.Types
							where t.IsClass && !t.IsAbstract && t.Name != "<Module>"
							select t;

				foreach (var typeDef in types)
				{
					info($"Rewriting type {typeDef}");

					var success = new LoggerImplementer(logDef, ModuleDefinition, typeDef)
					{
						LogInfo = info
					}.TryRewrite();

					info($" Result: {success}");
				}
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
