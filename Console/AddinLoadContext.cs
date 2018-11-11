#define EASY_DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Enyim.Build
{
	public class AddinLoadContext : AssemblyLoadContext
	{
		private readonly string root;
		private readonly Dictionary<string, string> knownAssemblies;
		private readonly HashSet<string> preferDefault;

		public AddinLoadContext(string addinPath, params Type[] sharedTypes)
			: this(addinPath, sharedTypes.Select(t => t.Assembly.GetName().Name).ToArray()) { }

		public AddinLoadContext(string addinPath, params AssemblyName[] sharedAssemblies)
			: this(addinPath, sharedAssemblies.Select(a => a.Name).ToArray()) { }

		public AddinLoadContext(string addinPath, string[] sharedAssemblies)
		{
			addinPath = Path.GetFullPath(addinPath);
			root = Path.GetDirectoryName(addinPath);
			preferDefault = new HashSet<string>(sharedAssemblies);

			knownAssemblies = new Dictionary<string, string>();
			foreach (var p in GetKnownDependencies(addinPath).Concat(GetAppBaseAssemblies(addinPath)))
				knownAssemblies[Path.GetFileNameWithoutExtension(p)] = p;
		}

		private static IEnumerable<string> GetAppBaseAssemblies(string addinPath)
		{
			return Directory
					.GetFiles(Path.GetDirectoryName(addinPath), "*.dll")
					.Where(p => !p.Equals(addinPath, StringComparison.OrdinalIgnoreCase));
		}

		private static IEnumerable<string> GetKnownDependencies(string addinPath)
		{
			var retval = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var deps = Path.ChangeExtension(addinPath, ".deps.json");

			DependencyContext context;

			using (var stream = File.OpenRead(deps))
			{
				context = new DependencyContextJsonReader().Read(stream);
			}

			if (context == null) return Enumerable.Empty<string>();

			var resolver = new CompositeCompilationAssemblyResolver
							(new ICompilationAssemblyResolver[]
							{
									new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(addinPath)),
									new ReferenceAssemblyPathResolver(),
									new PackageCompilationAssemblyResolver()
							});

			var rid = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
			var rids = new[] { rid };

			var fallbacks = context.RuntimeGraph.FirstOrDefault(f => f.Runtime == rid);
			if (fallbacks != null) rids = rids.Concat(fallbacks.Fallbacks).ToArray();

			var assemblies = new List<string>();

			foreach (var rlib in context.RuntimeLibraries)
			{
				var ridAssets = SelectAssets(rids, rlib.RuntimeAssemblyGroups);
				var cl = new CompilationLibrary(rlib.Type, rlib.Name, rlib.Version, rlib.Hash, ridAssets, rlib.Dependencies, rlib.Serviceable, rlib.Path, rlib.HashPath);
				resolver.TryResolveAssemblyPaths(cl, assemblies);
			}

			return assemblies.Select(Path.GetFullPath);
		}


		private static IEnumerable<string> SelectAssets(string[] rids, IEnumerable<RuntimeAssetGroup> groups)
		{
			foreach (var rid in rids)
			{
				var runtimeAssetGroup = groups.FirstOrDefault(g => g.Runtime == rid);
				if (runtimeAssetGroup != null)
					return runtimeAssetGroup.AssetPaths;
			}

			return groups.GetDefaultAssets();
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			if (preferDefault.Contains(assemblyName.Name))
			{
				try
				{
					var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
					if (defaultAssembly != null)
						return null; // the runtime will load it from the default context
				}
				catch { }
			}

			if (knownAssemblies.TryGetValue(assemblyName.Name, out var path))
				return LoadFromAssemblyPath(path);

			var fileName = Path.Combine(root, assemblyName.Name) + ".dll";
			if (File.Exists(fileName))
				return LoadFromAssemblyPath(fileName);

			return null;
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
