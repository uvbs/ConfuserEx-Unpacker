using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
   

    class StaticPacker
    {
        private static byte[] initialValue;
        private static uint[] dst;
        private static uint[] src;
        private static MethodDef decryptMethod;
        public static int epToken;

        public static bool Run(ModuleDefMD module)
        {
            MethodDef GetFirstMetohd = module.EntryPoint;
           
         
            uint[] val2 = arrayFinder();
            if (val2 == null)
                return false;
            uint val3 = findLocal();
            if (val3 == 0)
                return false;
            byte[] val = Decrypt(decryptMethod,val2, val3);
            if (val == null)
                return false;
            int value = epStuff(module.EntryPoint);
            if (value == 0)
                return false;
            byte[] epstuff = module.ReadBlob((uint)value);
            if (epstuff == null)
                return false;
            epToken = ((int)epstuff[0] | (int)epstuff[1] << 8 | (int)epstuff[2] << 16 | (int)epstuff[3] << 24);
            Program.module = ModuleDefMD.Load(val);
            Program.module.EntryPoint = Program.module.ResolveToken(epToken) as MethodDef;
            return true;
        }
        public static void PopolateArrays(ulong key, out ulong conv)
        {
            dst = new uint[0x10];
            src = new uint[0x10];
            ulong decryptionKey = key;
            for (int i = 0; i < 0x10; i++)
            {
                decryptionKey = (decryptionKey * decryptionKey) % ((ulong)0x143fc089L);
                src[i] = (uint)decryptionKey;
                dst[i] = (uint)((decryptionKey * decryptionKey) % ((ulong)0x444d56fbL));
            }
            conv = decryptionKey;
        }
        public static int epStuff(MethodDef method)
        {
            for (int i = 38; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].IsLdcI4())
                {
                    //42	0075	callvirt	instance uint8[] [mscorlib]System.Reflection.Module::ResolveSignature(int32)
                    if (method.Body.Instructions[i+1].OpCode == OpCodes.Callvirt&&method.Body.Instructions[i+1].Operand.ToString().Contains("ResolveSignature"))
                    {
                        return method.Body.Instructions[i].GetLdcI4Value();
                    }
                  
                }
            }
            return 0;
        }
        private static byte[] Decrypt(MethodDef meth,uint[] array, uint num)
        {
            PopolateArrays((ulong)num,out ulong conv);
            uint[] uii = DeriveKey(meth,dst, src);
            var fgfff = decryptDataArray(array);
            byte[] arr = Lzma.Decompress(fgfff);
            return decryptDecompData(arr, conv);
        }
        public static byte[] decryptDecompData(byte[] arr, ulong conv)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] buffer = arr;
                buffer[i] = (byte)(buffer[i] ^ ((byte)conv));
                if ((i & 0xff) == 0)
                {
                    conv = (conv * conv) % ((ulong)0x8a5cb7L);
                }
            }
            return arr;
        }
        public static byte[] decryptDataArray(uint[] DataField)
        {
            byte[] buffer = new byte[DataField.Length << 2];
            uint index = 0;
            for (int i = 0; i < DataField.Length; i++)
            {
                uint decryption_key = DataField[i] ^ dst[i & 15];
                dst[i & 15] = (dst[i & 15] ^ decryption_key) + 0x3ddb2819;
                buffer[index] = (byte)decryption_key;
                buffer[((int)index) + 1] = (byte)(decryption_key >> 8);
                buffer[((int)index) + 2] = (byte)(decryption_key >> 0x10);
                buffer[((int)index) + 3] = (byte)(decryption_key >> 0x18);
                index += 4;
            }
            return buffer;
        }
        private static uint[] DeriveKey(MethodDef DecryptMethod,uint[] dst, uint[] src)
        {
            Instruction[] bodyInstr = DecryptMethod.Body.Instructions.ToArray<Instruction>();
            int valCheck;
            if (bodyInstr[48].IsLdcI4())
            {
                valCheck = 48;
            }
            else
            {
                valCheck = 50;
            }
            int num = 0;
            for (int i = valCheck; i < 240; i += 12)
            {
                uint operand2 = (uint)((int)bodyInstr[i].Operand);
                if (bodyInstr[i - 1].OpCode.Equals(OpCodes.Add))
                {
                    dst[num] += src[num];
                }
                if (bodyInstr[i - 1].OpCode.Equals(OpCodes.Mul))
                {
                    dst[num] *= src[num];
                }
                if (bodyInstr[i - 1].OpCode.Equals(OpCodes.Xor))
                {
                    dst[num] ^= src[num];
                }
                if (bodyInstr[i + 1].OpCode.Equals(OpCodes.Add))
                {
                    dst[num] += operand2;
                }
                if (bodyInstr[i + 1].OpCode.Equals(OpCodes.Mul))
                {
                    dst[num] *= operand2;
                }
                if (bodyInstr[i + 1].OpCode.Equals(OpCodes.Xor))
                {
                    dst[num] ^= operand2;
                }
                num++;
            }
            return dst;
        }
       

        public static uint findLocal()
        {
            
            MethodDef entryPoint = Program.module.EntryPoint;
            var aaa = Program.module.CorLibTypes.GetTypeRef("System.Runtime.InteropServices", "GCHandle");
            var tester = Program.module.EntryPoint.Body.Variables.Where(i => i.Type.Namespace == "System.Runtime.InteropServices" && i.Type.TypeName == "GCHandle").ToArray();
            if (tester.Length != 0)
            {
                Local loc = tester[0];
                for (int i = 0; i < entryPoint.Body.Instructions.Count; i++)
                {
                    if (entryPoint.Body.Instructions[i].IsStloc())
                    {
                        if (entryPoint.Body.Instructions[i].GetLocal(entryPoint.Body.Variables) == loc)
                        {
                            if (entryPoint.Body.Instructions[i - 1].OpCode == OpCodes.Call)
                            {
                                if (entryPoint.Body.Instructions[i - 2].IsLdcI4())
                                {
                                    if (entryPoint.Body.Instructions[i - 3].IsLdloc())
                                    {
                                        decryptMethod = entryPoint.Body.Instructions[i - 1].Operand as MethodDef;
                                       
                                      
                                        return (uint)entryPoint.Body.Instructions[i - 2].GetLdcI4Value();
                                       
                                    }


                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }
        private static uint[] arrayFinder()
        {
            MethodDef entryPoint = Program.module.EntryPoint;
            for (int i = 0; i < entryPoint.Body.Instructions.Count; i++)
            {
                if (entryPoint.Body.Instructions[i].OpCode == OpCodes.Stloc_0)
                {
                    
                        if (entryPoint.Body.Instructions[i - 1].OpCode == OpCodes.Call && entryPoint.Body.Instructions[i - 2].OpCode == OpCodes.Ldtoken)
                        {
                            var tester = entryPoint.Body.Instructions[i - 2].Operand as FieldDef;
                        var aa = tester.InitialValue;
                        uint[] decoded = new uint[aa.Length / 4];
                        Buffer.BlockCopy(aa, 0, decoded, 0, aa.Length);

                        return decoded ;
                            
                        }
                    
                }
            }
            return null;
        }
    }
}
