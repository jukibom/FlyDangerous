using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Core.Player.HeadTracking {
    public delegate void OpenTrackDataCallback(OpenTrackData Data);

    // This class work properly only if OpenTrack 'Output' set as 'UDP over network' 
    // Local port provided to this class should be same as in OpenTrack 
    // IP of machine using this class should be same as in OpenTrack 
    // If this class using on the same machine with OpenTrack use '127.0.0.1' aka 'localhost' 
    public struct OpenTrackData {
        //public int ReceiveTimeout;          // Timeout for receive operation 
        public bool bLastReceiveSucceed; // Was last receive operation succeed 
        public UdpClient udpClient;
        public OpenTrackDataCallback dataCallback;

        public double x;
        public double y;
        public double z;
        public double yaw;
        public double pitch;
        public double roll;

        // Default constructor, makes OpenTrackData from provided values
        public OpenTrackData(double x = 0, double y = 0, double z = 0, double yaw = 0, double pitch = 0, double roll = 0) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.yaw = yaw;
            this.pitch = pitch;
            this.roll = roll;
            bLastReceiveSucceed = false;
            udpClient = null;
            dataCallback = null;
        }

        // Sync function, parse provided array of bytes into OpenTrackData
        public void SetOpenTrackData(byte[] bytes) {
            if (bytes.Length != 48)
                return;
            if (BitConverter.IsLittleEndian)
                bytes.Reverse();
            x = BitConverter.ToDouble(bytes, 0);
            y = BitConverter.ToDouble(bytes, 8);
            z = BitConverter.ToDouble(bytes, 16);
            yaw = BitConverter.ToDouble(bytes, 24);
            pitch = BitConverter.ToDouble(bytes, 32);
            roll = BitConverter.ToDouble(bytes, 40);
        }

        // Sync function, fill OpenTrackData with values from OpenTrack, return true is OpenTrackData filled properly, false otherwise
        public bool ReceiveOpenTrackData(int localPort, int receiveTimeout = 5000) {
            try {
                udpClient = new UdpClient(localPort);
                udpClient.Client.ReceiveTimeout = receiveTimeout;
                IPEndPoint ep = null;
                var receivedData = udpClient.Receive(ref ep);
                SetOpenTrackData(receivedData);
                bLastReceiveSucceed = true;
            }
            catch (Exception /* e*/) {
                //Console.WriteLine(e.ToString());
                bLastReceiveSucceed = false;
            }

            udpClient.Close();
            return bLastReceiveSucceed;
        }

        // Constructor, make OpenTrackData from provided array of bytes
        public OpenTrackData(byte[] bytes) {
            x = y = z = yaw = pitch = roll = 0;
            bLastReceiveSucceed = false;
            udpClient = null;
            dataCallback = null;
            SetOpenTrackData(bytes);
        }

        // Constructor, make OpenTrackData with values from OpenTrack
        public OpenTrackData(int localPort, int receiveTimeout = 5000) {
            x = y = z = yaw = pitch = roll = 0;
            bLastReceiveSucceed = false;
            udpClient = null;
            dataCallback = null;
            ReceiveOpenTrackData(localPort, receiveTimeout);
        }

        // Functions that fires when received OpenTrack data
        public void OnDataReceived(IAsyncResult res) {
            if (!res.IsCompleted)
                return;
            IPEndPoint ep = null;
            SetOpenTrackData(udpClient.EndReceive(res, ref ep));
            udpClient.Close();
            try {
                dataCallback.Invoke(this);
                bLastReceiveSucceed = true;
            }
            catch (Exception) {
            }
        }

        // Async function, start receiving, fires Callback function when receive was properly finished
        public void ReceiveOpenTrackDataAsync(OpenTrackDataCallback callback, int localPort, int receiveTimeout = 5000) {
            dataCallback = callback;
            bLastReceiveSucceed = false;
            try {
                udpClient = new UdpClient(localPort);
                udpClient.Client.ReceiveTimeout = receiveTimeout;
            }
            catch (Exception /* e*/) {
                //Console.WriteLine(e.ToString());
            }

            try {
                udpClient.BeginReceive(OnDataReceived, null);
            }
            catch (Exception /* e*/) {
                //Console.WriteLine(e.ToString());
            }
        }

        // Async constructor, start receiving, fires Callback function when receive was properly finished
        public OpenTrackData(OpenTrackDataCallback Callback, int LocalPort, int ReceiveTimeout = 5000) {
            x = y = z = yaw = pitch = roll = 0;
            bLastReceiveSucceed = false;
            udpClient = null;
            dataCallback = null;
            ReceiveOpenTrackDataAsync(Callback, LocalPort, ReceiveTimeout);
        }
    }
}