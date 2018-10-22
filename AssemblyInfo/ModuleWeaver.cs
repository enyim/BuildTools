using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.LogTo
{
	public class ModuleWeaver : ModuleWeaverBase
	{
		public string Product { get; set; }
		public string Company { get; set; }
		public string Copyright { get; set; }

		public string Configuration { get; set; }

		public string Version { get; set; }
		public string FileVersion { get; set; }
		public string InformalVersion { get; set; }

		protected override void OnExecute()
		{
			ReplaceIfSet<System.Reflection.AssemblyProductAttribute>(Product);
			ReplaceIfSet<System.Reflection.AssemblyCompanyAttribute>(Company);
			ReplaceIfSet<System.Reflection.AssemblyCopyrightAttribute>(Copyright);

			ReplaceIfSet<System.Reflection.AssemblyConfigurationAttribute>(Configuration);

			ReplaceIfSet<System.Reflection.AssemblyVersionAttribute>(Version);
			ReplaceIfSet<System.Reflection.AssemblyFileVersionAttribute>(FileVersion);
			ReplaceIfSet<System.Reflection.AssemblyInformationalVersionAttribute>(InformalVersion);
		}

		private bool ReplaceIfSet<T>(string arg)
			where T : Attribute
		{
			if (String.IsNullOrEmpty(arg)) return false;

			var target = ModuleDefinition.Assembly;
			var previous = target.GetAttr<T>();
			if (previous != null) target.CustomAttributes.Remove(previous);

			target.AddAttr<T>(ModuleDefinition, arg);

			return true;
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
