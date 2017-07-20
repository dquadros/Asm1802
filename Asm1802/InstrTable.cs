/*
 * ASM1802 - CDP1802 Level I Assembler
 * 
 * Intruction table
 * 
 * (C) 2017, Daniel Quadros
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Asm1802
{
    // Type of operand
    // does not include value passed through a datalist
    public enum OperType
    {
        NONE,       // no operand
        REG,        // register (0 to F)
        REG1,       // register (1 to F)
        IODEV,      // io device (0 to 7)
        EXPR,       // expression
        SADDR,      // short address
        LADDR       // long address
    };

    // Defines an 1802 instruction
    class Instruction
    {
        // Opcode, variable bits are 0
        private byte _opcode;
        public byte Opcode
        {
            get { return _opcode; }
        }

        // Type of operand
        private OperType _operand;
        public OperType Operand
        {
            get { return _operand; }
        }

        // Number of bytes to generate
        // does not include value passed through a datalist
        private UInt16 _size;
        public UInt16 Size
        {
            get { return _size; }
        }

        // Constructor
        public Instruction(byte op, OperType oper, UInt16 s)
        {
            _opcode = op;
            _operand = oper;
            _size = s;
        }

    }

    static class InstrTable
    {
        static Dictionary<string, Instruction> it;

        // Constructor
        static InstrTable()
        {
            it = new Dictionary<string, Instruction>();

            // register instructions
            it.Add("INC", new Instruction(0x10, OperType.REG,  1));
            it.Add("DEC", new Instruction(0x20, OperType.REG,  1));
            it.Add("GLO", new Instruction(0x80, OperType.REG,  1));
            it.Add("GHI", new Instruction(0x90, OperType.REG,  1));
            it.Add("PLO", new Instruction(0xA0, OperType.REG,  1));
            it.Add("PHI", new Instruction(0xB0, OperType.REG,  1));
            it.Add("IRX", new Instruction(0x60, OperType.NONE, 1));

            // memory instructions
            it.Add("LDN",  new Instruction(0x00, OperType.REG1, 1));
            it.Add("LDA",  new Instruction(0x40, OperType.REG,  1));
            it.Add("LDX",  new Instruction(0xF0, OperType.NONE, 1));
            it.Add("LDXA", new Instruction(0x72, OperType.NONE, 1));
            it.Add("LDI",  new Instruction(0xF8, OperType.EXPR, 2));
            it.Add("STR",  new Instruction(0x50, OperType.REG,  1));
            it.Add("STXD", new Instruction(0x73, OperType.NONE, 1));

            // logic instructions
            it.Add("OR",   new Instruction(0xF1, OperType.NONE, 1));
            it.Add("ORI",  new Instruction(0xF9, OperType.EXPR, 2));
            it.Add("XOR",  new Instruction(0xF3, OperType.NONE, 1));
            it.Add("XRI",  new Instruction(0xFB, OperType.EXPR, 2));
            it.Add("AND",  new Instruction(0xF2, OperType.NONE, 1));
            it.Add("ANI",  new Instruction(0xFA, OperType.EXPR, 2));
            it.Add("SHR",  new Instruction(0xF6, OperType.NONE, 1));
            it.Add("SHRC", new Instruction(0x76, OperType.NONE, 1));
            it.Add("RSHR", new Instruction(0x76, OperType.NONE, 1));
            it.Add("SHL",  new Instruction(0xFC, OperType.NONE, 1));
            it.Add("SHLC", new Instruction(0x7E, OperType.NONE, 1));
            it.Add("RSHL", new Instruction(0x7E, OperType.NONE, 1));

            // Arithmetic instructions
            it.Add("ADD",  new Instruction(0xF4, OperType.NONE, 1));
            it.Add("ADI",  new Instruction(0xFC, OperType.EXPR, 2));
            it.Add("ADC",  new Instruction(0x74, OperType.NONE, 1));
            it.Add("ADCI", new Instruction(0x7C, OperType.EXPR, 2));
            it.Add("SD",   new Instruction(0xF5, OperType.NONE, 1));
            it.Add("SDI",  new Instruction(0xFD, OperType.EXPR, 2));
            it.Add("SDB",  new Instruction(0x75, OperType.NONE, 1));
            it.Add("SDBI", new Instruction(0x7D, OperType.EXPR, 2));
            it.Add("SM",   new Instruction(0xF7, OperType.NONE, 1));
            it.Add("SMI",  new Instruction(0xFF, OperType.EXPR, 2));
            it.Add("SMB",  new Instruction(0x77, OperType.NONE, 1));
            it.Add("SMBI", new Instruction(0x7F, OperType.EXPR, 2));

            // Short Branch
            it.Add("BR",  new Instruction(0x30, OperType.SADDR, 2));
            it.Add("NBR", new Instruction(0x38, OperType.NONE,  1));
            it.Add("BZ",  new Instruction(0x32, OperType.SADDR, 2));
            it.Add("BNZ", new Instruction(0x3A, OperType.SADDR, 2));
            it.Add("BDF", new Instruction(0x33, OperType.SADDR, 2));
            it.Add("BPZ", new Instruction(0x33, OperType.SADDR, 2));
            it.Add("BGE", new Instruction(0x33, OperType.SADDR, 2));
            it.Add("BNF", new Instruction(0x3B, OperType.SADDR, 2));
            it.Add("BM",  new Instruction(0x3B, OperType.SADDR, 2));
            it.Add("BL",  new Instruction(0x3B, OperType.SADDR, 2));
            it.Add("BQ",  new Instruction(0x31, OperType.SADDR, 2));
            it.Add("BNQ", new Instruction(0x39, OperType.SADDR, 2));
            it.Add("B1",  new Instruction(0x34, OperType.SADDR, 2));
            it.Add("BN1", new Instruction(0x3C, OperType.SADDR, 2));
            it.Add("B2",  new Instruction(0x35, OperType.SADDR, 2));
            it.Add("BN2", new Instruction(0x3D, OperType.SADDR, 2));
            it.Add("B3",  new Instruction(0x36, OperType.SADDR, 2));
            it.Add("BN3", new Instruction(0x3E, OperType.SADDR, 2));
            it.Add("B4",  new Instruction(0x37, OperType.SADDR, 2));
            it.Add("BN4", new Instruction(0x3F, OperType.SADDR, 2));

            // Long Branch
            it.Add("LBR",  new Instruction(0xC0, OperType.LADDR, 3));
            it.Add("NLBR", new Instruction(0xC8, OperType.LADDR, 3));
            it.Add("LBZ",  new Instruction(0xC2, OperType.LADDR, 3));
            it.Add("LBNZ", new Instruction(0xCA, OperType.LADDR, 3));
            it.Add("LBDF", new Instruction(0xC3, OperType.LADDR, 3));
            it.Add("LBNF", new Instruction(0xCB, OperType.LADDR, 3));
            it.Add("LBQ",  new Instruction(0xC1, OperType.LADDR, 3));
            it.Add("LBNQ", new Instruction(0xC9, OperType.LADDR, 3));

            // Skip
            it.Add("SKP",  new Instruction(0x38, OperType.NONE,  1));
            it.Add("LSKP", new Instruction(0xC8, OperType.NONE,  1));
            it.Add("LSZ",  new Instruction(0xCE, OperType.LADDR, 3));
            it.Add("LSNZ", new Instruction(0xC6, OperType.LADDR, 3));
            it.Add("LSDF", new Instruction(0xCF, OperType.LADDR, 3));
            it.Add("LSNF", new Instruction(0xC7, OperType.LADDR, 3));
            it.Add("LSQ",  new Instruction(0xCD, OperType.LADDR, 3));
            it.Add("LSNQ", new Instruction(0xC5, OperType.LADDR, 3));
            it.Add("LSIE", new Instruction(0xCC, OperType.LADDR, 3));

            // Control
            it.Add("IDL",  new Instruction(0x00, OperType.NONE, 1));
            it.Add("NOP",  new Instruction(0xC4, OperType.NONE, 1));
            it.Add("SEP",  new Instruction(0xD0, OperType.REG,  1));
            it.Add("SEX",  new Instruction(0xE0, OperType.REG,  1));
            it.Add("SEQ",  new Instruction(0x7B, OperType.NONE, 1));
            it.Add("REQ",  new Instruction(0x7A, OperType.NONE, 1));
            it.Add("SAV",  new Instruction(0x78, OperType.NONE, 1));
            it.Add("MARK", new Instruction(0x79, OperType.NONE, 1));
            it.Add("RET",  new Instruction(0x70, OperType.NONE, 1));
            it.Add("DIS",  new Instruction(0x71, OperType.NONE, 1));
            it.Add("OUT",  new Instruction(0x60, OperType.IODEV, 1));
            it.Add("INP",  new Instruction(0x68, OperType.IODEV, 1));
        }

        // Lookup an instruction
        // return null if not found
        public static Instruction Lookup(string name)
        {
            if (it.ContainsKey(name))
            {
                return it[name];
            }
            else
            {
                return null;
            }
        }

    }
}
