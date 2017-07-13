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
            MISSING_PAREN,  // Missing closing parentheses
            INV_SYNTAX      // Anything else we cannot accept
        };
        public static string MsgError (StError err)
        {
            switch (err)
            {
                case StError.INV_MNE: return "Invalid mnemonic or missing comma";
                case StError.DUP_SYM: return "Previously defined symbol";
                case StError.INV_BCONST: return "Invalid binary constant";
                case StError.MISSING_CONST: return "A constant was expected";
                case StError.UNDEF_SYMB: return "Undefined symbol";
                case StError.MISSING_EXPR: return "An expression was expected";
                case StError.INV_HCONST: return "Invalid hex constant";
                case StError.MISSING_QUOTE: return "Missing end quote";
                case StError.INV_PERIOD: return "Invalid \'.\'";
                case StError.BAD_START: return " Invalid char at start of statement";
                case StError.INV_BRANCH: return "Branch out of page";
                case StError.INV_REG: return "Invalid register number";
                case StError.INV_DEV: return "Invalid device number";
                case StError.INV_DCONST: return "Invalid decimal constant";
                case StError.MISSING_PAREN: return "Missing closing parentheses";
                case StError.INV_SYNTAX: return "Sintax error";
            }
            return "Unknow error";
        }


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
                if (value != StError.NONE)
                {
                    _type = StType.ERROR;
                }
            }
        }
        
        private string _label = "";
        public string Label
        {
            get { return _label; }
        }

        private UInt16 operand = 0;

        private List<string> datalist;

        private UInt16 _size = 0;
        public UInt16 Size
        {
            get { return _size; }
        }

        private int linenum;

        // Final result of value will be determined on second pass,
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
                    SkipSpace(text, ref pos);
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
                    SkipSpace(text, ref pos);
                    if ((pos < text.Length) && (text[pos] == '='))
                    {
                        pos++;
                        st._type = StType.EQU;
                        st._label = tk1.Text;
                        StError err = st.EvalExpr(text, ref pos);
                        if (err != StError.NONE)
                        {
                            st.Error = err;
                        }
                        return st;
                    }
                }
            }

            SkipSpace(text, ref pos);
            if (tk1.Text == "DC")
            {
                st._type = StType.DC;
            }
            else if (tk1.Text == "END")
            {
                Token tk2 = Token.Parse(text, ref pos);

                // Does not have parameters
                if (tk2.Type == Token.TokenType.EMPTY)
                {
                    st._type = StType.END;
                }
                else
                {
                    st.Error = StError.INV_SYNTAX;
                }
            }
            else if (tk1.Text == "ORG")
            {
                st._type = StType.ORG;
                st.Error = st.EvalExpr(text, ref pos);
            }
            else if (tk1.Text == "PAGE")
            {
                Token tk2 = Token.Parse(text, ref pos);

                // Does not have parameters
                if (tk2.Type == Token.TokenType.EMPTY)
                {
                    st._type = StType.PAGE;
                }
                else
                {
                    st.Error = StError.INV_SYNTAX;
                }
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

                // Check operand
                StError err;
                switch (inst.Operand)
                {
                    case OperType.NONE:
                        break;
                    case OperType.REG:
                        st.ParseReg(text, ref pos);
                        break;
                    case OperType.REG1:
                        st.ParseReg(text, ref pos);
                        if ((st.Error == StError.NONE) && (st.operand == 0))
                        {
                            st.Error = StError.INV_REG;
                        }
                        break;
                    case OperType.IODEV:
                        st.ParseIODev(text, ref pos);
                        break;
                    case OperType.SADDR:
                        err = st.EvalExpr(text, ref pos);
                        if (err == StError.NONE)
                        {
                            if (((Program.pc + 1) & 0xFF00) == (st.Value & 0xFF00))
                            {
                                st.operand = (UInt16)(st.Value & 0xFF);
                            }
                            else
                            {
                                st.Error = StError.INV_BRANCH;
                            }
                        }
                        else
                        {
                            st.Error = err;
                        }
                        break;
                    case OperType.LADDR:
                        err = st.EvalExpr(text, ref pos);
                        if (err == StError.NONE)
                        {
                            st.operand = st.Value;
                        }
                        break;
                }

                // Check datalist ...
            }

            return st;
        }

        // Parse a register parameter
        // Updates type, error and operand
        private void ParseReg(string text, ref int pos)
        {
            Token tk = Token.Parse(text, ref pos);
            switch (tk.Type)
            {
                case Token.TokenType.BCONST:
                case Token.TokenType.DCONST:
                case Token.TokenType.HCONST:
                    operand = (UInt16) (EvalConst(tk) & 0xF);
                    break;
                case Token.TokenType.TEXT:
                    Symbol symb = Program.symtab.Lookup(tk.Text);
                    if (symb != null)
                    {
                        operand = (UInt16)(symb.Value & 0xF);
                    }
                    else if ((tk.Text.Length == 2) && (tk.Text[0] == 'R'))
                    {
                        char r = tk.Text[1];
                        if ((r >= '0') && (r <= '9'))
                        {
                            operand = (UInt16)(r - '0');
                        }
                        else if ((r >= 'A') && (r <= 'F'))
                        {
                            operand = (UInt16)(r - 'A' + 10);
                        }
                        else
                        {
                            Error = StError.INV_REG;
                        }
                    }
                    else
                    {
                        Regex rx = new Regex("^[0-9A-F]{1,4}$");
                        if (rx.Match(tk.Text).Success)
                        {
                            // Unprefixed hex constant
                            operand = (UInt16)(EvalConst(tk) & 0xF);
                        }
                        else if (Program.pass == 2)
                        {
                            Error = StError.UNDEF_SYMB;
                        }
                    }

                    break;
                default:
                    Error = StError.INV_SYNTAX;
                    break;
            }
        }

        // Parse a IO device parameter
        // Updates type, error and operand
        private void ParseIODev(string text, ref int pos)
        {
            Token tk = Token.Parse(text, ref pos);
            switch (tk.Type)
            {
                case Token.TokenType.BCONST:
                case Token.TokenType.DCONST:
                case Token.TokenType.HCONST:
                    operand = (UInt16)(EvalConst(tk) & 0x7);
                    break;
                case Token.TokenType.TEXT:
                    Symbol symb = Program.symtab.Lookup(tk.Text);
                    if (symb != null)
                    {
                        operand = (UInt16)(symb.Value & 0x7);
                    }
                    else
                    {
                        Regex rx = new Regex("^[0-9A-F]{1,4}$");
                        if (rx.Match(tk.Text).Success)
                        {
                            // Unprefixed hex constant
                            operand = (UInt16)(EvalConst(tk) & 0x7);
                        }
                        else if (Program.pass == 2)
                        {
                            Error = StError.UNDEF_SYMB;
                        }
                    }
                    break;
                default:
                    Error = StError.INV_SYNTAX;
                    break;
            }
        }

        // Parses a datalist
        private void ParseDatalist(string text, ref int pos)
        {
        }

        // Evaluate expression, put result in _value
        enum ExprMode { NORMAL, ADDR, ADDR_LOW, ADDR_HIGH };
        private StError EvalExpr(string text, ref int pos)
        {
            ExprMode mode = ExprMode.NORMAL;
            StError err = StError.NONE;

            // check for A(sexpr), A.0(sexpr) and A.1(sexpr)
            SkipSpace(text, ref pos);
            if ((char.ToUpper(text[pos]) == 'A') && (pos < (text.Length-1)))
            {
                int aux = pos + 1;
                if (text[aux] == '.')
                {
                    if (aux < text.Length)
                    {
                        aux++;
                        if (text[aux] == '0')
                        {
                            aux++;
                            mode = ExprMode.ADDR_LOW;
                        }
                        else if (text[aux] == '1')
                        {
                            aux++;
                            mode = ExprMode.ADDR_HIGH;
                        }
                        else
                        {
                            return StError.INV_PERIOD;
                        }
                    }
                    else
                    {
                        return StError.INV_PERIOD;
                    }
                }

                if (SkipSpace(text, ref aux) || (text[aux] != '('))
                {
                    if (mode != ExprMode.NORMAL)
                    {
                        return StError.INV_PERIOD;
                    }
                }
                else
                {
                    // all correct (for now)
                    pos = aux + 1;
                    switch (mode)
                    {
                        case ExprMode.NORMAL:
                            mode = ExprMode.ADDR;
                            _value = EvalSimpleExpr(text, ref pos, ref err);
                            break;
                        case ExprMode.ADDR_LOW:
                            _value = (UInt16) (EvalSimpleExpr(text, ref pos, ref err) & 0xFF);
                            break;
                        case ExprMode.ADDR_HIGH:
                            _value = (UInt16) (EvalSimpleExpr(text, ref pos, ref err) >> 8);
                            break;
                    }
                    if (err != StError.NONE)
                    {
                        // something went wrong parsing simple expression
                        return err;
                    }

                    // now find the closing parentheses
                    if (SkipSpace(text, ref pos) || (text[pos] != ')'))
                    {
                        return StError.MISSING_PAREN;
                    }
                    else
                    {
                        pos++;
                        return StError.NONE;
                    }
                }
            }

            _value = EvalSimpleExpr(text, ref pos, ref err);
            return err;
        }

        // Evaluate simple expression
        private UInt16 EvalSimpleExpr(string text, ref int pos, ref StError err)
        {
            UInt16 val = 0;

            if (SkipSpace(text, ref pos))
            {
                err = StError.MISSING_EXPR;
            }
            else
            {
                if (text[pos] == '*')
                {
                    val = Program.pc;
                    pos++;
                }
                else
                {
                    Token tk = Token.Parse(text, ref pos);
                    switch (tk.Type)
                    {
                        case Token.TokenType.BCONST:
                        case Token.TokenType.DCONST:
                        case Token.TokenType.HCONST:
                            return EvalConst(tk);
                        case Token.TokenType.TEXT:
                            Symbol symb = Program.symtab.Lookup(tk.Text);
                            if (symb == null)
                            {
                                // Special case: accept unprefixed hex constants
                                //               starting with a letter
                                Regex rx = new Regex("^[0-9A-F]{1,4}$");
                                if (rx.Match(tk.Text).Success)
                                {
                                    tk.Type = Token.TokenType.HCONST;
                                    return EvalConst(tk);
                                }
                                else
                                {
                                    // error if pass 2
                                }
                            }
                            else
                            {
                                val = symb.Value;
                            }
                            break;
                        default:
                            err = StError.MISSING_EXPR;
                            return 0;
                    }
                }

                // check for +/- const
                if (!SkipSpace(text, ref pos) &&
                    ((text[pos] == '+') || (text[pos] == '-')))
                {
                    char oper = text[pos++];
                    SkipSpace(text, ref pos);
                    Token tk = Token.Parse(text, ref pos);
                    switch (tk.Type)
                    {
                        case Token.TokenType.BCONST:
                        case Token.TokenType.DCONST:
                        case Token.TokenType.HCONST:
                            if (oper == '+')
                            {
                                val += EvalConst(tk);
                            }
                            else
                            {
                                val -= EvalConst(tk);
                            }
                            break;
                        default:
                            err = StError.MISSING_CONST;
                            return 0;
                    }
                }
            }
            return val;
        }

        // Generates object code (Pass2)
        public byte[] Generate()
        {
            return code;
        }

        // Skip space
        // return true if end of line
        private static bool SkipSpace(string text, ref int pos)
        {
            while ((pos < text.Length) && Char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            return pos == text.Length;
        }

        // Eval constant
        private static UInt16 EvalConst(Token tk)
        {
            UInt16 val = 0;

            switch (tk.Type)
            {
                case Token.TokenType.BCONST:
                    foreach (char c in tk.Text)
                    {
                        val = (UInt16)((val << 1) + (c - '0'));
                    }
                    break;
                case Token.TokenType.DCONST:
                    foreach (char c in tk.Text)
                    {
                        val = (UInt16)((val * 10) + (c - '0'));
                    }
                    break;
                case Token.TokenType.HCONST:
                    foreach (char c in tk.Text)
                    {
                        val = (UInt16)(val << 4);
                        if (c <= '9')
                        {
                            val += (UInt16)(c - '0');
                        }
                        else
                        {
                            val += (UInt16)(c - 'A' + 10);
                        }
                    }
                    break;
            }
            return val;
        }

    }
}
