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
	internal class NetstandardAssembyResolver : DefaultAssemblyResolver
	{
		private static readonly ILog log = LogManager.GetLogger<NetstandardAssembyResolver>();

		private readonly ICompilationAssemblyResolver resolver;
		private readonly DependencyContext ctx;

		public NetstandardAssembyResolver()
		{
			resolver = new CompositeCompilationAssemblyResolver
							(new ICompilationAssemblyResolver[]
							{
								new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)),
								new ReferenceAssemblyPathResolver(),
								new PackageCompilationAssemblyResolver()
							});
			ctx = DependencyContext.Default;
			var assemblies = new List<string>();

			// load whatever we can from deps.json
			// a lib can have multiple assets (dlls) so we cannot just do this in Resolve
			foreach (var lib in ctx.RuntimeLibraries)
			{
				assemblies.Clear();

				var cl = new CompilationLibrary(lib.Type, lib.Name, lib.Version, lib.Hash,
												lib.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths), lib.Dependencies, lib.Serviceable, lib.Path, lib.HashPath);

				resolver.TryResolveAssemblyPaths(cl, assemblies);
			}

			foreach (var a in assemblies)
				RegisterAssembly(AssemblyDefinition.ReadAssembly(a));
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
