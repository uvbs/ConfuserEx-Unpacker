using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker.Protections
{
    class ControlFlowRun
    {
        private static BlocksCflowDeobfuscator CfDeob;

        public static void DeobfuscateCflow(MethodDef meth)
        {

            for (int i = 0; i < 2; i++)
            {

                CfDeob = new BlocksCflowDeobfuscator();
                Blocks blocks = new Blocks(meth);
                List<Block> test = blocks.MethodBlocks.GetAllBlocks();
                blocks.RemoveDeadBlocks();
                blocks.RepartitionBlocks();

                blocks.UpdateBlocks();
                blocks.Method.Body.SimplifyBranches();
                blocks.Method.Body.OptimizeBranches();
                CfDeob.Initialize(blocks);
                //CfDeob.Deobfuscate();
                CfDeob.Add(new ControlFlow());

                // CfDeob.Add(new Cflow());
                CfDeob.Deobfuscate();
                blocks.RepartitionBlocks();


                IList<Instruction> instructions;
                IList<ExceptionHandler> exceptionHandlers;
                blocks.GetCode(out instructions, out exceptionHandlers);
                DotNetUtils.RestoreBody(meth, instructions, exceptionHandlers);





            }
        }
        public static bool hasCflow(dnlib.DotNet.MethodDef methods)
        {
            for (int i = 0; i < methods.Body.Instructions.Count; i++)
            {
                if (methods.Body.Instructions[i].OpCode == OpCodes.Switch)
                {
                    return true;
                }
            }
            return false;
        }
        public static void cleaner(ModuleDefMD module)
        {
            foreach (TypeDef types in module.GetTypes())
            {
                foreach (MethodDef methods in types.Methods)
                {
                    if (!methods.HasBody) continue;

                    if (hasCflow(methods))
                    {
                        if (Program.veryVerbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("Cleaning Control Flow for " + methods.FullName + "\nThe case order is: ");
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                            
                        
                        DeobfuscateCflow(methods);
                        if(Program.veryVerbose)
                            Console.WriteLine();
                    }



                }
            }
        }
    }
}
