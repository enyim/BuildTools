using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class LogMethod
	{
		public LogMethod(MethodDefinition method, CustomAttribute a, bool isEmpty = true)
		{
			Method = method;
			IsEmpty = isEmpty;
			EventAttribute = a;
			if (a == null) return;

			Id = (int)a.ConstructorArguments[0].Value;
			Level = LogMethod.GetProp<EventLevel, EventLevel?>(a, "Level", v => v);
			Keywords = LogMethod.GetProp<EventKeywords, EventKeywords?>(a, "Keywords", v => v);
			Task = LogMethod.GetProp<EventTask, NamedConst<int>>(a, "Task", v => NamedConst.Existing(v.ToString(), (int)v));
			Opcode = LogMethod.GetProp<EventOpcode, NamedConst<int>>(a, "Opcode", v => NamedConst.Existing(v.ToString(), (int)v));

			if (Task != null) Log.Info($"LM: {method} {Task.Name}={Task.Value} - exists: {Task.Exists}");
			if (Opcode != null) Log.Info($"LM: {method} {Opcode.Name}={Opcode.Value} - exists: {Opcode.Exists}");
		}

		public int Id { get; set; }
		public bool IsEmpty { get; }
		public CustomAttribute EventAttribute { get; }
		public MethodDefinition Method { get; }
		public EventLevel? Level { get; }
		public EventKeywords? Keywords { get; }
		public NamedConst<int> Opcode { get; set; }
		public NamedConst<int> Task { get; set; }

		public bool HasLevel => Level.HasValue;
		public bool HasKeywords => Keywords.HasValue;

		private static V GetProp<T, V>(CustomAttribute a, string name, Func<T, V> setter)
			=> a.TryGetPropertyValue<T>(name, out var retval) ? setter(retval) : default;
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
