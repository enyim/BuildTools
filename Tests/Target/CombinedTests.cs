using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Target
{
	public class CombinedTests
	{
		public string currentWriteCopier;

		public bool LogAndEVentSource()
		{
			LogTo.Debug("Sent & finished " + currentWriteCopier.Length);
			var a = DateTime.Now;
			Console.WriteLine(a.Add(TimeSpan.FromDays(12)));
			StaticEventSource.ConnectStop(currentWriteCopier);

			return false;
		}

		public void LogFromTryCatch()
		{
			try
			{
				LogTo.Debug("a");
				HellogTo.Debug("a");
				LogTo.Debug("a");
				HellogTo.Debug("a");
				HellogTo.Hello("a");
			}
			catch { Console.WriteLine(1); }

			try
			{
				LogTo.Info("a", 2);
				throw new NotSupportedException("generic catch");
			}
			catch (InvalidCastException e1) { LogTo.Warn(e1); }
			catch (Exception e2) { LogTo.Error(e2); }

			try
			{
				HellogTo.Debug("0");
				LogTo.Debug("5");
				LogTo.Debug("6");
				HellogTo.Debug("1");
				HellogTo.Debug("2");

				throw new InvalidCastException("no cast");
			}
			catch (InvalidCastException e1) { LogTo.Warn(e1); }
			catch (Exception e2)
			{
				LogTo.Error(e2);
				LogTo.Error(e2);
			}
		}

		public async Task AsyncLogWithError()
		{
			await Task.Delay(1);

			LogTo.Debug("aaaa");

			try
			{
				await Task.Delay(1);
				throw new InvalidOperationException("after delay");
			}
			catch (Exception e)
			{
				LogTo.Error(e);
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
