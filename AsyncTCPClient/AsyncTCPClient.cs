using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace AsyncTCPClient
{
    public class AsyncTCPClient
    {
        private IPEndPoint endPoint;
        private Connection connection;
        private logging log;
        private InputOutput io;

        public AsyncTCPClient(int port) {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            endPoint = new IPEndPoint(ipAddress, port);

            File.Delete("log.txt");

            log = new logging("");
            io = new InputOutput(log);
        }

        public void Connect() {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.BeginConnect(endPoint, ConnectCallback, listener);
        }        

        private void ConnectCallback(IAsyncResult ar) {
            Socket connection = (Socket)ar.AsyncState;

            this.connection = new Connection(connection);
            io.AddMessageToWriteBuffer(this.connection, new Message(24, new byte[] { 1, 2, 3, 4,5,6 }));
            Send(new Message(24, new byte[] { 1, 2, 3, 4,5,6 }));
        }

        private void Send(Message message)
        {
            io.AddMessageToWriteBuffer(connection, message);
            
            byte[] buffer = connection.wbuffer.ToArray();
            connection.socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None,new AsyncCallback(SendCallback), connection);
        }

        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            Connection connection = (Connection)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = connection.socket.EndSend(ar);
            IOStatus status = io.EndWrite(connection, bytesSent);
            log.add_to_log(log_vrste.info, String.Format("Sent {0} bytes to server.", bytesSent), "AsyncTCPClient.cs SendCallback()");

            if (status == IOStatus.INCOMPLETE)
            {
                byte[] wbuffer = connection.wbuffer.ToArray();
                connection.socket.BeginSend(wbuffer, 0, wbuffer.Length, SocketFlags.None, new AsyncCallback(ReadCallback), connection);
            }
            else
            {
                connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                    new AsyncCallback(ReadCallback), connection);
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;

            int bytesRead = connection.socket.EndReceive(ar);
            IOStatus status;
            List<Message> messages = io.EndRead(connection, bytesRead, out status);

            if (messages.Count > 0)
            {
                foreach (Message message in messages)
                {
                    log.add_to_log(log_vrste.info, String.Format("Handler {0}: received opcode={1}, data={2}", connection.uid, message.opcode, io.ByteArrayToString(message.data)), "AsyncTCPClient.cs ReadCallback()");
                }

                io.AddMessageToWriteBuffer(connection, new Message(24, new byte[] { 1, 2, 3, 4,5,6 }));
                byte[] wbuffer = connection.wbuffer.ToArray();

                //TODO: remove this
                Thread.Sleep(1000);

                connection.socket.BeginSend(wbuffer, 0, wbuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), connection);

                if (status == IOStatus.INCOMPLETE)
                {
                    connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                        new AsyncCallback(ReadCallback), connection);
                }
            }
            else
            {
                connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                    new AsyncCallback(ReadCallback), connection);
            }
        }

        static void Main(string[] args)
        {
            AsyncTCPClient server = new AsyncTCPClient(11000);
            server.Connect();

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
    }
}
