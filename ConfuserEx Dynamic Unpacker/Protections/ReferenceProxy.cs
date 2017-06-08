using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class ReferenceProxy
    {
        private static List<MethodDef> junkMethods = new List<MethodDef>();
        private static object result;
        private static void RemoveJunkMethods(ModuleDefMD module)
        {
            int num = 0;
            foreach (TypeDef current in module.GetTypes())
            {
                List<MethodDef> list = new List<MethodDef>();
                foreach (MethodDef current2 in current.Methods)
                {
                    bool flag = junkMethods.Contains(current2);
                    if (flag)
                    {
                        list.Add(current2);
                    }
                }
                int num2;
                for (int i = 0; i < list.Count; i = num2 + 1)
                {
                    current.Methods.Remove(list[i]);
                    num2 = num;
                    num = num2 + 1;
                    num2 = i;
                }
                list.Clear();
            }
            junkMethods.Clear();
            bool flag2 = num > 0;

        }

        public static int ProxyFixer(ModuleDefMD module)
        {
            int amount = 0;
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Call)
                        {
                            try
                            {
                                MethodDef methodDef = method.Body.Instructions[i].Operand as MethodDef;
                                bool flag2 = methodDef == null;
                                if (!flag2)
                                {
                                    bool flag3 = !methodDef.IsStatic || !type.Methods.Contains(methodDef);
                                    if (!flag3)
                                    {
                                        OpCode opCode;
                                        object proxyValues = GetProxyValues(methodDef, out opCode);
                                        bool flag4 = opCode == null || proxyValues == null;
                                        if (!flag4)
                                        {
                                            method.Body.Instructions[i].OpCode = opCode;
                                            method.Body.Instructions[i].Operand = proxyValues;
                                            amount++;

                                            if (!junkMethods.Contains(methodDef))
                                            {
                                                junkMethods.Add(methodDef);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            RemoveJunkMethods(module);
            return amount;
        }
        private static object GetProxyValues(MethodDef method, out OpCode opCode)
        {
            result = null;
            opCode = null;
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions.Count <= 10)
                {
                    if (method.Body.Instructions[i].OpCode == OpCodes.Call)
                    {
                        opCode = OpCodes.Call;
                        result = method.Body.Instructions[i].Operand;
                        return result;

                    }
                    else if (method.Body.Instructions[i].OpCode == OpCodes.Newobj)
                    {
                        opCode = OpCodes.Newobj;
                        result = method.Body.Instructions[i].Operand;
                        return result;

                    }
                    else if (method.Body.Instructions[i].OpCode == OpCodes.Callvirt)
                    {
                        opCode = OpCodes.Callvirt;
                        result = method.Body.Instructions[i].Operand;
                        return result;
                    }
                    else
                    {
                        opCode = null;
                        result = null;

                    }
                }
                else
                {
                    return null;
                }
            }
            return result;

        }
    }
}
