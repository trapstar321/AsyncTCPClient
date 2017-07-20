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
        public class ReceivedEventArgs : EventArgs
        {
            public Message[] Messages { get; private set; }
            public int ClientID { get; private set; }

            public ReceivedEventArgs(int clientID, Message[] messages)
            {
                Messages = messages;
                ClientID = clientID;
            }
        }

        public delegate void Received(object sender, ReceivedEventArgs e);
        public event Received OnReceived;

        private IPEndPoint endPoint;
        private Connection connection;
        private logging log;
        private InputOutput io;
        private ManualResetEvent connected = new ManualResetEvent(false);        

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

            connected.WaitOne();
        }        

        private void ConnectCallback(IAsyncResult ar) {
            Socket connection = (Socket)ar.AsyncState;

            this.connection = new Connection(connection);
            /*io.AddMessageToWriteBuffer(this.connection, new Message(24, new byte[] { 1, 2, 3, 4,5,6 }));
            Send(new Message(24, new byte[] { 1, 2, 3, 4,5,6 }));*/
            connected.Set();

            log.add_to_log(log_vrste.info, "BeginReceive", "AsyncTCPClient.cs ConnectCallback()");
            this.connection.socket.BeginReceive(this.connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReadCallback), this.connection);
        }

        private void Send(Message message)
        {
            byte[] wbuffer;
            lock (connection.wbuffer_lock)
            {
                io.AddMessageToWriteBuffer(connection, message);
                wbuffer = connection.GetWBuffer();
            }

            log.add_to_log(log_vrste.info, "BeginSend", "AsyncTCPClient.cs Send()");
            lock (connection.sendLock)
            {
                if (connection.sendComplete)
                {
                    connection.sendComplete = false;                    
                    if (wbuffer.Length == 0)
                    {
                        string s = "";
                    }
                    log.add_to_log(log_vrste.info, String.Format("Current buffer: {0}", io.ByteArrayToString(wbuffer)), "AsyncTCPClient.cs Send()");
                    connection.socket.BeginSend(wbuffer, 0, wbuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), connection);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {                        
            log.add_to_log(log_vrste.info, "EndSend", "AsyncTCPClient.cs SendCallback()");
            // Retrieve the socket from the state object.
            Connection connection = (Connection)ar.AsyncState;

            lock (connection.sendLock)
            {
                connection.sendComplete = true;
            }

            // Complete sending the data to the remote device.
            int bytesSent = connection.socket.EndSend(ar);

            if (bytesSent == 0)
            {
                string s = "";
            }

            //lock write buffer to make sure no new messages are added while handling end write
            byte[] wbuffer;
            lock (connection.wbuffer_lock)
            {
                io.EndWrite(connection, bytesSent);
                wbuffer = connection.GetWBuffer();
            }
            log.add_to_log(log_vrste.info, String.Format("Sent {0} bytes to server.", bytesSent), "AsyncTCPClient.cs SendCallback()");

                
            if (wbuffer.Length > 0)
            {
                lock (connection.sendLock)
                    connection.sendComplete = false;
                if (wbuffer.Length == 0)
                {
                    string s = "";
                }
                log.add_to_log(log_vrste.info, String.Format("Current buffer: {0}", io.ByteArrayToString(wbuffer)), "AsyncTCPClient.cs SendCallback()");
                connection.socket.BeginSend(wbuffer, 0, wbuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), connection);
            }            
        }

        public void ReadCallback(IAsyncResult ar)
        {
            log.add_to_log(log_vrste.info, "EndRead", "AsyncTCPClient.cs ReadCallback()");
            Connection connection = (Connection)ar.AsyncState;

            int bytesRead = connection.socket.EndReceive(ar);
            IOStatus status;
            List<Message> messages = io.EndRead(connection, bytesRead, out status);

            if (messages.Count > 0)
            {
                /*connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                        new AsyncCallback(ReadCallback), connection);*/

                /*if (status == IOStatus.INCOMPLETE)
                {
                    connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                        new AsyncCallback(ReadCallback), connection);
                }*/
                ReceivedEventArgs args = new ReceivedEventArgs(connection.uid, messages.ToArray());
                OnReceived?.Invoke(this, args);
            }
            //
            //{
            log.add_to_log(log_vrste.info, "BeginReceive", "AsyncTCPClient.cs ReadCallback()");
            connection.socket.BeginReceive(connection.bytes_read, 0, Connection.RBUFFER_SIZE, SocketFlags.None,
                new AsyncCallback(ReadCallback), connection);
            //}
        }

        static void Main(string[] args)
        {
            AsyncTCPClient client = new AsyncTCPClient(11000);
            client.OnReceived += client.Client_OnReceived;
            client.Connect();
            client.Send(new Message(24, new byte[] { 1, 2, 3, 4, 5, 6 }));

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        private void Client_OnReceived(object sender, ReceivedEventArgs e)
        {
            //Thread.Sleep(1000);
            log.add_to_log(log_vrste.info, String.Format("Received {0}", e.Messages.Length), "AsyncTCPClient.cs Client_OnReceived()");
            AsyncTCPClient client = (AsyncTCPClient)sender;
            client.Send(new Message(24, new byte[] { 1,2,3,4,5,6}));
        }
    }
}
