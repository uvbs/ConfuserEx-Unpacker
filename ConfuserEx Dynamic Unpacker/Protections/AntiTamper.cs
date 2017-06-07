using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class AntiTamper
    {
        public string DirectoryName = "";
       
        private static MethodDef antitamp;
        private static uint[] arrayKeys;
        private static byte[] byteResult;
        private static MethodDef cctor;
        private static List<Instruction> dynInstr;
        private static uint[] initialKeys;
       
        private static BinaryReader reader;
        private static MemoryStream input;
        public static ModuleDefMD UnAntiTamper(ModuleDefMD module, byte[] rawbytes)
        {
            dynInstr = new List<Instruction>();
            initialKeys = new uint[4];
            cctor = module.GlobalType.FindStaticConstructor();
            antitamp = cctor.Body.Instructions[0].Operand as MethodDef;
            if (antitamp == null) return null;
            IList<ImageSectionHeader> imageSectionHeaders = module.MetaData.PEImage.ImageSectionHeaders;
            ImageSectionHeader confSec = imageSectionHeaders[0];
            FindInitialKeys(antitamp);
            if (initialKeys == null) return null;
            input = new MemoryStream(rawbytes);
            reader = new BinaryReader(input);
            Hash1(input, reader, imageSectionHeaders, confSec);
            arrayKeys = GetArrayKeys();
            DecryptMethods(reader, confSec, input);
            ModuleDefMD fmd2 = ModuleDefMD.Load(input);
            fmd2.GlobalType.FindStaticConstructor().Body.Instructions.RemoveAt(0);
            return fmd2;
        }
        private static void DecryptMethods(BinaryReader reader, ImageSectionHeader confSec, Stream stream)
        {
            int num = (int)(confSec.SizeOfRawData >> 2);
            int pointerToRawData = (int)confSec.PointerToRawData;
            stream.Position = pointerToRawData;
            uint[] numArray = new uint[num];
            for (uint i = 0; i < num; i++)
            {
                uint num4 = reader.ReadUInt32();
                numArray[i] = num4 ^ arrayKeys[(int)((IntPtr)(i & 15))];
                arrayKeys[(int)((IntPtr)(i & 15))] = num4 + 0x3dbb2819;
            }
            byteResult = new byte[num << 2];
            byteResult = Enumerable.SelectMany<uint, byte>(numArray, new System.Func<uint, IEnumerable<byte>>(BitConverter.GetBytes)).ToArray<byte>();
            byte[] byteArray = ConvertUInt32ArrayToByteArray(numArray);
            stream.Position = pointerToRawData;
            stream.Write(byteResult, 0, byteResult.Length);
        }
        public static bool? IsTampered(ModuleDefMD module)
        {
            var sections = module.MetaData.PEImage.ImageSectionHeaders;

            if (sections.Count == 3)
            {

                return false;
            }

            foreach (var section in sections)
            {
                switch (section.DisplayName)
                {
                    case ".text":
                    case ".rsrc":
                    case ".reloc":
                        continue;
                    default:

                        return true;
                }
            }
            return null;
        }
        private static byte[] ConvertUInt32ArrayToByteArray(uint[] value)
        {
            const int bytesPerUInt32 = 4;
            byte[] result = new byte[value.Length * bytesPerUInt32];
            for (int index = 0; index < value.Length; index++)
            {
                byte[] partialResult = System.BitConverter.GetBytes(value[index]);
                for (int indexTwo = 0; indexTwo < partialResult.Length; indexTwo++)
                    result[index * bytesPerUInt32 + indexTwo] = partialResult[indexTwo];
            }
            return result;
        }
        private static void FindInitialKeys(MethodDef antitamp)
        {
            int count = antitamp.Body.Instructions.Count;
            int num2 = count - 0x125;
            for (int i = 0; i < count; i++)
            {
                Instruction item = antitamp.Body.Instructions[i];
                if (item.OpCode.Equals(OpCodes.Ldc_I4))
                {
                    if (antitamp.Body.Instructions[i + 1].OpCode.Equals(OpCodes.Stloc_S))
                    {
                        if (antitamp.Body.Instructions[i + 1].Operand.ToString().Contains("V_10"))
                        {
                            initialKeys[0] = (uint)((int)item.Operand);
                        }
                        if (antitamp.Body.Instructions[i + 1].Operand.ToString().Contains("V_11"))
                        {
                            initialKeys[1] = (uint)((int)item.Operand);
                        }
                        if (antitamp.Body.Instructions[i + 1].Operand.ToString().Contains("V_12"))
                        {
                            initialKeys[2] = (uint)((int)item.Operand);
                        }
                        if (antitamp.Body.Instructions[i + 1].Operand.ToString().Contains("V_13"))
                        {
                            initialKeys[3] = (uint)((int)item.Operand);
                        }
                    }
                }
            }
        }

        private static uint[] GetArrayKeys()
        {
            uint[] dst = new uint[0x10];
            uint[] src = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                dst[i] = initialKeys[3];
                src[i] = initialKeys[1];
                initialKeys[0] = (initialKeys[1] >> 5) | (initialKeys[1] << 0x1b);
                initialKeys[1] = (initialKeys[2] >> 3) | (initialKeys[2] << 0x1d);
                initialKeys[2] = (initialKeys[3] >> 7) | (initialKeys[3] << 0x19);
                initialKeys[3] = (initialKeys[0] >> 11) | (initialKeys[0] << 0x15);
            }
            return DeriveKeyAntiTamp(dst, src);
        }
        public static uint[] DeriveKeyAntiTamp(uint[] dst, uint[] src)
        {
            uint[] numArray = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                switch ((i % 3))
                {
                    case 0:
                        numArray[i] = dst[i] ^ src[i];
                        break;

                    case 1:
                        numArray[i] = dst[i] * src[i];
                        break;

                    case 2:
                        numArray[i] = dst[i] + src[i];
                        break;
                }
            }
            return numArray;
        }
        private static void Hash1(Stream stream, BinaryReader reader, IList<ImageSectionHeader> sections, ImageSectionHeader confSec)
        {
            foreach (ImageSectionHeader header in sections)
            {
                if ((header != confSec) && (header.DisplayName != ""))
                {
                    int num = (int)(header.SizeOfRawData >> 2);
                    int pointerToRawData = (int)header.PointerToRawData;
                    stream.Position = pointerToRawData;
                    for (int i = 0; i < num; i++)
                    {
                        uint num4 = reader.ReadUInt32();
                        uint num5 = ((initialKeys[0] ^ num4) + initialKeys[1]) + (initialKeys[2] * initialKeys[3]);
                        initialKeys[0] = initialKeys[1];
                        initialKeys[1] = initialKeys[2];
                        initialKeys[1] = initialKeys[3];
                        initialKeys[3] = num5;
                    }
                }
            }
        }

    }
}
