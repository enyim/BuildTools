using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Target
{
	class AbstractEventSourceTests
	{
		AbstractEventSource trace = EventSourceFactory.Get<AbstractEventSource>();

		public void Hello()
		{
			trace.ConnectStart("aaaa");
			trace.ConnectStart("aaaa");
			trace.ReceiveStop("aaaa", false, true);
		}

	}

	[EventSource(Name = "Enyim-Caching-Core")]
	public abstract class AbstractEventSource : EventSource
	{
		[Event(1, Level = EventLevel.Informational, Keywords = Keywords.Socket)]
		public abstract void ConnectStart(string endpoint);
		[Event(2, Level = EventLevel.Informational, Keywords = Keywords.Socket)]
		public abstract void ConnectStop(string endpoint);

		[Event(3, Level = EventLevel.Error, Keywords = Keywords.Socket)]
		public abstract void ConnectFail(string endpoint, SocketError status);

		[Event(10, Level = EventLevel.Verbose, Keywords = Keywords.Socket)]
		public abstract void SendStart(string endpoint, bool isAlive, int byteCount);
		[Event(11, Level = EventLevel.Verbose, Keywords = Keywords.Socket)]
		public abstract void SendStop(string endpoint, bool isAlive, bool success);

		[Event(12, Level = EventLevel.Verbose, Keywords = Keywords.Socket)]
		public unsafe void SendChunk(string endpoint, string hello, string world, bool isAlive, int bytesSent, SocketError status)
		{
			var data = stackalloc EventData[6];

			fixed (char* pEndpoint = endpoint) data[0].DataPointer = (IntPtr)(pEndpoint);
			data[0].Size = (endpoint.Length + 1) * 2;

			fixed (char* pHello = hello) data[1].DataPointer = (IntPtr)(pHello);
			data[1].Size = (hello.Length + 1) * 2;

			fixed (char* pWorld = world) data[2].DataPointer = (IntPtr)(pWorld);
			data[2].Size = (world.Length + 1) * 2;

			var alive = isAlive ? 1 : 0;
			data[2].DataPointer = (IntPtr)(&alive);
			data[2].Size = sizeof(int);

			data[4].DataPointer = (IntPtr)(&bytesSent);
			data[4].Size = sizeof(int);

			data[5].DataPointer = (IntPtr)(&status);
			data[5].Size = sizeof(int);

			WriteEventCore(12, 6, data);
		}

		[Event(20, Level = EventLevel.Verbose, Keywords = Keywords.Random)]
		public abstract void ReceiveStart(string endpoint, bool isAlive);
		[Event(21, Level = EventLevel.Verbose, Keywords = Keywords.Random)]
		public abstract void ReceiveStop(string endpoint, bool isAlive, bool success);

		[Event(22, Level = EventLevel.Verbose, Keywords = Keywords.Socket)]
		public abstract void ReceiveChunk(string endpoint, bool isAlive, int bytesReceived, SocketError status);

		[Event(23, Opcode = Opcodes.Lofasz)]
		public abstract void HelloWorld(string endpoint, bool isAlive, int bytesReceived, bool b, int c, SocketError status);

		public unsafe void HelloWorld2(string endpoint, bool isAlive, int bytesReceived, bool b, int c, SocketError status)
		{
			if (IsEnabled())
			{
				fixed (char* pEndpoint = endpoint)
				{
					var data = stackalloc EventData[6];

					data[0].DataPointer = (IntPtr)(pEndpoint);
					data[0].Size = (endpoint.Length + 1) * 2;

					var alive = isAlive ? 1 : 0;
					data[1].DataPointer = (IntPtr)(&alive);
					data[1].Size = sizeof(int);

					data[2].DataPointer = (IntPtr)(&bytesReceived);
					data[2].Size = sizeof(int);

					data[3].Size = sizeof(int);
					data[3].DataPointer = (IntPtr)(&status);

					alive = b ? 1 : 0;
					data[4].DataPointer = (IntPtr)(&alive);
					data[4].Size = sizeof(int);

					data[5].DataPointer = (IntPtr)(&c);
					data[5].Size = sizeof(int);

					WriteEventCore(22, 6, data);
				}
			}
		}

		public static class Keywords
		{
			public const EventKeywords Socket = (EventKeywords)1;
			public const EventKeywords Random = (EventKeywords)10;
		}

		public static class Opcodes
		{
			public const EventOpcode Lofasz = (EventOpcode)100;
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
