using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Enyim.Build
{
	internal class Options
	{
		public string Rewriter { get; set; }
		public FileInfo Source { get; set; }
		public FileInfo Target { get; set; }

#if CAN_SIGN
		public bool SignAssembly { get; private set; }
		public FileInfo KeyFile { get; private set; }
#endif
		public bool DebugSymbols { get; set; }
		public DebugType? DebugType { get; set; }
		public List<string> References { get; } = new List<string>();
		public List<KeyValuePair<string, string>> Properties { get; } = new List<KeyValuePair<string, string>>();
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
