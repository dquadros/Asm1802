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
    class Token
    {
        // Token types
        public enum TokenType 
        { 
            LABEL,      // la..a:
            SYMBOL,     // la..a in symbol table
            DIRECTIVE,  // la..a
            INSTRUCT,   // la..a
            REG,        // Rh
            TEXT,       // la..a
            STRING,     // T'c..c'
            BCONST,     // B'b..b'
            DCONST,     // D'd..d' or d..d
            HCONST,     // #h..h or X'h..h' or h..h
            ERROR 
        };

        private TokenType _type;
        public TokenType Type
        {
            get { return _type; }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
        }

        private Statement.StError _error = Statement.StError.NONE;
        public Statement.StError Error
        {
            get { return _error; }
        }

        // Constructor
        public Token(TokenType type, string text)
        {
            _type = type;
            _text = text;
        }

        // Get next token from line
        public static Token Parse(string line, ref int pos)
        {
            StringBuilder sbText = new StringBuilder();

            while (pos < line.Length)
            {
                if (Char.IsWhiteSpace(line[pos]))
                {
                }
            }

            return new Token(TokenType.TEXT, "");
        }

    }

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
            INV_DEV         // Invalid device number
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
                    st._type = StType.ERROR;
                    st._error = StError.INV_PERIOD;
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
                st._type = StType.ERROR;
                st._error = StError.BAD_START;
            }



            return st;
        }

        // Parses a datalist
        private void ParseDatalist(string text, ref int pos)
        {
        }



        // Generates object code (Pass2)
        public byte[] Generate()
        {
            return code;
        }


    }
}
