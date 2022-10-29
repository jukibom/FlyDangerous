using System;
//using System.Net.NetworkInformation;
using UnityEngine;

namespace Den.Tools
{
	public static class Id
	{
		private static ulong idVersion;

		public static ulong Generate ()
		{
			//timestamp (40 bits, 35 years of milliseconds)
			ulong dateTimeCode = 0;
			TimeSpan dateTime = DateTime.Now - new DateTime(2020,1,1);
			dateTimeCode = (ulong)(dateTime.TotalMilliseconds % 0xFFFFFFFFFF);

			//session number (12 bits, 16383)
			//alternative to machine id 
			//#if UNITY_EDITOR
			//ulong sessionCode = (ulong)(UnityEditor.EditorAnalyticsSessionInfo.sessionCount % 0xFFF);
			//#else
			ulong sessionCode = 0;
			//#endif
			//doesn't work on serialization

			//version (12 bits, 16383)
			//in case several generators are created in one millisecond with script.
			idVersion++;
			if (idVersion >= 0xFFF) idVersion = 1;

			return (dateTimeCode << 40) | (sessionCode << 12) | idVersion;
		}


		public static double GetIdTimestamp (this ulong id) => (id >> 40) / 4.0;
		public static ulong GetIdSession (this ulong id) => (id>>12) & 0xFFFF;
		public static ulong GetIdVersion (this ulong id) => id & 0xFFFF;
		
		public static byte[] ToByteArray (ulong id)
		{
			byte[] arr = new byte[8];
			for (int i=0; i<8; i++)
				arr[7-i] = (byte)((id >> (i*8)) | 0xFF);
			return arr;
		}

		public static string ToString (ulong id)
		{
			string str = "";
			for (int i=13; i>=0; i--)
			{
				int e = (int)((id >> (i*5)) & 0x3F);
				char c = ByteToCharLut[e];
				str += c;
			}
			return str;
		}

		private static string ByteToCharLut = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-+";


		public static ulong MachineId ()
		{
			//machine mac address
			/*int macAddressCode = 0;
			byte[] macAddressBytes = null;
			foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
				if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				{
					macAddressBytes = nic.GetPhysicalAddress().GetAddressBytes();
					break;
				}
			foreach (byte bt in macAddressBytes)
				macAddressCode = macAddressCode ^ bt;*/

			//machine name (if mac address is 0)
			/*int machineNameCode = 0;
			string machineName = Environment.MachineName;
			foreach (char ch in machineName)
				machineNameCode = machineNameCode ^ Convert.ToByte(ch);*/

			//process id
			//int pid = System.Diagnostics.Process.GetCurrentProcess().Id;

			return 0;
		}
	}
}
