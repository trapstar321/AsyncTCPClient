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

        public const int RBUFFER_SIZE = 2048;

        public int uid;
        public Socket socket;
        public MemoryStream rbuffer = new MemoryStream();
        public MemoryStream wbuffer = new MemoryStream();

        public byte[] bytes_read = new byte[RBUFFER_SIZE];

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

        public override string ToString()
        {
            return socket.LocalEndPoint.ToString();
        }
    }
}
