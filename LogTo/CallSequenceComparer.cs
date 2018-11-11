using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.LogTo
{
	internal abstract class CallSequenceComparer : ISequenceComparer<CallCollector.CallInfo>
	{
		public bool IsConsecutive(CallCollector.CallInfo left, CallCollector.CallInfo right)
		{
			var afterX = left.Call.Next;

			// nopos between the two call do not matter
			while (afterX != null && afterX.OpCode == OpCodes.Nop)
				afterX = afterX.Next;

			// there are no (meaningful) ops between the two calls, they can be potentially merged into the same sequence
			return (afterX.Offset == right.StartsAt.Offset) && IsConsecutive(left.Call, right.Call);
		}

		protected abstract bool IsConsecutive(Instruction left, Instruction right);
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
