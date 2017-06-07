using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class Constants
    {
        public static void constants()
        {
            var manifestModule = Program.asm.ManifestModule;
            foreach(TypeDef types in Program.module.GetTypes())
            {
                foreach(MethodDef methods in types.Methods)
                {
                    if (!methods.HasBody) continue;
                    for(int i = 0; i < methods.Body.Instructions.Count; i++)
                    {
                        if(methods.Body.Instructions[i].OpCode == OpCodes.Call && methods.Body.Instructions[i].Operand.ToString().Contains("tring>")&&methods.Body.Instructions[i].Operand is MethodSpec)
                        {
                            if (methods.Body.Instructions[i - 1].IsLdcI4())
                            {
                                MethodSpec methodSpec = methods.Body.Instructions[i].Operand as MethodSpec;
                                
                                var value = (string)manifestModule.ResolveMethod(methodSpec.MDToken.ToInt32()).Invoke(null,new object[] {(uint) methods.Body.Instructions[i - 1].GetLdcI4Value() });
                                methods.Body.Instructions[i].OpCode = OpCodes.Nop;
                                methods.Body.Instructions[i - 1].OpCode = OpCodes.Ldstr;
                                methods.Body.Instructions[i - 1].Operand = value;
                            }
                        }
                    }
                }
            }
        }
    }
}
