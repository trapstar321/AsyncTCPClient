using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTCPClient
{
    public class Connection
    {
        private static object c_lock = new object();
        private static int counter = 0;

        public const int RBUFFER_SIZE = 10;

        public int uid;
        public Socket socket;

        public object wbuffer_lock = new object();
        private MemoryStream rbuffer = new MemoryStream();
        private MemoryStream wbuffer = new MemoryStream();

        public byte[] bytes_read = new byte[RBUFFER_SIZE];

        public int position;

        public object sendLock = new object();
        public bool sendComplete = true;

        public Connection(Socket socket)
        {
            lock (c_lock)
            {
                counter += 1;
                uid = counter;
            }
            this.socket = socket;
        }

        public byte[] GetRBuffer()
        {
            return rbuffer.ToArray();            
        }

        public void ResetRBuffer()
        {
            rbuffer = new MemoryStream();         
        }

        public void WriteRBuffer(byte[] data, int start, int length)
        {
            rbuffer.Write(data, start, length);         
        }

        public byte[] GetWBuffer()
        {
            return wbuffer.ToArray();            
        }

        public void ResetWBuffer()
        {
            wbuffer = new MemoryStream();         
        }

        public void WriteWBuffer(byte[] data, int start, int length)
        {
            wbuffer.Write(data, start, length);         
        }

        public override string ToString()
        {
            return socket.LocalEndPoint.ToString();
        }
    }
}
