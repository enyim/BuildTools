using System;
using NDesk.Options;

namespace Enyim.Build
{
	internal static class OptionSetExtensions
	{
		public static OptionSet AddEnumSwitch<TEnum>(this OptionSet set, string prototype, string description, Action<TEnum> setter)
			where TEnum : struct
		{
			return set.Add(prototype, description + " (" + String.Join(", ", Enum.GetNames(typeof(TEnum))) + ")", s =>
			{
				if (Enum.TryParse<TEnum>(s, out var value))
					setter(value);
			});
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
