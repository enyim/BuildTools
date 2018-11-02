using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Mono.Cecil;

namespace Enyim.Build
{
	internal class AssemblyReferenceResolver : DefaultAssemblyResolver
	{
		private static readonly ILog log = LogManager.GetLogger<AssemblyReferenceResolver>();

		public AssemblyReferenceResolver(string source, IEnumerable<string> extraRefs)
		{
			TryCacheDependencies(source, extraRefs);
		}

		private void TryCacheDependencies(string source, IEnumerable<string> extraRefs)
		{
			var sw = Stopwatch.StartNew();
			var deps = TryLoadDeps(source).Concat(extraRefs).Distinct();

			foreach (var a in deps)
			{
				try
				{
					RegisterAssembly(AssemblyDefinition.ReadAssembly(a, new ReaderParameters { ReadingMode = ReadingMode.Deferred }));
				}
				catch { }
			}

			sw.Stop();
			log.Trace($"Initialized the assembly cache in {sw.Elapsed}");
		}

		private static IEnumerable<string> TryLoadDeps(string source)
		{
			source = Path.GetFullPath(source);
			var deps = Path.ChangeExtension(source, ".deps.json");

			if (!File.Exists(deps))
			{
				log.Trace($"Assembly {source} does not have a deps file");
				return Enumerable.Empty<string>();
			}

			DependencyContext context;
			using (var stream = File.OpenRead(deps))
			{
				context = new DependencyContextJsonReader().Read(stream);
			}

			if (context == null) return Enumerable.Empty<string>();

			var resolver = new CompositeCompilationAssemblyResolver
							(new ICompilationAssemblyResolver[]
							{
								new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(source)),
								new ReferenceAssemblyPathResolver(),
								new PackageCompilationAssemblyResolver()
							});

			var assemblies = new List<string>();

			foreach (var lib in context.RuntimeLibraries)
			{
				var cl = new CompilationLibrary(lib.Type, lib.Name, lib.Version, lib.Hash,
												lib.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths), lib.Dependencies, lib.Serviceable, lib.Path, lib.HashPath);

				resolver.TryResolveAssemblyPaths(cl, assemblies);
			}

			var index = assemblies.FindIndex(v => source.Equals(v, StringComparison.OrdinalIgnoreCase));
			if (index > -1)
				assemblies.RemoveAt(index);

			return assemblies;
		}

		public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			try
			{
				return base.Resolve(name, parameters);
			}
			catch (AssemblyResolutionException)
			{
				try
				{
					// maybe it's a system lib, which we cannot resolve from deps.json
					var a = Assembly.Load(name.FullName);

					return AssemblyDefinition.ReadAssembly(a.Location);
				}
				catch (FileNotFoundException)
				{
					log.Trace("could not load assembly " + name);
					return null;
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
