/*
 * ASM1802 - CDP1802 Level I Assembler
 * 
 * This module treats a source code statement
 * [Label:] [Mnemonic [operand] [datalist]] [,datalist]
 * 
 * (C) 2017, Daniel Quadros
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Asm1802
{
    public class Statement
    {
        // Statement types
        public enum StType { NOP, DC, EQU, ORG, PAGE, END, INSTR, ERROR };

        // This fields are determined in the first pass
        private StType _type = StType.NOP;
        public StType Type
        {
            get { return _type; }
        }

        private string _label = "";
        public string Label
        {
            get { return _label; }
        }

        private string _mne = "";
        public string Mnemonic
        {
            get { return _mne; }
        }

        private string _oper = "";
        private List<string> datalist;

        private UInt16 _size = 0;
        public UInt16 Size
        {
            get { return _size; }
        }

        // Value will be determined on second pass,
        // except if its an ORG statement
        private UInt16 _value = 0;
        public UInt16 Value
        {
            get { return _value; }
        }

        // Object code, determined at second pass
        private byte[] code;

        // Constructor
        public Statement()
        {
            datalist = new List<string>();
            code = new byte[0];
        }

        // Construct from source code (Pass1)
        public static Statement Parse(string text)
        {
            Statement st = new Statement();
            return st;
        }

        // Generates object code (Pass2)
        public byte[] Generate()
        {
            return code;
        }

        // Breaks a source line in statements
        private enum SPLIT_STATE { TEXT, DOT, STRING };
        public static List<string> Split(string line)
        {
            List<string> result = new List<string>();
            SPLIT_STATE spst;
            int start, pos;

            spst = SPLIT_STATE.TEXT;
            start = pos = 0;
            while (pos < line.Length)
            {
                switch (spst)
                {
                    case SPLIT_STATE.TEXT:
                        if (line[pos] == '.')
                        {
                            spst = SPLIT_STATE.DOT;
                        }
                        else if (line[pos] == '\'')
                        {
                            spst = SPLIT_STATE.STRING;
                        }
                        else if (line[pos] == ';')
                        {
                            if (pos != start)
                            {
                                result.Add(line.Substring(start, pos-start));
                            }
                            start = pos + 1;
                        }
                        pos++;
                        break;
                    case SPLIT_STATE.DOT:
                        if (line[pos] == '.')
                        {
                            // Comment to the end of the line
                            pos = line.Length;
                        }
                        else
                        {
                            spst = SPLIT_STATE.TEXT;
                            pos++;
                        }
                        break;
                    case SPLIT_STATE.STRING:
                        if (line[pos] == '\'')
                        {
                            spst = SPLIT_STATE.TEXT;
                        }
                        pos++;
                        break;
                }
            }
            if (pos != start)
            {
                result.Add(line.Substring(start, pos - start));
            }

            return result;
        }



    }
}
