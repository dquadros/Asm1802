/*
 * ASM1802 - CDP1802 Level I Assembler
 * 
 * Assembles code for the RCA CDP1892 microprocessor writen
 * in Level I Assemby Language (as defined in the  Operator 
 * Manual for the RCA COSMAC Development System II.
 * 
 * Usage: Asm1802 source
 * 
 * (C) 2017, Daniel Quadros
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Asm1802
{
    class Program
    {
        static string SourceFile;       // source file name
        static SymbolTable symtab;          // symbol table
        static string[] source;         // source file in memory
        static List<Statement> lstSt;   // parsed source file
        static UInt16 pc;               // location counter


        // Program entry point
        static void Main(string[] args)
        {
            try
            {
                Console.Out.WriteLine("ASM81 v"+
                    Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." +
                    Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString("D2"));
                if (Init(args))
                {
                    Pass1();
                    Pass2();
                    Info();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("FATAL ERROR: " + ex.Message);
            }
        }

        // Initialization
        // Returns false in case of a serious error
        static bool Init(string[] args)
        {
            // Check parameters
            if (!ParseParam(args))
            {
                Console.Out.WriteLine("usage: ASM1802 source");
                return false;
            }

            // Load source file
            if (!File.Exists(SourceFile))
            {
                Console.Out.WriteLine("ERROR: " + SourceFile + " not found");
                return false;
            }
            source = File.ReadAllLines(SourceFile);

            // Create symbol table
            symtab = new SymbolTable();

            return true;
        }

        // Parses parameters - returns false if invalid
        // For now accepts only the source file name
        static bool ParseParam(string[] args)
        {
            if (args.Length < 1)
            {
                return false;
            }
            if (Path.HasExtension(args[0]))
            {
                SourceFile = args[0];
            }
            else
            {
                SourceFile = args[0] + ".asm";
            }
            return true;
        }

        // First Pass
        // The first pass main objetive is to create the symbol table
        static void Pass1()
        {
            pc = 0;
            foreach (string line in source)
            {
                List<string> parts = Statement.Split(line);
                foreach (string s in parts)
                {
                    Statement st = Statement.Parse(s);
                    if (st.Type == Statement.StType.ERROR)
                    {
                    }
                    else 
                    {
                        if (st.Label != "")
                        {
                            if (symtab.Lookup(st.Label) != null)
                            {
                                Console.Out.WriteLine("Duplicate symbol: " + st.Label);
                            }
                            else
                            {
                                symtab.Add(st.Label, pc);
                            }
                        }
                        if (st.Type == Statement.StType.ORG)
                        {
                            pc = st.Value;
                        }
                        else if (st.Type == Statement.StType.END)
                        {
                            break;  // ignore lines after END
                        }
                        else
                        {
                            pc += st.Size;
                        }
                    }
                }
            }
        }


        // Second Pass
        // The second pass will create the object code
        static void Pass2()
        {
            pc = 0;
        }

        // Prints information about the program
        static void Info()
        {
            symtab.Print();
        }
    }
}
