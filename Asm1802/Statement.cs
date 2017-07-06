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
using System.Text.RegularExpressions;

namespace Asm1802
{
    class Token
    {
        // Token types
        public enum TokenType 
        { 
            ERROR,
            EMPTY,      // no token
            STRING,     // T'c..c'
            BCONST,     // B'b..b'
            HCONST,     // #h..h or X'h..h' or h..h
            DCONST,     // D'd..d' or d..d
            TEXT,       // la..a will be classified latter in the following types
            SYMBOL,     // la..a in symbol table
            REG,        // Rh
            DIRECTIVE,  // la..a
            INSTRUCT    // la..a
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
            Token tk = new Token(TokenType.EMPTY, "");
            bool insideQuotes = false;

            // Extract token text
            while (pos < line.Length)
            {
                Char c = line[pos++];
                if (insideQuotes && (line[pos] == '\''))
                {
                    if ((pos < line.Length) && (line[pos] == '\''))
                    {
                        // '' -> '
                        sbText.Append(c);
                        pos++;
                        continue;
                    }
                    else
                    {
                        insideQuotes = false;
                        break;
                    }
                }
                if (insideQuotes)
                {
                    sbText.Append(c);
                }
                else if (c == '\'')
                {
                    sbText.Append(c);
                    insideQuotes = true;
                }
                else if (Char.IsLetterOrDigit(c) || (c == '#'))
                {
                    sbText.Append(c);
                }
                else
                {
                    break;
                }
            }
            tk._text = sbText.ToString();
            if (insideQuotes)
            {
                tk._type = TokenType.ERROR;
                tk._error = Statement.StError.MISSING_QUOTE;
                return tk;
            }

            // Found out what kind of token
            if (tk.Text.Length == 0)
            {
                return tk;
            }
            if (tk.Text[0] == '#')
            {
                tk.CheckHex(1);
            }
            else if (Char.IsLetter(tk.Text[0]))
            {
                tk._text = tk.Text.ToUpper();
                if ((tk.Text.Length > 1) && (tk.Text[1] == '\''))
                {
                    switch (tk.Text[0])
                    {
                        case 'B':
                            tk.CheckBin();
                            break;
                        case 'D':
                            tk.CheckDec(2);
                            break;
                        case 'T':
                            tk.CheckString();
                            break;
                        case 'X':
                            tk.CheckHex(2);
                            break;
                        default:
                            tk._type = TokenType.ERROR;
                            tk._error = Statement.StError.INV_SYNTAX;
                            break;
                    }
                }
                else
                {
                    tk.CheckAlpha();
                }
            }
            else
            {
                // Must be a decimal number
                tk.CheckDec(0);
            }

            return tk;
        }

        // Check that token has only alphanumeric chars
        private void CheckAlpha()
        {
            Regex rx = new Regex("^[0-9A-Z]+$");
            if (rx.Match(Text).Success)
            {
                _type = TokenType.TEXT;
            }
            else
            {
                _type = TokenType.ERROR;
                _error = Statement.StError.INV_SYNTAX;
            }
        }

        // Check if valid binary contant
        private void CheckBin()
        {
            Regex rx = new Regex("^B\'[01]{1,8}$");
            if (rx.Match(Text).Success)
            {
                _type = TokenType.BCONST;
                _text = Text.Substring(2);
            }
            else
            {
                _type = TokenType.ERROR;
                _error = Statement.StError.INV_BCONST;
            }
        }

        // Check if valid decimal contant
        private void CheckHex(int start)
        {
            Regex rx = new Regex("^[0-9A-F]{1,4}$");
            string txt = Text.Substring(start);
            if (rx.Match(txt).Success)
            {
                _type = TokenType.HCONST;
                _text = txt;
            }
            else
            {
                _type = TokenType.ERROR;
                _error = Statement.StError.INV_HCONST;
            }
        }

        // Check if valid hexdecimal contant
        private void CheckDec(int start)
        {
            Regex rx = new Regex("^[0-9]{1,5}$");
            string txt = Text.Substring(start);
            if (rx.Match(txt).Success && (Int32.Parse(txt) <= 0xFFFF))
            {
                _type = TokenType.DCONST;
                _text = txt;
            }
            else
            {
                _type = TokenType.ERROR;
                _error = Statement.StError.INV_DCONST;
            }
        }

        // Check if valid string
        private void CheckString()
        {
            if (Text.Length > 2)
            {
                // T'xxx
                _type = TokenType.STRING;
                _text = Text.Substring(2);
            }
            else
            {
                // T'
                _type = TokenType.ERROR;
                _error = Statement.StError.MISSING_QUOTE;
            }
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
                return st;
            }
            Token tk1 = Token.Parse(text, ref pos);


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
