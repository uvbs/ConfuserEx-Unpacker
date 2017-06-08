
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
       
        public static bool veryVerbose =false;
        private static string path = null;
        private static string mode;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            optionParser(args);
            Console.WriteLine("Yeah confuserex unpacker so what");

            if (path == null||mode == null)
            {
                Console.WriteLine("Check args make sure path and either -d or -s is included (Dynamic or static)"); Console.ReadLine(); return;
            }
                
            module = ModuleDefMD.Load(path);
            
           
          
            if(mode.ToLower() == "static")
            {
                staticRoute();
               
            }
            else if(mode.ToLower() == "dynamic")
            {
                asm = Assembly.LoadFrom(path);
                dynamicRoute();
             
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
        static void staticRoute()
        {
            
            antitamper();
            Protections.ControlFlowRun.cleaner(module);
            Staticpacker();
            try
            {
                Console.WriteLine("[!] Cleaning Proxy Calls");
                int amountProxy = Protections.ReferenceProxy.ProxyFixer(module);
                Console.WriteLine("[!] Amount Of Proxy Calls Fixed: "+amountProxy);
                Protections.ControlFlowRun.cleaner(module);
                Console.WriteLine("[!] Decrytping Strings");
                int strings = Protections.StaticStrings.Run(module);
                Console.WriteLine("[!] Amount Of Strings Decrypted: " + strings);

            }
            catch
            {
                Console.WriteLine("error happened somewhere apart from tamper and packer im too lazy to implement proper error handling");
            }
        }
        static void dynamicRoute()
        {

            antitamper();
            Protections.ControlFlowRun.cleaner(module);
            packer();
            try
            {
                Console.WriteLine("[!] Cleaning Proxy Calls");
                int amountProxy = Protections.ReferenceProxy.ProxyFixer(module);
                Console.WriteLine("[!] Amount Of Proxy Calls Fixed: " + amountProxy);
                Protections.ControlFlowRun.cleaner(module);
                Console.WriteLine("[!] Decrytping Strings");
                int strings = Protections.Constants.constants();
                Console.WriteLine("[!] Amount Of Strings Decrypted: " + strings);

            }
            catch
            {
                Console.WriteLine("error happened somewhere apart from tamper and packer im too lazy to implement proper error handling");
            }
        }
        static void optionParser(string[] str)
        {
            foreach(string arg in str)
            {
                switch (arg)
                {
                   
                    case "-vv":
                        veryVerbose = true;
                      
                        break;
                    case "-d":
                        mode = "dynamic";
                        break;
                    case "-s":
                        mode = "static";
                        break;
                    default:
                        path = arg;
                        break;
                }
            }
        }
        static void packer()
        {
            try
            {
                if (Protections.Packer.IsPacked(module))
                {
                    Console.WriteLine("[!] Compressor Detected");
                    try
                    {
                        Protections.Packer.findLocal();
                        Console.WriteLine("[!] Compressor Removed Successfully");
                        Console.WriteLine("[!] Now Cleaning The koi Module");
                    }
                    catch
                    {
                        Console.WriteLine("[!] Compressor Failed To Remove");
                    }

                    antitamper();
                    module.EntryPoint = module.ResolveToken(Protections.StaticPacker.epToken) as MethodDef;

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
                    Console.WriteLine("[!] Compressor Detected");
                    try
                    {
                        Protections.StaticPacker.Run(module);
                        Console.WriteLine("[!] Compressor Removed Successfully");
                        Console.WriteLine("[!] Now Cleaning The koi Module");
                    }
                    catch
                    {
                        Console.WriteLine("[!] Compressor Failed To Remove");
                    }
                    
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
                    Console.WriteLine("[!] Anti Tamper Detected");

                    byte[] rawbytes = null;

                    var htdgfd = (module).MetaData.PEImage.CreateFullStream();

                    rawbytes = htdgfd.ReadBytes((int)htdgfd.Length);
                    try
                    {
                        module = Protections.AntiTamper.UnAntiTamper(module, rawbytes);
                        Console.WriteLine("[!] Anti Tamper Removed Successfully");
                    }
                    catch
                    {
                        Console.WriteLine("[!] Anti Tamper Failed To Remove");
                    }
                
                }

            }
            catch
            {
                Console.WriteLine("An error in anti tamper remover happened");
            }
            
        }
    }
}
