
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
            Console.WriteLine("Yeah confuserex unpacker so what\nDrag and drop file");

            string path = Console.ReadLine();
            module = ModuleDefMD.Load(path);
            Console.WriteLine("Do you want to try a static approach or a dynamic approach ( S/D )");
            string value = Console.ReadLine();
            if(value.ToLower() == "s")
            {
                antitamper();
                Protections.ControlFlowRun.cleaner(module);
                Staticpacker();
                try
                {
                    Protections.ReferenceProxy.ProxyFixer(module);
                    Protections.ControlFlowRun.cleaner(module);
                    Protections.StaticStrings.Run(module);
                    
                }
                catch
                {
                    Console.WriteLine("error happened somewhere apart from tamper and packer im too lazy to implement proper error handling");
                }
               
            }
            else if(value.ToLower() == "d")
            {
                asm = Assembly.LoadFrom(path);
                antitamper();
                packer();
                try
                {
                    Protections.Constants.constants();
                    Protections.ReferenceProxy.ProxyFixer(module);
                    Protections.ControlFlowRun.cleaner(module);
                }
                catch
                {
                    Console.WriteLine("error happened somewhere apart from tamper and packer im too lazy to implement proper error handling");
                }
             
            }
            else
            {
                Console.Write("Yeah erm you might be a bit of an idiot follow the instructions");
                Console.ReadLine();
                return;
            }
           
           
            ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
            writerOptions.MetaDataOptions.Flags |= MetaDataFlags.PreserveAll;
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            
            module.Write(path + "Cleaned.exe",writerOptions);
            Console.ReadLine();
        }
       
        static void packer()
        {
            try
            {
                if (Protections.Packer.IsPacked(module))
                {
                    Protections.Packer.findLocal();
                    antitamper();
                    module.EntryPoint = module.ResolveToken(Protections.Packer.epToken) as MethodDef;

                }
            }
            catch
            {
                Console.WriteLine("An error in dynamic packer remover happened");
            }
            
        }
        static void Staticpacker()
        {
            try
            {
                if (Protections.Packer.IsPacked(module))
                {
                    Protections.StaticPacker.Run(module);
                    antitamper();
                    module.EntryPoint = module.ResolveToken(Protections.StaticPacker.epToken) as MethodDef;

                }
            }
            catch
            {
                Console.WriteLine("An error in static packer remover happened");
            }

        }
        static void antitamper()
        {
            try
            {
                if (Protections.AntiTamper.IsTampered(module) == true)
                {

                    byte[] rawbytes = null;

                    var htdgfd = (module).MetaData.PEImage.CreateFullStream();

                    rawbytes = htdgfd.ReadBytes((int)htdgfd.Length);
                    module = Protections.AntiTamper.UnAntiTamper(module, rawbytes);
                }

            }
            catch
            {
                Console.WriteLine("An error in anti tamper remover happened");
            }
            
        }
    }
}
