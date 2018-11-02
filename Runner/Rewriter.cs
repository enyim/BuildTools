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
			var source = options.Source.FullName;
			var pdbPath = Path.ChangeExtension(source, ".pdb");
			var pdbExists = File.Exists(pdbPath);

			var readerParameters = new ReaderParameters
			{
				AssemblyResolver = new AssemblyReferenceResolver(source, options.References),
				InMemory = true,
				ReadingMode = ReadingMode.Immediate
			};

			var writerParameters = new Mono.Cecil.WriterParameters
			{
#if CAN_SIGN
					StrongNameKeyPair = options.SignAssembly && options.KeyFile != null
											? new StrongNameKeyPair(File.ReadAllBytes(options.KeyFile.FullName))
											: null
#endif
			};

			ApplySymbolProviders(options, readerParameters, writerParameters);

			using (var module = Mono.Cecil.ModuleDefinition.ReadModule(source, readerParameters))
			{
				var instance = ResolveRewriterInstance(options.Rewriter);
				SetProperties(instance, options.Properties);

				instance.Execute(module);

				var target = (options.Target ?? options.Source).FullName;
				if (writerParameters.SymbolWriterProvider == null)
				{
					try { File.Delete(Path.ChangeExtension(target, ".pdb")); }
					catch { }
				}

				module.Assembly.AddAttr<AssemblyMetadataAttribute>(module, "Rewriter", DateTime.Now.ToString());

				module.Write(target, writerParameters);
				log.Info($"Saved module as {target}");
			}
		}

		private static void ApplySymbolProviders(Options options, ReaderParameters readerParameters, WriterParameters writerParameters)
		{
			if (!options.DebugSymbols)
			{
				readerParameters.ReadSymbols = false;
				readerParameters.SymbolReaderProvider = null;

				writerParameters.WriteSymbols = false;
				writerParameters.SymbolWriterProvider = null;

				return;
			}

			var source = options.Source.FullName;
			var pdbPath = Path.ChangeExtension(source, ".pdb");
			var pdbExists = File.Exists(pdbPath);

			readerParameters.ReadSymbols = true;
			readerParameters.SymbolReaderProvider = pdbExists
													? (ISymbolReaderProvider)new Mono.Cecil.Pdb.PdbReaderProvider()
													: new EmbeddedPortablePdbReaderProvider();

			ISymbolWriterProvider writer = null;
			switch (options.DebugType)
			{
				case null: writer = new Mono.Cecil.Pdb.PdbWriterProvider(); break;
				case DebugType.Portable: writer = new PortablePdbWriterProvider(); break;
				case DebugType.Embedded: writer = new EmbeddedPortablePdbWriterProvider(); break;
			}

			writerParameters.SymbolWriterProvider = writer;
			writerParameters.WriteSymbols = writer != null;
		}

		private static Assembly ResolveRewriterAssembly(string name)
		{
			var root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			var files = Directory.GetFiles($@"{root}\plugins\{name}", $"*{name}.dll");
			if (files.Length == 0) throw new FileNotFoundException($"Cannot find plugin {name}");
			if (files.Length > 1) throw new FileNotFoundException($"Multiple dlls found for {name}; htere should only be one.\n{String.Join("\n", files)}");

			return Assembly.LoadFile(files[0]);
		}

		private static ModuleRewriterBase ResolveRewriterInstance(string rewriterName)
		{
			const string ExpectedTypeName = "ModuleRewriter";

			var assembly = ResolveRewriterAssembly(rewriterName);
			var implementation = assembly.ExportedTypes.FirstOrDefault(t => t.Name == ExpectedTypeName)
									?? throw new InvalidOperationException($"Cannot find {ExpectedTypeName} in {assembly.GetName()}");

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
