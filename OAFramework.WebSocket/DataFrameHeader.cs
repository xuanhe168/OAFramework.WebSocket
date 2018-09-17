using System;

namespace OAFramework.WebSocket
{
    internal class DataFrameHeader
    {
        bool _fin, _rsv1, _rsv2, _rsv3, _maskcode;
        sbyte _opcode, _payloadlength;
        public bool FIN { get { return _fin; } }
        public bool RSV1 { get { return _rsv1; } }
        public bool RSV2 { get { return _rsv2; } }
        public bool RSV3 { get { return _rsv3; } }
        public sbyte OpCode { get { return _opcode; } }
        public bool HasMask { get { return _maskcode; } }
        public sbyte Length { get { return _payloadlength; } }
        public DataFrameHeader(byte[] buffer)
        {
            if (buffer.Length < 2) throw new ArgumentException("Invalid data header.");
            // First byte
            _fin = (buffer[0] & 0x80) == 0x80;
            _rsv1 = (buffer[0] & 0x40) == 0x40;
            _rsv2 = (buffer[0] & 0x20) == 0x20;
            _rsv3 = (buffer[0] & 0x10) == 0x10;
            _opcode = (sbyte)(buffer[0] & 0x0f);
            // Second byte
            _maskcode = (buffer[1] & 0x80) == 0x80;
            _payloadlength = (sbyte)(buffer[1] & 0x7f);
        }
        // Send package data
        public DataFrameHeader(bool fin, bool rsv1, bool rsv2, bool rsv3, sbyte opcode, bool hasmask, int length)
        {
            _fin = fin;_rsv1 = rsv1;_rsv2 = rsv2;_rsv3 = rsv3;_opcode = opcode;_maskcode = hasmask;_payloadlength = (sbyte)length;
        }
        // Return Frame header byte
        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2] { 0, 0 };
            if (_fin) buffer[0] ^= 0x80;
            if (_rsv1) buffer[0] ^= 0x40;
            if (_rsv2) buffer[0] ^= 0x20;
            if (_rsv3) buffer[0] ^= 0x10;
            buffer[0] ^= (byte)_opcode;
            if (_maskcode) buffer[1] ^= 0x80;
            buffer[1] ^= (byte)_payloadlength;
            return buffer;
        }
    }
}