using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;

namespace Enyim.Build
{
	public abstract class ModuleWeaverBase
	{
		[Browsable(true)]
		public bool Enabled { get; set; } = true;

		public XElement Config { get; set; }
		public ModuleDefinition ModuleDefinition { get; set; }
		public Action<string> LogInfo { get; set; }
		public Action<string> LogWarning { get; set; }
		public Action<string> LogError { get; set; }

		public void Execute()
		{
			ReadConfig();

			if (Enabled) OnExecute();
		}

		protected virtual void ReadConfig()
		{
			if (Config == null) return;

			var props = (from p in GetType().GetProperties()
						 let b = p.GetCustomAttributes(typeof(BrowsableAttribute), true)
									.OfType<BrowsableAttribute>()
									.FirstOrDefault()
						 where BrowsableAttribute.Yes.Equals(b) && p.GetSetMethod() != null
						 select p)
							.ToDictionary(p => p.Name, p => p);

			foreach (var attribute in Config.Attributes())
			{
				PropertyInfo p;

				LogInfo($"Checking {attribute} - {attribute.Name.LocalName}");

				if (props.TryGetValue(attribute.Name.LocalName, out p))
				{
					p.GetSetMethod().Invoke(this, new object[] { Convert.ChangeType(attribute.Value, p.PropertyType) });
					LogInfo($"Set property {attribute.Name.LocalName} to {attribute.Value}");
				}
			}
		}

		protected abstract void OnExecute();
	}
}
