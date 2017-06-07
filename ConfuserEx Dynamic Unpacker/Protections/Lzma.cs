using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    internal static class Lzma
    {
        private const uint kAlignTableSize = 0x10;
        private const uint kEndPosModelIndex = 14;
        private const uint kMatchMinLen = 2;
        private const int kNumAlignBits = 4;
        private const uint kNumFullDistances = 0x80;
        private const int kNumHighLenBits = 8;
        private const uint kNumLenToPosStates = 4;
        private const int kNumLowLenBits = 3;
        private const uint kNumLowLenSymbols = 8;
        private const int kNumMidLenBits = 3;
        private const uint kNumMidLenSymbols = 8;
        private const int kNumPosSlotBits = 6;
        private const int kNumPosStatesBitsMax = 4;
        private const uint kNumPosStatesMax = 0x10;
        private const uint kNumStates = 12;
        private const uint kStartPosModelIndex = 4;

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream inStream = new MemoryStream(data);
            LzmaDecoder decoder = new LzmaDecoder();
            byte[] buffer = new byte[5];
            inStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);
            long outSize = 0L;
            for (int i = 0; i < 8; i++)
            {
                int num3 = inStream.ReadByte();
                outSize |= (long)((long)((ulong)((byte)num3)) << 8 * i);
            }
            byte[] buffer2 = new byte[outSize];
            MemoryStream outStream = new MemoryStream(buffer2, true);
            long inSize = inStream.Length - 13L;
            decoder.Code(inStream, outStream, inSize, outSize);
            return buffer2;
        }

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
        private struct BitDecoder
        {
            public const int kNumBitModelTotalBits = 11;
            public const uint kBitModelTotal = 0x800;
            private const int kNumMoveBits = 5;
            private uint Prob;
            public void Init()
            {
                this.Prob = 0x400;
            }

            public uint Decode(Lzma.Decoder rangeDecoder)
            {
                uint num = (rangeDecoder.Range >> 11) * this.Prob;
                if (rangeDecoder.Code < num)
                {
                    rangeDecoder.Range = num;
                    this.Prob += (uint)((0x800 - this.Prob) >> 5);
                    if (rangeDecoder.Range < 0x1000000)
                    {
                        rangeDecoder.Code = (rangeDecoder.Code << 8) | ((byte)rangeDecoder.Stream.ReadByte());
                        rangeDecoder.Range = rangeDecoder.Range << 8;
                    }
                    return 0;
                }
                rangeDecoder.Range -= num;
                rangeDecoder.Code -= num;
                this.Prob -= this.Prob >> 5;
                if (rangeDecoder.Range < 0x1000000)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | ((byte)rangeDecoder.Stream.ReadByte());
                    rangeDecoder.Range = rangeDecoder.Range << 8;
                }
                return 1;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitTreeDecoder
        {
            private readonly Lzma.BitDecoder[] Models;
            private readonly int NumBitLevels;
            public BitTreeDecoder(int numBitLevels)
            {
                this.NumBitLevels = numBitLevels;
                this.Models = new Lzma.BitDecoder[((int)1) << numBitLevels];
            }

            public void Init()
            {
                for (uint i = 1; i < (((int)1) << this.NumBitLevels); i++)
                {
                    this.Models[i].Init();
                }
            }

            public uint Decode(Lzma.Decoder rangeDecoder)
            {
                uint index = 1;
                for (int i = this.NumBitLevels; i > 0; i--)
                {
                    index = (index << 1) + this.Models[index].Decode(rangeDecoder);
                }
                return (index - (((uint)1) << this.NumBitLevels));
            }

            public uint ReverseDecode(Lzma.Decoder rangeDecoder)
            {
                uint index = 1;
                uint num2 = 0;
                for (int i = 0; i < this.NumBitLevels; i++)
                {
                    uint num4 = this.Models[index].Decode(rangeDecoder);
                    index = index << 1;
                    index += num4;
                    num2 |= num4 << i;
                }
                return num2;
            }

            public static uint ReverseDecode(Lzma.BitDecoder[] Models, uint startIndex, Lzma.Decoder rangeDecoder, int NumBitLevels)
            {
                uint num = 1;
                uint num2 = 0;
                for (int i = 0; i < NumBitLevels; i++)
                {
                    uint num4 = Models[startIndex + num].Decode(rangeDecoder);
                    num = num << 1;
                    num += num4;
                    num2 |= num4 << i;
                }
                return num2;
            }
        }

        private class Decoder
        {
            public uint Code;
            public const uint kTopValue = 0x1000000;
            public uint Range;
            public System.IO.Stream Stream;

            public uint DecodeDirectBits(int numTotalBits)
            {
                uint range = this.Range;
                uint code = this.Code;
                uint num3 = 0;
                for (int i = numTotalBits; i > 0; i--)
                {
                    range = range >> 1;
                    uint num5 = (code - range) >> 0x1f;
                    code -= range & (num5 - 1);
                    num3 = (num3 << 1) | (1 - num5);
                    if (range < 0x1000000)
                    {
                        code = (code << 8) | ((byte)this.Stream.ReadByte());
                        range = range << 8;
                    }
                }
                this.Range = range;
                this.Code = code;
                return num3;
            }

            public void Init(System.IO.Stream stream)
            {
                this.Stream = stream;
                this.Code = 0;
                this.Range = uint.MaxValue;
                for (int i = 0; i < 5; i++)
                {
                    this.Code = (this.Code << 8) | ((byte)this.Stream.ReadByte());
                }
            }

            public void Normalize()
            {
                while (this.Range < 0x1000000)
                {
                    this.Code = (this.Code << 8) | ((byte)this.Stream.ReadByte());
                    this.Range = this.Range << 8;
                }
            }

            public void ReleaseStream()
            {
                this.Stream = null;
            }
        }

        private class LzmaDecoder
        {
            private bool _solid = false;
            private uint m_DictionarySize = uint.MaxValue;
            private uint m_DictionarySizeCheck;
            private readonly Lzma.BitDecoder[] m_IsMatchDecoders = new Lzma.BitDecoder[0xc0];
            private readonly Lzma.BitDecoder[] m_IsRep0LongDecoders = new Lzma.BitDecoder[0xc0];
            private readonly Lzma.BitDecoder[] m_IsRepDecoders = new Lzma.BitDecoder[12];
            private readonly Lzma.BitDecoder[] m_IsRepG0Decoders = new Lzma.BitDecoder[12];
            private readonly Lzma.BitDecoder[] m_IsRepG1Decoders = new Lzma.BitDecoder[12];
            private readonly Lzma.BitDecoder[] m_IsRepG2Decoders = new Lzma.BitDecoder[12];
            private readonly LenDecoder m_LenDecoder = new LenDecoder();
            private readonly LiteralDecoder m_LiteralDecoder = new LiteralDecoder();
            private readonly Lzma.OutWindow m_OutWindow = new Lzma.OutWindow();
            private Lzma.BitTreeDecoder m_PosAlignDecoder = new Lzma.BitTreeDecoder(4);
            private readonly Lzma.BitDecoder[] m_PosDecoders = new Lzma.BitDecoder[0x72];
            private readonly Lzma.BitTreeDecoder[] m_PosSlotDecoder = new Lzma.BitTreeDecoder[4];
            private uint m_PosStateMask;
            private readonly Lzma.Decoder m_RangeDecoder = new Lzma.Decoder();
            private readonly LenDecoder m_RepLenDecoder = new LenDecoder();

            public LzmaDecoder()
            {
                for (int i = 0; i < 4L; i++)
                {
                    this.m_PosSlotDecoder[i] = new Lzma.BitTreeDecoder(6);
                }
            }

            public void Code(Stream inStream, Stream outStream, long inSize, long outSize)
            {
                byte num7;
                this.Init(inStream, outStream);
                Lzma.State state = new Lzma.State();
                state.Init();
                uint distance = 0;
                uint num2 = 0;
                uint num3 = 0;
                uint num4 = 0;
                ulong num5 = 0L;
                ulong num6 = (ulong)outSize;
                if (num5 < num6)
                {
                    this.m_IsMatchDecoders[state.Index << 4].Decode(this.m_RangeDecoder);
                    state.UpdateChar();
                    num7 = this.m_LiteralDecoder.DecodeNormal(this.m_RangeDecoder, 0, 0);
                    this.m_OutWindow.PutByte(num7);
                    num5 += (ulong)1L;
                }
                while (num5 < num6)
                {
                    uint posState = ((uint)num5) & this.m_PosStateMask;
                    if (this.m_IsMatchDecoders[(state.Index << 4) + posState].Decode(this.m_RangeDecoder) == 0)
                    {
                        byte @byte = this.m_OutWindow.GetByte(0);
                        if (!state.IsCharState())
                        {
                            num7 = this.m_LiteralDecoder.DecodeWithMatchByte(this.m_RangeDecoder, (uint)num5, @byte, this.m_OutWindow.GetByte(distance));
                        }
                        else
                        {
                            num7 = this.m_LiteralDecoder.DecodeNormal(this.m_RangeDecoder, (uint)num5, @byte);
                        }
                        this.m_OutWindow.PutByte(num7);
                        state.UpdateChar();
                        num5 += (ulong)1L;
                    }
                    else
                    {
                        uint num10;
                        if (this.m_IsRepDecoders[state.Index].Decode(this.m_RangeDecoder) == 1)
                        {
                            if (this.m_IsRepG0Decoders[state.Index].Decode(this.m_RangeDecoder) == 0)
                            {
                                if (this.m_IsRep0LongDecoders[(state.Index << 4) + posState].Decode(this.m_RangeDecoder) == 0)
                                {
                                    state.UpdateShortRep();
                                    this.m_OutWindow.PutByte(this.m_OutWindow.GetByte(distance));
                                    num5 += (ulong)1L;
                                    continue;
                                }
                            }
                            else
                            {
                                uint num11;
                                if (this.m_IsRepG1Decoders[state.Index].Decode(this.m_RangeDecoder) == 0)
                                {
                                    num11 = num2;
                                }
                                else
                                {
                                    if (this.m_IsRepG2Decoders[state.Index].Decode(this.m_RangeDecoder) == 0)
                                    {
                                        num11 = num3;
                                    }
                                    else
                                    {
                                        num11 = num4;
                                        num4 = num3;
                                    }
                                    num3 = num2;
                                }
                                num2 = distance;
                                distance = num11;
                            }
                            num10 = this.m_RepLenDecoder.Decode(this.m_RangeDecoder, posState) + 2;
                            state.UpdateRep();
                        }
                        else
                        {
                            num4 = num3;
                            num3 = num2;
                            num2 = distance;
                            num10 = 2 + this.m_LenDecoder.Decode(this.m_RangeDecoder, posState);
                            state.UpdateMatch();
                            uint num12 = this.m_PosSlotDecoder[GetLenToPosState(num10)].Decode(this.m_RangeDecoder);
                            if (num12 >= 4)
                            {
                                int numBitLevels = ((int)(num12 >> 1)) - 1;
                                distance = (uint)((2 | (num12 & 1)) << (numBitLevels & 0x1f));
                                if (num12 < 14)
                                {
                                    distance += Lzma.BitTreeDecoder.ReverseDecode(this.m_PosDecoders, (distance - num12) - 1, this.m_RangeDecoder, numBitLevels);
                                }
                                else
                                {
                                    distance += this.m_RangeDecoder.DecodeDirectBits(numBitLevels - 4) << 4;
                                    distance += this.m_PosAlignDecoder.ReverseDecode(this.m_RangeDecoder);
                                }
                            }
                            else
                            {
                                distance = num12;
                            }
                        }
                        if (((distance >= num5) || (distance >= this.m_DictionarySizeCheck)) && (distance == uint.MaxValue))
                        {
                            break;
                        }
                        this.m_OutWindow.CopyBlock(distance, num10);
                        num5 += num10;
                    }
                }
                this.m_OutWindow.Flush();
                this.m_OutWindow.ReleaseStream();
                this.m_RangeDecoder.ReleaseStream();
            }

            private static uint GetLenToPosState(uint len)
            {
                len -= 2;
                if (len < 4)
                {
                    return len;
                }
                return 3;
            }

            private void Init(Stream inStream, Stream outStream)
            {
                uint num;
                this.m_RangeDecoder.Init(inStream);
                this.m_OutWindow.Init(outStream, this._solid);
                for (num = 0; num < 12; num++)
                {
                    for (uint i = 0; i <= this.m_PosStateMask; i++)
                    {
                        uint index = (num << 4) + i;
                        this.m_IsMatchDecoders[index].Init();
                        this.m_IsRep0LongDecoders[index].Init();
                    }
                    this.m_IsRepDecoders[num].Init();
                    this.m_IsRepG0Decoders[num].Init();
                    this.m_IsRepG1Decoders[num].Init();
                    this.m_IsRepG2Decoders[num].Init();
                }
                this.m_LiteralDecoder.Init();
                for (num = 0; num < 4; num++)
                {
                    this.m_PosSlotDecoder[num].Init();
                }
                for (num = 0; num < 0x72; num++)
                {
                    this.m_PosDecoders[num].Init();
                }
                this.m_LenDecoder.Init();
                this.m_RepLenDecoder.Init();
                this.m_PosAlignDecoder.Init();
            }

            public void SetDecoderProperties(byte[] properties)
            {
                int lc = properties[0] % 9;
                int num2 = properties[0] / 9;
                int lp = num2 % 5;
                int pb = num2 / 5;
                uint dictionarySize = 0;
                for (int i = 0; i < 4; i++)
                {
                    dictionarySize += (uint)(properties[1 + i] << (i * 8));
                }
                this.SetDictionarySize(dictionarySize);
                this.SetLiteralProperties(lp, lc);
                this.SetPosBitsProperties(pb);
            }

            private void SetDictionarySize(uint dictionarySize)
            {
                if (this.m_DictionarySize != dictionarySize)
                {
                    this.m_DictionarySize = dictionarySize;
                    this.m_DictionarySizeCheck = Math.Max(this.m_DictionarySize, 1);
                    uint windowSize = Math.Max(this.m_DictionarySizeCheck, 0x1000);
                    this.m_OutWindow.Create(windowSize);
                }
            }

            private void SetLiteralProperties(int lp, int lc)
            {
                this.m_LiteralDecoder.Create(lp, lc);
            }

            private void SetPosBitsProperties(int pb)
            {
                uint numPosStates = ((uint)1) << pb;
                this.m_LenDecoder.Create(numPosStates);
                this.m_RepLenDecoder.Create(numPosStates);
                this.m_PosStateMask = numPosStates - 1;
            }

            private class LenDecoder
            {
                private Lzma.BitDecoder m_Choice = new Lzma.BitDecoder();
                private Lzma.BitDecoder m_Choice2 = new Lzma.BitDecoder();
                private Lzma.BitTreeDecoder m_HighCoder = new Lzma.BitTreeDecoder(8);
                private readonly Lzma.BitTreeDecoder[] m_LowCoder = new Lzma.BitTreeDecoder[0x10];
                private readonly Lzma.BitTreeDecoder[] m_MidCoder = new Lzma.BitTreeDecoder[0x10];
                private uint m_NumPosStates;

                public void Create(uint numPosStates)
                {
                    for (uint i = this.m_NumPosStates; i < numPosStates; i++)
                    {
                        this.m_LowCoder[i] = new Lzma.BitTreeDecoder(3);
                        this.m_MidCoder[i] = new Lzma.BitTreeDecoder(3);
                    }
                    this.m_NumPosStates = numPosStates;
                }

                public uint Decode(Lzma.Decoder rangeDecoder, uint posState)
                {
                    if (this.m_Choice.Decode(rangeDecoder) == 0)
                    {
                        return this.m_LowCoder[posState].Decode(rangeDecoder);
                    }
                    uint num = 8;
                    if (this.m_Choice2.Decode(rangeDecoder) == 0)
                    {
                        num += this.m_MidCoder[posState].Decode(rangeDecoder);
                    }
                    else
                    {
                        num += 8;
                        num += this.m_HighCoder.Decode(rangeDecoder);
                    }
                    return num;
                }

                public void Init()
                {
                    this.m_Choice.Init();
                    for (uint i = 0; i < this.m_NumPosStates; i++)
                    {
                        this.m_LowCoder[i].Init();
                        this.m_MidCoder[i].Init();
                    }
                    this.m_Choice2.Init();
                    this.m_HighCoder.Init();
                }
            }

            private class LiteralDecoder
            {
                private Decoder2[] m_Coders;
                private int m_NumPosBits;
                private int m_NumPrevBits;
                private uint m_PosMask;

                public void Create(int numPosBits, int numPrevBits)
                {
                    if (((this.m_Coders == null) || (this.m_NumPrevBits != numPrevBits)) || (this.m_NumPosBits != numPosBits))
                    {
                        this.m_NumPosBits = numPosBits;
                        this.m_PosMask = (uint)((((int)1) << numPosBits) - 1);
                        this.m_NumPrevBits = numPrevBits;
                        uint num = ((uint)1) << (this.m_NumPrevBits + this.m_NumPosBits);
                        this.m_Coders = new Decoder2[num];
                        for (uint i = 0; i < num; i++)
                        {
                            this.m_Coders[i].Create();
                        }
                    }
                }

                public byte DecodeNormal(Lzma.Decoder rangeDecoder, uint pos, byte prevByte)
                {
                    return this.m_Coders[this.GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
                }

                public byte DecodeWithMatchByte(Lzma.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
                {
                    return this.m_Coders[this.GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
                }

                private uint GetState(uint pos, byte prevByte)
                {
                    return (((pos & this.m_PosMask) << this.m_NumPrevBits) + ((uint)(prevByte >> (8 - this.m_NumPrevBits))));
                }

                public void Init()
                {
                    uint num = ((uint)1) << (this.m_NumPrevBits + this.m_NumPosBits);
                    for (uint i = 0; i < num; i++)
                    {
                        this.m_Coders[i].Init();
                    }
                }

                [StructLayout(LayoutKind.Sequential)]
                private struct Decoder2
                {
                    private Lzma.BitDecoder[] m_Decoders;
                    public void Create()
                    {
                        this.m_Decoders = new Lzma.BitDecoder[0x300];
                    }

                    public void Init()
                    {
                        for (int i = 0; i < 0x300; i++)
                        {
                            this.m_Decoders[i].Init();
                        }
                    }

                    public byte DecodeNormal(Lzma.Decoder rangeDecoder)
                    {
                        uint index = 1;
                        do
                        {
                            index = (index << 1) | this.m_Decoders[index].Decode(rangeDecoder);
                        }
                        while (index < 0x100);
                        return (byte)index;
                    }

                    public byte DecodeWithMatchByte(Lzma.Decoder rangeDecoder, byte matchByte)
                    {
                        uint index = 1;
                        do
                        {
                            uint num2 = (uint)((matchByte >> 7) & 1);
                            matchByte = (byte)(matchByte << 1);
                            uint num3 = this.m_Decoders[(int)((IntPtr)(((1 + num2) << 8) + index))].Decode(rangeDecoder);
                            index = (index << 1) | num3;
                            if (num2 != num3)
                            {
                                while (index < 0x100)
                                {
                                    index = (index << 1) | this.m_Decoders[index].Decode(rangeDecoder);
                                }
                                break;
                            }
                        }
                        while (index < 0x100);
                        return (byte)index;
                    }
                }
            }
        }

        private class OutWindow
        {
            private byte[] _buffer;
            private uint _pos;
            private Stream _stream;
            private uint _streamPos;
            private uint _windowSize;

            public void CopyBlock(uint distance, uint len)
            {
                uint num = (this._pos - distance) - 1;
                if (num >= this._windowSize)
                {
                    num += this._windowSize;
                }
                while (len > 0)
                {
                    if (num >= this._windowSize)
                    {
                        num = 0;
                    }
                    this._buffer[this._pos++] = this._buffer[num++];
                    if (this._pos >= this._windowSize)
                    {
                        this.Flush();
                    }
                    len--;
                }
            }

            public void Create(uint windowSize)
            {
                if (this._windowSize != windowSize)
                {
                    this._buffer = new byte[windowSize];
                }
                this._windowSize = windowSize;
                this._pos = 0;
                this._streamPos = 0;
            }

            public void Flush()
            {
                uint num = this._pos - this._streamPos;
                if (num != 0)
                {
                    this._stream.Write(this._buffer, (int)this._streamPos, (int)num);
                    if (this._pos >= this._windowSize)
                    {
                        this._pos = 0;
                    }
                    this._streamPos = this._pos;
                }
            }

            public byte GetByte(uint distance)
            {
                uint index = (this._pos - distance) - 1;
                if (index >= this._windowSize)
                {
                    index += this._windowSize;
                }
                return this._buffer[index];
            }

            public void Init(Stream stream, bool solid)
            {
                this.ReleaseStream();
                this._stream = stream;
                if (!solid)
                {
                    this._streamPos = 0;
                    this._pos = 0;
                }
            }

            public void PutByte(byte b)
            {
                this._buffer[this._pos++] = b;
                if (this._pos >= this._windowSize)
                {
                    this.Flush();
                }
            }

            public void ReleaseStream()
            {
                this.Flush();
                this._stream = null;
                Buffer.BlockCopy(new byte[this._buffer.Length], 0, this._buffer, 0, this._buffer.Length);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            public uint Index;
            public void Init()
            {
                this.Index = 0;
            }

            public void UpdateChar()
            {
                if (this.Index < 4)
                {
                    this.Index = 0;
                }
                else if (this.Index < 10)
                {
                    this.Index -= 3;
                }
                else
                {
                    this.Index -= 6;
                }
            }

            public void UpdateMatch()
            {
                this.Index = ((this.Index < 7u) ? 7u : 10u);
            }

            public void UpdateRep()
            {
                this.Index = ((this.Index < 7u) ? 8u : 11u);
            }

            public void UpdateShortRep()
            {
                this.Index = ((this.Index < 7u) ? 9u : 11u);
            }

            public bool IsCharState()
            {
                return (this.Index < 7);
            }
        }
    }
}
