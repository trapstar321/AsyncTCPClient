using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTCPClient
{
    public class Message
    {
        private byte opcode_;
        private byte[] data_;

        public Message(byte opcode, byte[] data)
        {
            opcode_ = opcode;
            data_ = data;
        }

        public byte opcode { get { return opcode_; } }
        public byte[] data { get { return data_; } }
    }
}
