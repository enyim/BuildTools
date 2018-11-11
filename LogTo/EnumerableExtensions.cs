using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build.Rewriters.LogTo
{
	internal static class EnumerableExtensions
	{
		public static IEnumerable<IEnumerable<T>> SplitToSequences<T>(this IEnumerable<T> source, ISequenceComparer<T> comparer)
		{
			var retval = new List<List<T>>();
			List<T> currentSequence = null;

			foreach (var item in source)
			{
				if (currentSequence != null && comparer.IsConsecutive(currentSequence.Last(), item))
				{
					currentSequence.Add(item);
					continue;
				}

				currentSequence = new List<T> { item };
				retval.Add(currentSequence);
			}

			return retval;
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
