using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mono.Cecil;

namespace Runner
{
	internal class Weaver
	{
		private readonly Assembly assembly;
		private readonly ILogger logger;
		private readonly XElement config;

		public Weaver(Assembly assembly, ILogger logger)
		{
			Name = assembly.GetName().Name.Replace(".Fody", "");

			this.assembly = assembly;
			this.logger = logger;
			config = new XElement(Name);
		}

		public string Name { get; }

		private object Resolve()
		{
			var weaverType = assembly.ExportedTypes.FirstOrDefault(t => t.Name == "ModuleWeaver");
			if (weaverType == null) throw new InvalidOperationException($"Cannot find ModuleWeaver in {assembly.GetName()}");

			return Activator.CreateInstance(weaverType);
		}

		private void SetProperty(Type t, object instance, string name, object value)
		{
			var p = t.GetProperty(name);
			p.SetValue(instance, value);
		}

		public void SetProperties(IEnumerable<KeyValuePair<string, string>> properties)
		{
			foreach (var kvp in properties)
				config.SetAttributeValue(kvp.Key, kvp.Value);
		}

		public ModuleDefinition Rewrite(string source)
		{
			var module = ModuleDefinition.ReadModule(source, new ReaderParameters
			{
				AssemblyResolver = new WeaverAssembyResolver(Path.GetDirectoryName(source)),
				ReadSymbols = false,//true,
				ReadingMode = ReadingMode.Deferred
			});

			RunWeaver(module);

			return module;
		}

		private void RunWeaver(ModuleDefinition module)
		{
			var weaver = Resolve();
			var weaverType = weaver.GetType();

			SetProperty(weaverType, weaver, "Config", config);
			SetProperty(weaverType, weaver, "LogError", new Action<string>(logger.Error));
			SetProperty(weaverType, weaver, "LogInfo", new Action<string>(logger.Info));
			SetProperty(weaverType, weaver, "LogWarning", new Action<string>(logger.Warn));
			SetProperty(weaverType, weaver, "ModuleDefinition", module);

			weaverType.GetMethod("Execute").Invoke(weaver, null);
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
