using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class EventSourceTemplate
	{
		protected const string GuardPrefix = "Can";
		private const int MinTask = 11;
		private const int MaxTask = 238;
		protected static readonly HashSet<string> SpecialMethods = new HashSet<string>(new[] { "IsEnabled" });

		private readonly Lazy<IReadOnlyList<LogMethod>> loggers;
		private readonly Lazy<IReadOnlyList<GuardMethod>> guards;
		protected readonly IEventSourceTypeDefs typeDefs;

		public EventSourceTemplate(TypeDefinition template, IEventSourceTypeDefs typeDefs)
		{
			this.typeDefs = typeDefs;
			Type = template;
			TypeDefs = typeDefs;

			Keywords = GetNamedNestedType(template, "Keywords");
			Tasks = GetNamedNestedType(template, "Tasks");
			Opcodes = GetNamedNestedType(template, "Opcodes");

			loggers = new Lazy<IReadOnlyList<LogMethod>>(() => GetLogMethods().ToArray());
			guards = new Lazy<IReadOnlyList<GuardMethod>>(() => GetGuardMethods().ToArray());
		}

		public TypeDefinition Type { get; private set; }
		public IEventSourceTypeDefs TypeDefs { get; private set; }
		public TypeDefinition Keywords { get; private set; }
		public TypeDefinition Tasks { get; private set; }
		public TypeDefinition Opcodes { get; private set; }

		public IReadOnlyList<LogMethod> Loggers { get { return this.loggers.Value; } }
		public IReadOnlyList<GuardMethod> Guards { get { return this.guards.Value; } }

		private static TypeDefinition GetNamedNestedType(TypeDefinition type, string name)
		{
			return type.NestedTypes.FirstOrDefault(n => n.Name == name);
		}

		protected virtual IEnumerable<LogMethod> GetLogMethods()
		{
			var maxId = 0;
			var loggers = Type.Methods.Where(IsLogMethod);
			var needsId = new List<LogMethod>();
			var all = new List<LogMethod>();

			foreach (var logger in loggers)
			{
				var ea = logger.CustomAttributes.Named("EventAttribute");
				var method = new LogMethod(logger, ea, !logger.HasBody);

				if (ea == null)
					needsId.Add(method);
				else if (method.Id > maxId)
					maxId = method.Id;

				all.Add(method);
			}

			foreach (var method in needsId)
				method.Id = ++maxId;

			TryGenerateTasks(all);

			return all;
		}

		protected virtual bool IsLogMethod(MethodDefinition m)
		{
			return (!m.IsSpecialName && !IsGuardMethod(m)) && !SpecialMethods.Contains(m.Name);
		}

		protected virtual IEnumerable<GuardMethod> GetGuardMethods()
		{
			var loggersByName = Loggers.ToDictionary(m => m.Method.Name);

			foreach (var g in Type.Methods.Where(IsGuardMethod))
			{
				LogMethod lm;

				if (loggersByName.TryGetValue(g.Name.Substring("Can".Length), out lm))
				{
					var guard = new GuardMethod
					{
						LoggerTemplate = lm,
						Template = g,
						IsTemplate = !g.HasBody
					};

					yield return guard;
				}
			}
		}

		protected virtual bool IsGuardMethod(MethodDefinition m)
		{
			return m.Name.StartsWith("Can", StringComparison.Ordinal);
		}

		private void TryGenerateTasks(IEnumerable<LogMethod> methods)
		{
			var toFix = (from m in methods
						 where m.Task == null
						 let match = Regex.Match(m.Method.Name, "^(?'Task'[A-Z]([a-z0-9]*))(?'Op'[A-Z]([a-z0-9]*))$")
						 where match.Success
						 select new
						 {
							 Log = m,
							 Task = match.Groups["Task"].ToString(),
							 Op = match.Groups["Op"].ToString()
						 }).ToArray();

			// get the start id for tasks & OpCOdes
			// which is either 1 or the largest constsant +1 from the respective inner class
			var maxTask = (Tasks == null ? 0 : MaxConst(Tasks, "EventTask")) + 1;
			var maxOp = (Opcodes == null ? 0 : MaxConst(Opcodes, "EventOpcode")) + 1;

			if (maxOp < 11) maxOp = 11;
			if (maxOp + toFix.Length > 238) throw new ArgumentException("too much op");

			var systemOps = MapEnumMembers<int>(typeDefs.EventOpcode.Resolve());
			var knownTasks = MapEnumMembers<int>(Tasks);
			var knownOps = MapEnumMembers<int>(Opcodes);

			foreach (var a in toFix)
			{
				int id;

				a.Log.Task = !knownTasks.TryGetValue(a.Task, out id)
								? new NamedConst<int>(a.Task, knownTasks[a.Task] = maxTask++)
								: new NamedConst<int>(a.Task, id) { Exists = true };

				a.Log.Opcode = !systemOps.TryGetValue(a.Op, out id) && !knownOps.TryGetValue(a.Op, out id)
								? new NamedConst<int>(a.Op, knownOps[a.Op] = maxOp++)
								: new NamedConst<int>(a.Op, id) { Exists = true };
			}
		}

		private static Dictionary<string, T> MapEnumMembers<T>(TypeDefinition type)
		{
			if (type == null) return new Dictionary<string, T>();

			return type.Fields
						.Where(f => f.IsStatic && f.HasConstant)
						.ToDictionary(f => f.Name, f => (T)f.Constant);
		}

		private static TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
		{
			TValue obj;
			return !dict.TryGetValue(key, out obj)
					? dict[key] = factory()
					: obj;
		}

		private int MaxConst(TypeDefinition type, string constType)
		{
			var values = type.Fields
								.Where(f => f.IsStatic && f.FieldType.Name == constType)
								.Select(f => (int)f.Constant)
								.ToArray();

			return values.Length == 0 ? 0 : values.Max();
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
