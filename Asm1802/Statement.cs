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

        // Errors
        // (adapted from CRA errors)
        public enum StError 
        { 
            NONE,           // No error found
            INV_MNE,        // Invalid mnemonic or missing comma
            DUP_SYM,        // Previously defined symbol
            INV_BCONST,     // Invalid binary constant
            MISSING_CONST,  // A constant was expected
            UNDEF_SYMB,     // Undefined symbol
            MISSING_EXPR,   // An expression was expected
            INV_HCONST,     // Invalid hex constant
            MISSING_QUOTE,  // Missing end quote in string
            INV_PERIOD,     // Invalid '.'
            BAD_START,      // Invalid char at start of statement
            INV_BRANCH,     // Branch out of page
            INV_REG,        // Invalid register number
            INV_DEV,        // Invalid device number
            INV_DCONST,     // Invalid decimal constant
            INV_SYNTAX      // Anything else we cannot accept
        };

        // This fields are determined in the first pass
        private StType _type = StType.NOP;
        public StType Type
        {
            get { return _type; }
        }

        private StError _error = StError.NONE;
        public StError Error
        {
            get { return _error; }
            set
            {
                _error = value;
                _type = StType.ERROR;
            }
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

        private int linenum;

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
        public static Statement Parse(string text, int ln, ref int pos)
        {
            const string delim = " \t;";
            Statement st = new Statement();
            st.linenum = ln;

            // skip leading spaces and/or ';'
            while ((pos < text.Length) && (delim.IndexOf(text[pos]) >= 0))
            {
                pos++;
            }
            if (pos == text.Length)
            {
                return st;  // empty
            }

            // test for comment and datalist
            if (text[pos] == '.')
            {
                pos++;
                if ((pos == text.Length) || (text[pos] != '.'))
                {
                    st.Error = StError.INV_PERIOD;
                }
                pos = text.Length;
                return st;  // comment or error
            }
            else if (text[pos] == ',')
            {
                st.ParseDatalist(text, ref pos);
                return st;
            }
            
            // normal statement
            if (!char.IsLetter(text[pos]))
            {
                st.Error = StError.BAD_START;
                return st;
            }
            Token tk1 = Token.Parse(text, ref pos);
            if (tk1.Type != Token.TokenType.TEXT)
            {
                st.Error = StError.BAD_START;
                return st;
            }
            if (pos < text.Length)
            {
                if (text[pos] == ':')
                {
                    // Label
                    st._label = tk1.Text;
                    pos++;
                    while ((pos < text.Length) && Char.IsWhiteSpace(text[pos]))
                    {
                        pos++;
                    }
                    if (pos >= text.Length)
                    {
                        st._type = StType.NOP;
                        return st;
                    }
                    // Check what is next
                    if (text[pos] == ',')
                    {
                        pos++;
                        st.ParseDatalist(text, ref pos);
                        return st;
                    }
                    tk1 = Token.Parse(text, ref pos);
                    if (tk1.Type != Token.TokenType.TEXT)
                    {
                        st.Error = StError.INV_MNE;
                        return st;
                    }
                }
                else
                {
                    while ((pos < text.Length) && Char.IsWhiteSpace(text[pos]))
                    {
                        pos++;
                    }
                    if ((pos < text.Length) && (text[pos] == '='))
                    {
                        pos++;
                        st._type = StType.EQU;
                        st._label = tk1.Text;
                        st.EvalExpr(text, ref pos);
                        return st;
                    }
                }
            }

            while ((pos < text.Length) && Char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            if (tk1.Text == "DC")
            {
                st._type = StType.DC;
            }
            else if (tk1.Text == "END")
            {
                st._type = StType.END;
            }
            else if (tk1.Text == "ORG")
            {
                st._type = StType.ORG;
            }
            else if (tk1.Text == "PAGE")
            {
                st._type = StType.PAGE;
            }
            else
            {
                Instruction inst = InstrTable.Lookup(tk1.Text);
                if (inst == null)
                {
                    st.Error = StError.INV_MNE;
                    return st;
                }
                st._type = StType.INSTR;
                st._size = inst.Size;
            }

            return st;
        }

        // Parses a datalist
        private void ParseDatalist(string text, ref int pos)
        {
        }

        // Evaluate expression
        private bool EvalExpr(string text, ref int pos)
        {
            _value = 0;
            return true;
        }

        // Generates object code (Pass2)
        public byte[] Generate()
        {
            return code;
        }


    }
}
