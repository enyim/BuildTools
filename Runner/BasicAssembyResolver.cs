using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Mono.Cecil;

namespace Enyim.Build
{
	internal class SourceAssemblyResolver : DefaultAssemblyResolver
	{
		private static readonly ILog log = LogManager.GetLogger<SourceAssemblyResolver>();

		public SourceAssemblyResolver(string source)
		{
			TryCacheDependencies(source);
		}

		private void TryCacheDependencies(string source)
		{
			var deps = Path.ChangeExtension(source, ".deps.json");
			if (!File.Exists(deps)) return;

			DependencyContext context;

			using (var stream = File.OpenRead(deps))
			{
				context = new DependencyContextJsonReader().Read(stream);
			}

			if (context == null) return;


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

			foreach (var a in assemblies.Distinct())
				RegisterAssembly(AssemblyDefinition.ReadAssembly(a, new ReaderParameters { InMemory = true }));
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
