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
        private static string SourceFile;               // source file name
        private static string[] source;                 // source file in memory
        private static List<Statement> lstStatements;   // parsed source file
        public static SymbolTable symtab;               // symbol table
        public static UInt16 pc;                        // location counter
        public static int pass;                         // pass number
        private static int errcount;                    // number of errors in pass2

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
                    PreProcess();
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

        // Breaks lines into statements
        private enum PreProcState
        {
            TEXT, STRING, PERIOD
        };
        static void PreProcess()
        {
            int linenum = 1;
            PreProcState state;
            lstStatements = new List<Statement>();
            foreach (string line in source)
            {
                int pos = 0;
                int start = 0;
                state = PreProcState.TEXT;
                while (pos < line.Length)
                {
                    switch (state)
                    {
                        case PreProcState.TEXT:
                            if (line[pos] == '\'')
                            {
                                state = PreProcState.STRING;
                            }
                            else if (line[pos] == '.')
                            {
                                state = PreProcState.PERIOD;
                            }
                            else if (line[pos] == ';')
                            {
                                string text = line.Substring(start, pos - start);
                                lstStatements.Add(new Statement(text, linenum));
                                start = pos + 1;
                            }
                            pos++;
                            break;
                        case PreProcState.PERIOD:
                            if (line[pos] == '.')
                            {
                                string text = line.Substring(start, pos - start - 1);
                                lstStatements.Add(new Statement(text, linenum));
                                start = pos = line.Length;
                            }
                            else
                            {
                                state = PreProcState.TEXT;
                                pos++;
                            }
                            break;
                        case PreProcState.STRING:
                            if (line[pos] == '\'')
                            {
                                state = PreProcState.TEXT;
                            }
                            pos++;
                            break;
                    }
                }
                if (start < pos)
                {
                    string text = line.Substring(start, pos - start);
                    lstStatements.Add(new Statement(text, linenum));
                }
                linenum++;
            }
        }

        // First Pass
        static void Pass1()
        {
            bool ended = false;
            pc = 0;
            pass = 1;

            foreach (Statement st in lstStatements)
            {
                st.Parse();
                if (st.Type != Statement.StType.ERROR)
                {
                    // Treat symbol definitions
                    if (st.Label != "")
                    {
                        Symbol symb = symtab.Lookup(st.Label);
                        if (symb != null)
                        {
                            symb.MarkDup();
                        }
                        else if (st.Type == Statement.StType.EQU)
                        {
                            symtab.Add(st.Label, st.Value);
                            continue;   // that is all in this statement
                        }
                        else
                        {
                            symtab.Add(st.Label, pc);
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
                        break;
                    }
                    else
                    {
                        // Normal statement
                        pc += st.Size;
                    }
                }
            }
            if (!ended)
            {
                Console.Out.WriteLine("Missing END directive");
            }
        }

        // Second pass
        static void Pass2 ()
        {
            List<string> lstErrors = new List<string>();
            byte[] objcode = new byte[0];
            int sline = 0;
            UInt16 linepc = 0;
            pc = 0;
            pass = 2;

            foreach (Statement st in lstStatements)
            {
                if (st.LineNum != sline)
                {
                    if (sline != 0)
                    {
                        // List previous line
                        ListLine(sline, linepc, objcode, lstErrors);
                    }
                    sline = st.LineNum;
                    errcount += lstErrors.Count;
                    lstErrors = new List<string>();
                    linepc = pc;
                    objcode = new byte[0];
                }

                st.Parse();

                if (st.Type == Statement.StType.ERROR)
                {
                    // record error
                    lstErrors.Add(Statement.MsgError(st.Error));
                }
                else
                {
                    // Treat symbol definitions
                    if (st.Label != "") 
                    {
                        Symbol symb = symtab.Lookup(st.Label);
                        if (symb.Duplicate)
                        {
                            lstErrors.Add(Statement.MsgError(Statement.StError.DUP_SYM));
                        }
                        else if (st.Type == Statement.StType.EQU)
                        {
                            // update value
                            symb.Value = st.Value;
                            continue;   // that is all in this statement
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
                        break;
                    }
                    else
                    {
                        // Normal statement or DC directive
                        // generate object code
                        byte [] code = st.Generate();
                        if  (code.Length > 0)
                        {
                            int oldsize = objcode.Length;
                            Array.Resize<byte>(ref objcode, objcode.Length + code.Length);
                            Array.Copy(code, 0, objcode, oldsize, code.Length);
                        }
                        pc += st.Size;
                    }
                }
            }
            // List last line
            ListLine(sline, linepc, objcode, lstErrors);
        }

        // Prints source line
        static void ListLine(int linenum, UInt16 pc, byte[] objcode, List<string> lstErrors)
        {
            int off = 0;
            int nb;

            do
            {
                Console.Write((pc+off).ToString("X4"));
                Console.Write(" ");
                for (nb = 0; (off < objcode.Length) && (nb < 7); nb++, off++)
                {
                    Console.Write(objcode[off].ToString("X2"));
                }
                Console.Write(";");
                while (nb < 7)
                {
                    Console.Write("  ");
                    nb++;
                }
                Console.Write(linenum.ToString(" "));
                Console.Write(linenum.ToString("D4"));
                if (off < 8)
                {
                    Console.Write(" ");
                    Console.Write(source[linenum - 1]);
                }
                Console.WriteLine();
            } while (off < objcode.Length);

            foreach (string errmsg in lstErrors)
            {
                System.Console.Out.WriteLine(">>> " + errmsg);
            }
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
