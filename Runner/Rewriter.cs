using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build
{
	internal class Rewriter
	{
		private static readonly ILog log = LogManager.GetLogger<Rewriter>();

		public void Rewrite(Options options)
		{
			ISymbolReaderProvider reader = null;
			ISymbolWriterProvider writer = null;

			switch (options.Symbols)
			{
				case DebugSymbolsKind.Embedded:
					reader = new EmbeddedPortablePdbReaderProvider();
					writer = new EmbeddedPortablePdbWriterProvider();
					break;

				case DebugSymbolsKind.Portable:
					reader = new PortablePdbReaderProvider();
					writer = new PortablePdbWriterProvider();
					break;
			}

			using (var module = Mono.Cecil.ModuleDefinition.ReadModule(options.Source.FullName, new ReaderParameters
			{
				InMemory = true,
				SymbolReaderProvider = reader,
				ReadSymbols = reader != null,
				ReadingMode = Mono.Cecil.ReadingMode.Deferred,
				AssemblyResolver = new BasicAssembyResolver(options.Source.Directory.FullName)
			}))
			{
				var instance = Resolve(options);
				SetProperties(instance, options.Properties);

				instance.Execute(module);

				module.Write(options.Target.FullName, new Mono.Cecil.WriterParameters
				{
					WriteSymbols = writer != null,
					SymbolWriterProvider = writer,
					StrongNameKeyPair = options.SignAssembly && options.KeyFile != null
											? new StrongNameKeyPair(File.ReadAllBytes(options.KeyFile.FullName))
											: null
				});
			}
		}

		private ModuleRewriterBase Resolve(Options options)
		{
			const string ExpectedTypeName = "ModuleRewriter";

			var assembly = Assembly.LoadFile(options.Rewriter.FullName);

			var implementation = assembly.ExportedTypes.FirstOrDefault(t => t.Name == ExpectedTypeName) ?? throw new InvalidOperationException($"Cannot find {ExpectedTypeName} in {assembly.GetName()}");
			var instance = Activator.CreateInstance(implementation) as ModuleRewriterBase ?? throw new InvalidOperationException($"{implementation.FullName} must inherit from {typeof(ModuleRewriterBase).FullName}");

			return instance;
		}

		private static void SetProperties(object instance, List<KeyValuePair<string, string>> properties)
		{
			if (properties.Count == 0) return;

			log.Trace($"Configuring {instance}");

			var configProperty = instance.GetType().GetProperty("Config");
			if (configProperty == null)
			{
				log.Trace($"It has no Config property.");
				return;
			}

			var configValue = configProperty.GetValue(instance);
			if (configValue == null)
			{
				log.Warn($"Config property is null and cannot be updated.");
				return;
			}

			var configType = configProperty.PropertyType;
			var props = (from p in configType.GetProperties()
						 let b = p.GetCustomAttributes(typeof(BrowsableAttribute), true)
									.OfType<BrowsableAttribute>()
									.FirstOrDefault()
						 where BrowsableAttribute.Yes.Equals(b) && p.GetSetMethod() != null
						 select p)
						.ToDictionary(p => p.Name, p => p);

			foreach (var kvp in properties)
			{
				if (props.TryGetValue(kvp.Key, out var cp))
				{
					cp.SetValue(configValue, kvp.Value);
					log.Info($"{kvp.Key} was set to {kvp.Value}");
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
