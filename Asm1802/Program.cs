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
        private static string SourceFile;   // source file name
        private static string[] source;     // source file in memory
        public static SymbolTable symtab;   // symbol table
        public static UInt16 pc;            // location counter
        public static int pass;             // pass number
        private static int errcount;        // number of errors in pass2

        // Program entry point
        static void Main(string[] args)
        {
            try
            {
                Console.Out.WriteLine("ASM1802 v"+
                    Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." +
                    Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString("D2"));
                Console.Out.WriteLine("(C) 2017, Daniel Quadros https://dqsoft.blogspot.com");
                Console.Out.WriteLine();
                if (Init(args))
                {
                    Assemble(1);
                    Assemble(2);
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

            errcount = 0;

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

        // Analise the loaded source code
        static void Assemble(int p)
        {
            int sline = 1;          // current source line
            bool ended = false;
            pc = 0;
            pass = p;

            foreach (string line in source)
            {
                // parse statements in current line
                List<string> lstErrors = new List<string>();
                for (int pos = 0; !ended && (pos < line.Length); )
                {
                    Statement st = Statement.Parse(line, sline, ref pos);
                    if (st.Type == Statement.StType.ERROR)
                    {
                        if (pass == 2)
                        {
                            // record error
                            lstErrors.Add(Statement.MsgError(st.Error) + " near column " + (pos + 1).ToString());
                        }

                        // ignore rest of line
                        break;
                    }
                    else
                    {
                        // Treat symbol definitions
                        if (st.Label != "") 
                        {
                            if (pass == 1)
                            {
                                if (symtab.Lookup(st.Label) != null)
                                {
                                    st.Error = Statement.StError.DUP_SYM;
                                }
                                else if (st.Type == Statement.StType.EQU)
                                {
                                    symtab.Add(st.Label, st.Value);
                                    continue;   // that is all this statement
                                }
                                else
                                {
                                    symtab.Add(st.Label, pc);
                                }
                            }
                            else
                            {
                                if (st.Type == Statement.StType.EQU)
                                {
                                    // update value
                                    symtab.Lookup(st.Label).Value = st.Value;
                                    continue;   // that is all this statement
                                }
                            }
                        }

                        // Treat directives
                        if (st.Type == Statement.StType.ORG)
                        {
                            // change PC
                            pc = st.Value;
                        }
                        else if (st.Type == Statement.StType.PAGE)
                        {
                            // Advance PC to start of next page
                            pc = (UInt16) ((pc + 0x100) & 0xFF00);
                        }
                        else if (st.Type == Statement.StType.END)
                        {
                            // end of source program
                            ended = true;   // ignote all text after END
                        }
                        else
                        {
                            // Normal statement
                            if (pass == 2)
                            {
                                // generate object code
                                byte [] code = st.Generate();
                            }
                            pc += st.Size;
                        }
                    }
                }
                if (pass == 2)
                {
                    // List current line
                    // List errors
                    errcount += lstErrors.Count;
                }
                if (ended)
                {
                    break;
                }
                sline++;
            }
            if (!ended)
            {
                Console.Out.WriteLine("Missing END directive");
            }
        }

        // Second Pass
        // The second pass will create the object code
        static void Pass2()
        {
        }

        // Prints information about the program
        static void Info()
        {
            Console.Out.WriteLine(errcount.ToString() + " errors");
            Console.Out.WriteLine();
            symtab.Print();
        }
    }
}
