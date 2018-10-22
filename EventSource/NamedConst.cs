using System;
using System.Diagnostics;

namespace Enyim.Build.Weavers.EventSource
{
	[DebuggerDisplay("{Name} = {Value}")]
	internal class NamedConst<T>
	{
		public readonly string Name;
		public readonly T Value;
		public bool Exists;

		public NamedConst(string name, T value)
		{
			Name = name;
			Value = value;
		}
	}

	internal static class NamedConst
	{
		public static NamedConst<T> Existing<T>(string name, T value)
			=> new NamedConst<T>(name, value) { Exists = true };
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
