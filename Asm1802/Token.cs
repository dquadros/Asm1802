/*
 * ASM1802 - CDP1802 Level I Assembler
 * 
 * This contains the token parsing code
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
                    pos--;  // last char is not part of the token
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

}
