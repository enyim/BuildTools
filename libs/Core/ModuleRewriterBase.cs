using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build
{
	public abstract class ModuleRewriterBase
	{
		private static readonly ILog logger = LogManager.GetLogger<ModuleRewriterBase>();

		protected abstract IModuleVisitor[] GetVisitors();

		public void Execute(ModuleDefinition module)
		{
			if (module == null) return;

			try
			{
				logger.Info("Rewriting " + module.FileName);

				var sw = Stopwatch.StartNew();
				OnExecute(module);
				sw.Stop();

				logger.Info($"Done rewriting; elapsed: {sw.Elapsed}");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		protected virtual void OnExecute(ModuleDefinition module)
		{
			var v = GetVisitors();

			if (v?.Length > 0)
				new ModuleRewriterLogic(v).Execute(module);
		}
	}
}
