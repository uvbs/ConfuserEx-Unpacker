using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConfuserEx_Dynamic_Unpacker
{
    class Program
    {
        public static ModuleDefMD module;
        public static Assembly asm;
        static void Main(string[] args)
        {
            string path = Console.ReadLine();
            module = ModuleDefMD.Load(path);
            asm = Assembly.LoadFrom(path);
            antitamper();
            packer();
            Protections.Constants.constants(); 
            Protections.ReferenceProxy.ProxyFixer(module);
            Protections.ControlFlowRun.cleaner(module);
            ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
            writerOptions.MetaDataOptions.Flags |= MetaDataFlags.PreserveAll;
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            
            module.Write(path + "Cleaned.exe",writerOptions);
        }
        static void packer()
        {
            if (Protections.Packer.IsPacked(module))
            {
                Protections.Packer.findLocal();
                antitamper();
                module.EntryPoint = module.ResolveToken(Protections.Packer.epToken) as MethodDef;
               
            }
        }
        static void antitamper()
        {

            if (Protections.AntiTamper.IsTampered(module) == true)
            {
                Console.WriteLine("Anti Tamper - Detected");
                // byte[] array1 = File.ReadAllBytes(assemblyHelper.reflectionAsm.Location);
                byte[] rawbytes = null;
                //   rawbytes = File.ReadAllBytes(module2.Location);
                //  rawbytes = ConvertToArray(module2 as ModuleDefMD);
                var htdgfd = (module).MetaData.PEImage.CreateFullStream();

                rawbytes = htdgfd.ReadBytes((int)htdgfd.Length);
                module = Protections.AntiTamper.UnAntiTamper(module, rawbytes);
            }
        }
    }
}
