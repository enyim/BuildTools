using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Target
{
	class InterfaceEventSourceTests
	{
		IInterfaceEventSource trace = EventSourceFactory.Get<IInterfaceEventSource>();

		public void Hello()
		{
			trace.ConnectStart("aaaa");
			trace.ConnectStart("aaaa");
			trace.ReceiveStop("aaaa", false, true);
		}
	}

	static class EventSourceFactory
	{
		public static T Get<T>()
		{
			throw new NotImplementedException();
		}
	}

	sealed class AsEventSourceAttribute : Attribute
	{
		public string Name { get; set; }
		public string Guid { get; set; }
	}

	[AsEventSource(Name = "Enyim-Caching-Core")]
	public interface IInterfaceEventSource
	{
		[Event(1, Level = EventLevel.Informational, Keywords = InterfaceEventSourceKeywords.Socket)]
		void ConnectStart(string endpoint);
		[Event(2, Level = EventLevel.Informational, Keywords = InterfaceEventSourceKeywords.Socket)]
		void ConnectStop(string endpoint);

		[Event(3, Level = EventLevel.Error, Keywords = InterfaceEventSourceKeywords.Socket)]
		void ConnectFail(string endpoint, SocketError status);

		[Event(10, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Socket)]
		void SendStart(string endpoint, bool isAlive, int byteCount);
		[Event(11, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Socket)]
		void SendStop(string endpoint, bool isAlive, bool success);

		[Event(12, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Socket)]
		void SendChunk(string endpoint, bool isAlive, int bytesSent, SocketError status);
		//{
		//	if (IsEnabled())
		//	{
		//		fixed (char* pEndpoint = endpoint)
		//		{
		//			var data = stackalloc EventData[4];

		//			data[0].DataPointer = (IntPtr)(pEndpoint);
		//			data[0].Size = (endpoint.Length + 1) * 2;

		//			var alive = isAlive ? 1 : 0;
		//			data[1].DataPointer = (IntPtr)(&alive);
		//			data[1].Size = sizeof(int);

		//			data[2].DataPointer = (IntPtr)(&bytesSent);
		//			data[2].Size = sizeof(int);

		//			data[3].Size = sizeof(int);
		//			data[3].DataPointer = (IntPtr)(&status);

		//			WriteEventCore(12, 4, data);
		//		}
		//	}
		//}

		[Event(20, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Random)]
		void ReceiveStart(string endpoint, bool isAlive);
		[Event(21, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Random)]
		void ReceiveStop(string endpoint, bool isAlive, bool success);

		[Event(22, Level = EventLevel.Verbose, Keywords = InterfaceEventSourceKeywords.Socket)]
		void ReceiveChunk(string endpoint, bool isAlive, int bytesReceived, SocketError status);

		void HelloWorld(string endpoint, bool isAlive, int bytesReceived, SocketError status);
		//{
		//	if (IsEnabled())
		//	{
		//		fixed (char* pEndpoint = endpoint)
		//		{
		//			var data = stackalloc EventData[4];

		//			data[0].DataPointer = (IntPtr)(pEndpoint);
		//			data[0].Size = (endpoint.Length + 1) * 2;

		//			var alive = isAlive ? 1 : 0;
		//			data[1].DataPointer = (IntPtr)(&alive);
		//			data[1].Size = sizeof(int);

		//			data[2].DataPointer = (IntPtr)(&bytesReceived);
		//			data[2].Size = sizeof(int);

		//			data[3].Size = sizeof(int);
		//			data[3].DataPointer = (IntPtr)(&status);

		//			WriteEventCore(22, 4, data);
		//		}
		//	}
		//}
	}

	class InterfaceEventSourceKeywords
	{
		public const EventKeywords Socket = (EventKeywords)1;
		public const EventKeywords Random = (EventKeywords)10;
	}

	class InterfaceEventSourceOpcodes
	{
		public const EventOpcode Lofasz = (EventOpcode)100;
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
