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

        public object write_lock = new object();

        public byte[] tmp_rbuffer = new byte[RBUFFER_SIZE];
        private MemoryStream rbuffer = new MemoryStream();

        public MemoryStream tmp_wbuffer = new MemoryStream();
        private MemoryStream wbuffer = new MemoryStream();

        public int position;

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

        public byte[] GetTmpWBuffer()
        {
            return tmp_wbuffer.ToArray();
        }

        public void ResetTmpWBuffer()
        {
            tmp_wbuffer = new MemoryStream();
        }

        public void WriteTmpWBuffer(byte[] data, int start, int length)
        {
            tmp_wbuffer.Write(data, start, length);
        }

        public void CopyWBufferToTmp()
        {
            byte[] tmp = wbuffer.ToArray();
            tmp_wbuffer.Write(tmp, 0, tmp.Length);
            ResetWBuffer();
        }

        public override string ToString()
        {
            return socket.LocalEndPoint.ToString();
        }
    }
}
