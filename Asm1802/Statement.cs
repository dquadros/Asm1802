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

        // Value sizes
        // CRA has a few quirks about sizes
        public enum ValueSize { ONE, TWO, FORCE_TWO };

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

        private Instruction inst;
        private UInt16 operand = 0;
        private ValueSize opersize = ValueSize.ONE;
        private List<byte> datalist;

        private UInt16 _size = 0;
        public UInt16 Size
        {
            get { return _size; }
        }

        private int _linenum;
        public int LineNum
        {
            get { return _linenum; }
        }
        private string text;

        // Final result of value will be determined on second pass,
        private UInt16 _value = 0;
        public UInt16 Value
        {
            get { return _value; }
        }
        private ValueSize vs = ValueSize.ONE;

        // Object code, determined at second pass
        private byte[] code;

        // Constructors
        public Statement()
        {
            datalist = new List<byte>();
            code = new byte[0];
        }

        public Statement(string txt, int ln)
        {
            _linenum = ln;
            text = txt;
            datalist = new List<byte>();
            code = new byte[0];
        }

        // Parse the statement, comment and ';' already stripped
        public void Parse()
        {
            int pos = 0;

            // Clear code
            _size = 0;
            datalist = new List<byte>();

            // Skip leading spaces
            if (SkipSpace(ref pos))
            {
                return;         // empty statement
            }

            // Check for datalist only
            if (text[pos] == ',')
            {
                pos++;
                ParseDatalist(ref pos);
                return;         // datalist
            }

            // Normal statement
            if (!char.IsLetter(text[pos]))
            {
                Error = StError.BAD_START;
                return;
            }
            Token tk1 = Token.Parse(text, ref pos);
            if (tk1.Type != Token.TokenType.TEXT)
            {
                Error = StError.BAD_START;
                return;
            }
            if (pos < text.Length)
            {
                if (text[pos] == ':')
                {
                    // Label
                    _label = tk1.Text;
                    pos++;
                    SkipSpace(ref pos);
                    if (pos >= text.Length)
                    {
                        _type = StType.NOP;
                        return;
                    }
                    // Check what is next
                    if (text[pos] == ',')
                    {
                        pos++;
                        ParseDatalist(ref pos);
                        return;
                    }
                    tk1 = Token.Parse(text, ref pos);
                    if (tk1.Type != Token.TokenType.TEXT)
                    {
                        Error = StError.INV_MNE;
                        return;
                    }
                }
                else
                {
                    SkipSpace(ref pos);
                    if ((pos < text.Length) && (text[pos] == '='))
                    {
                        // Equate
                        pos++;
                        _type = StType.EQU;
                        _label = tk1.Text;
                        StError err = EvalExpr(ref pos);
                        if (err != StError.NONE)
                        {
                            Error = err;
                        }
                        return;
                    }
                }
            }

            // Check for directive
            SkipSpace(ref pos);
            if (tk1.Text == "DC")
            {
                _type = StType.DC;
                ParseDatalist(ref pos);
            }
            else if (tk1.Text == "END")
            {
                Token tk2 = Token.Parse(text, ref pos);

                // Does not have parameters
                if (tk2.Type == Token.TokenType.EMPTY)
                {
                    _type = StType.END;
                }
                else
                {
                    Error = StError.INV_SYNTAX;
                }
            }
            else if (tk1.Text == "ORG")
            {
                _type = StType.ORG;
                Error = EvalExpr(ref pos);
            }
            else if (tk1.Text == "PAGE")
            {
                Token tk2 = Token.Parse(text, ref pos);

                // Does not have parameters
                if (tk2.Type == Token.TokenType.EMPTY)
                {
                    _type = StType.PAGE;
                }
                else
                {
                    Error = StError.INV_SYNTAX;
                }
            }
            else
            {
                // Must be an instruction mnemonic
                inst = InstrTable.Lookup(tk1.Text);
                if (inst == null)
                {
                    Error = StError.INV_MNE;
                    return;
                }
                _type = StType.INSTR;
                _size = inst.Size;

                // Check operand
                StError err;
                switch (inst.Operand)
                {
                    case OperType.NONE:
                        break;
                    case OperType.REG:
                        ParseReg(ref pos);
                        break;
                    case OperType.REG1:
                        ParseReg(ref pos);
                        if ((Error == StError.NONE) && (operand == 0))
                        {
                            Error = StError.INV_REG;
                        }
                        break;
                    case OperType.IODEV:
                        ParseIODev(ref pos);
                        break;
                    case OperType.EXPR:
                        err = EvalExpr(ref pos);
                        if (err == StError.NONE)
                        {
                            if (vs == ValueSize.FORCE_TWO)
                            {
                                // CRA Feature or Bug?
                                // LDI A(LABEL) generates three bytes
                                operand = Value;
                                opersize = ValueSize.TWO;
                                _size++;
                            }
                            else
                            {
                                operand = (UInt16)(Value & 0xFF);
                                opersize = ValueSize.ONE;
                            }
                        }
                        else
                        {
                            Error = err;
                        }
                        break;
                    case OperType.SADDR:
                        err = EvalExpr(ref pos);
                        if (err == StError.NONE)
                        {
                            if (((Program.pc + 1) & 0xFF00) == (Value & 0xFF00))
                            {
                                operand = (UInt16)(Value & 0xFF);
                            }
                            else if (Program.pass == 2)
                            {
                                Error = StError.INV_BRANCH;
                            }
                        }
                        else
                        {
                            Error = err;
                        }
                        break;
                    case OperType.LADDR:
                        err = EvalExpr(ref pos);
                        if (err == StError.NONE)
                        {
                            operand = Value;
                            opersize = ValueSize.TWO;
                        }
                        break;
                }

                // Check datalist after instruction
                if (!SkipSpace(ref pos))
                {
                    if (text[pos] == ',')
                    {
                        pos++;
                        ParseDatalist(ref pos);
                    }
                    else
                    {
                        Error = StError.INV_SYNTAX;
                    }
                }
            }

        }

        // Parse a register parameter
        // Updates type, error and operand
        private void ParseReg(ref int pos)
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
                            tk.Type = Token.TokenType.HCONST;
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
        private void ParseIODev(ref int pos)
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
                            tk.Type = Token.TokenType.HCONST;
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
        private void ParseDatalist(ref int pos)
        {
            Token tk;
            while (!SkipSpace(ref pos))
            {
                int aux = pos;
                tk = Token.Parse(text, ref aux);
                if (tk.Type == Token.TokenType.STRING)
                {
                    foreach (char c in tk.Text)
                    {
                        datalist.Add((byte) c);
                    }
                    pos = aux;
                }
                else
                {
                    StError err = EvalExpr(ref pos);
                    if (err == StError.NONE)
                    {
                        if (vs != ValueSize.ONE)
                        {
                            datalist.Add((byte)(Value >> 8));
                        }
                        datalist.Add((byte) (Value & 0xFF));
                    }
                    else
                    {
                        Error = err;
                        break;
                    }
                }

                if (!SkipSpace(ref pos))
                {
                    if (text[pos] != ',')
                    {
                        Error = StError.INV_SYNTAX;
                    }
                    else
                    {
                        pos++;
                    }
                }
            }

            _size += (UInt16) datalist.Count;
        }

        // Evaluate expression, put result in _value
        enum ExprMode { NORMAL, ADDR, ADDR_LOW, ADDR_HIGH };
        private StError EvalExpr(ref int pos)
        {
            ExprMode mode = ExprMode.NORMAL;
            StError err = StError.NONE;
            int nbytes = 1;

            // check for A(sexpr), A.0(sexpr) and A.1(sexpr)
            SkipSpace(ref pos);
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

                if (SkipSpace(ref aux) || (text[aux] != '('))
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
                            _value = EvalSimpleExpr(text, ref pos, ref err, ref nbytes);
                            vs = ValueSize.FORCE_TWO;
                            break;
                        case ExprMode.ADDR_LOW:
                            _value = (UInt16) (EvalSimpleExpr(text, ref pos, ref err, ref nbytes) & 0xFF);
                            vs = ValueSize.ONE;
                            break;
                        case ExprMode.ADDR_HIGH:
                            _value = (UInt16) (EvalSimpleExpr(text, ref pos, ref err, ref nbytes) >> 8);
                            vs = ValueSize.ONE;
                            break;
                    }
                    if (err != StError.NONE)
                    {
                        // something went wrong parsing simple expression
                        return err;
                    }

                    // now find the closing parentheses
                    if (SkipSpace(ref pos) || (text[pos] != ')'))
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

            _value = EvalSimpleExpr(text, ref pos, ref err, ref nbytes);
            vs = (nbytes == 1) ? ValueSize.ONE : ValueSize.TWO;
            return err;
        }

        // Evaluate simple expression
        private UInt16 EvalSimpleExpr(string text, ref int pos, ref StError err, ref int nbytes)
        {
            UInt16 val = 0;

            if (SkipSpace(ref pos))
            {
                err = StError.MISSING_EXPR;
            }
            else
            {
                if (text[pos] == '*')
                {
                    val = Program.pc;
                    pos++;
                    nbytes = 2;
                }
                else
                {
                    Token tk = Token.Parse(text, ref pos);
                    switch (tk.Type)
                    {
                        case Token.TokenType.BCONST:
                        case Token.TokenType.DCONST:
                        case Token.TokenType.HCONST:
                            val = EvalConst(tk);
                            if (val > 0xFF)
                            {
                                nbytes = 2;
                            }
                            return val;
                        case Token.TokenType.STRING:
                            return (UInt16)tk.Text[0];
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
                                    val = EvalConst(tk);
                                    if (val > 0xFF)
                                    {
                                        nbytes = 2;
                                    }
                                    return val;
                                }
                                else
                                {
                                    if (Program.pass == 2)
                                    {
                                        // Error if pass 2
                                        err = StError.UNDEF_SYMB;
                                    }
                                    else
                                    {
                                        val = 0;
                                    }
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
                if (!SkipSpace(ref pos) &&
                    ((text[pos] == '+') || (text[pos] == '-')))
                {
                    nbytes = 2;
                    char oper = text[pos++];
                    SkipSpace(ref pos);
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
            int pos = 0;
            code = new byte[Size];

            if (Type == StType.INSTR)
            {
                switch (inst.Operand)
                {
                    case OperType.NONE:
                        code[pos++] = inst.Opcode;
                        break;
                    case OperType.REG:
                    case OperType.REG1:
                    case OperType.IODEV:
                        code[pos++] = (byte) (inst.Opcode + operand);
                        break;
                    case OperType.EXPR:
                        code[pos++] = inst.Opcode;
                        if (opersize == ValueSize.TWO)
                        {
                            // CRA Feature or bug?
                            code[pos++] = (byte)(operand >> 8);
                        }
                        code[pos++] = (byte)(operand & 0xFF);
                        break;
                    case OperType.SADDR:
                        code[pos++] = inst.Opcode;
                        code[pos++] = (byte) operand;
                        break;
                    case OperType.LADDR:
                        code[pos++] = inst.Opcode;
                        code[pos++] = (byte)(operand >> 8);
                        code[pos++] = (byte)(operand & 0xFF);
                        break;
                }
            }
            foreach (byte b in datalist)
            {
                code[pos++] = b;
            }
            return code;
        }

        // Skip space
        // return true if end of line
        private bool SkipSpace(ref int pos)
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
