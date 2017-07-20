using AsyncTCPClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTCPClient
{
    public enum IOStatus
    {
        NONE = 0, INCOMPLETE = 1, COMPLETE = 2
    }

    public class InputOutput
    {
        public const int INT_SIZE = 4;
        public const int BYTE_SIZE = 1;
        public const int FIVE_BYTES = 5;

        private logging log;

        public InputOutput(logging log)
        {
            this.log = log;
        }

        public List<Message> EndRead(Connection connection, int bytesRead, out IOStatus status)
        {
            status = IOStatus.NONE;
            List<Message> readMessages = new List<Message>();
            Socket socket = connection.socket;
            byte[] messageData;
            byte[] rbuffer = connection.GetRBuffer();

            log.add_to_log(log_vrste.info, String.Format("Handler {0}: buffer={1}", connection.uid, ByteArrayToString(rbuffer)), "InputOutput.cs EndRead()");
            log.add_to_log(log_vrste.info, String.Format("Handler {0}: read {1} bytes", connection.uid, bytesRead), "InputOutput.cs EndRead()");
            log.add_to_log(log_vrste.info, String.Format("Handler {0}: read {1}", connection.uid, ByteArrayToString(new ArraySegment<byte>(connection.bytes_read, 0, bytesRead).ToArray())), "InputOutput.cs EndRead()");

            if (bytesRead == 0)
                throw new Exception("Client disconnected");

            connection.WriteRBuffer(connection.bytes_read, 0, bytesRead);
            rbuffer = connection.GetRBuffer();

            int length = 0, left, start = 0;
            byte opcode;

            while (connection.position < rbuffer.Length)
            {
                left = (int)rbuffer.Length - connection.position;
                log.add_to_log(log_vrste.info, String.Format("Handler {0}: left={1}", connection.uid, left), "InputOutput.cs EndRead()");

                if (left >= 5)
                {
                    length = BitConverter.ToInt32(rbuffer, connection.position);
                    connection.position += INT_SIZE;
                    opcode = rbuffer[connection.position];
                    connection.position += BYTE_SIZE;

                    if ((left - (FIVE_BYTES)) >= length)
                    {
                        start = FIVE_BYTES;
                        messageData = new ArraySegment<byte>(rbuffer, start, length).ToArray();
                        connection.position += length;

                        log.add_to_log(log_vrste.info, String.Format("Handler {0}: read message, opcode={1}, length={2}, data={3}", connection.uid, opcode, length, ByteArrayToString(messageData)), "InputOutput.cs EndRead()");
                        log.add_to_log(log_vrste.info, String.Format("Handler {0}: at position: {1}", connection.uid, connection.position), "InputOutput.cs EndRead()");
                        readMessages.Add(new Message(opcode, messageData));

                        if (rbuffer.Length > (length + start))
                        {
                            log.add_to_log(log_vrste.info, String.Format("Handler {0}: got parts of new message.", connection.uid), "InputOutput.cs EndRead()");
                            log.add_to_log(log_vrste.info, String.Format("Handler {0}: current buffer={1}", connection.uid, ByteArrayToString(rbuffer)), "InputOutput.cs EndRead()");

                            byte[] tmp = new ArraySegment<byte>(rbuffer, length + start, rbuffer.Length - (length + start)).ToArray();
                            connection.ResetRBuffer();
                            connection.WriteRBuffer(tmp, 0, tmp.Length);
                            rbuffer = connection.GetRBuffer();
                            connection.position = 0;
                            log.add_to_log(log_vrste.info, String.Format("Handler {0}: current buffer length is {1}", connection.uid, rbuffer.Length), "InputOutput.cs EndRead()");
                        }
                        else
                        {
                            log.add_to_log(log_vrste.info, String.Format("Handler {0}: reset buffer", connection.uid), "InputOutput.cs EndRead()");
                            connection.position = 0;
                            connection.ResetRBuffer();
                            break;
                        }
                    }
                    //nema podataka pa utrpaj ostatak u buffer 
                    else
                    {
                        log.add_to_log(log_vrste.info, String.Format("Handler {0}: opcode={1}, length={2}. Whole message not yet received", connection.uid, opcode, length), "InputOutput.cs EndRead()");
                        log.add_to_log(log_vrste.info, String.Format("Handler {0}: at position: {1}", connection.uid, connection.position), "InputOutput.cs EndRead()");
                        log.add_to_log(log_vrste.info, String.Format("Handler {0}: message not complete", connection.uid), "InputOutput.cs EndRead()");

                        status = IOStatus.INCOMPLETE;

                        connection.position = 0;
                        break;
                    }
                }
                //nema headera, utrpaj ostatak u buffer
                else
                {
                    log.add_to_log(log_vrste.info, String.Format("Handler {0}: message not complete, put received back to buffer", connection.uid), "InputOutput.cs EndRead()");
                    break;
                }
            }

            log.add_to_log(log_vrste.info, String.Format("Handler {0}: buffer: {1}", connection.uid, ByteArrayToString(rbuffer)), "InputOutput.cs EndRead()");

            if (status == IOStatus.NONE)
            {
                status = IOStatus.COMPLETE;
            }

            connection.bytes_read = new byte[Connection.RBUFFER_SIZE];
            return readMessages;
        }

        public void EndWrite(Connection connection, int bytesSent)
        {            
            byte[] wbuffer = connection.GetWBuffer();
            if (wbuffer.Length != 0)
            {
                log.add_to_log(log_vrste.info, String.Format("Handler {0}: send {1} to client {2}", connection.uid, ByteArrayToString(wbuffer), connection), "InputOutput.cs EndWrite()");

                if (bytesSent != wbuffer.Length)
                {
                    log.add_to_log(log_vrste.info, String.Format("Handler {0}: not whole buffer was sent for client {1}", connection.uid, connection), "InputOutput.cs EndWrite()");
                    log.add_to_log(log_vrste.info, String.Format("Handler {0}: sent {1} out of {2}", connection.uid, bytesSent, wbuffer.Length), "InputOutput.cs EndWrite()");
                    byte[] tmp = new ArraySegment<byte>(wbuffer, bytesSent, wbuffer.Length - bytesSent).ToArray();
                    connection.ResetWBuffer();
                    connection.WriteWBuffer(tmp, 0, tmp.Length);                        
                }
                else if (bytesSent == 0)
                {
                    throw new Exception("Client disconnected");
                }
                else
                {
                    log.add_to_log(log_vrste.info, String.Format("Handler {0}: whole buffer was sent for client {1}", connection.uid, connection), "InputOutput.cs EndWrite()");
                    connection.ResetWBuffer();
                }
            }            
        }

        public void AddMessageToWriteBuffer(Connection connection, Message message)
        {            
            log.add_to_log(log_vrste.info, String.Format("Handler {0}: send message {1}", connection.uid, ByteArrayToString(message.data)), "InputOutput.cs EndWrite()");

            MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(message.data.Length), 0, INT_SIZE);
            ms.Write(new byte[] { message.opcode }, 0, BYTE_SIZE);
            ms.Write(message.data, 0, message.data.Length);

            byte[] buffer = ms.ToArray();

            log.add_to_log(log_vrste.info, String.Format("Handler {0}: add {1} to write buffer for client {1}", connection.uid, ByteArrayToString(buffer), connection), "InputOutput.cs EndWrite()");            
            connection.WriteWBuffer(buffer, 0, buffer.Length);            
            log.add_to_log(log_vrste.info, String.Format("Handler {0}: write buffer for {1} is {2}", connection.uid, connection, ByteArrayToString(connection.GetWBuffer())), "InputOutput.cs EndWrite()");
        }

        public string ByteArrayToString(byte[] data)
        {
            return string.Join(",", data);
        }
    }
}




