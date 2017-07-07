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

        // Indicates if instruction need a value passed through a datalist
        private bool _needsData;
        public bool NeedsData
        {
            get { return _needsData; }
        }

        // Number of bytes to generate
        // does not include value passed through a datalist
        private int _size;
        public int Size
        {
            get { return _size; }
        }

        // Constructor
        public Instruction(byte op, OperType oper, bool d, int s)
        {
            _opcode = op;
            _operand = oper;
            _needsData = d;
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
            it.Add("INC", new Instruction(0x10, OperType.REG,  false, 1));
            it.Add("DEC", new Instruction(0x20, OperType.REG,  false, 1));
            it.Add("GLO", new Instruction(0x80, OperType.REG,  false, 1));
            it.Add("GHI", new Instruction(0x90, OperType.REG,  false, 1));
            it.Add("PLO", new Instruction(0xA0, OperType.REG,  false, 1));
            it.Add("PHI", new Instruction(0xB0, OperType.REG,  false, 1));
            it.Add("IRX", new Instruction(0x60, OperType.NONE, false, 1));

            // memory instructions
            it.Add("LDN",  new Instruction(0x00, OperType.REG1, false, 1));
            it.Add("LDA",  new Instruction(0x40, OperType.REG,  false, 1));
            it.Add("LDX",  new Instruction(0xF0, OperType.NONE, false, 1));
            it.Add("LDXA", new Instruction(0x72, OperType.NONE, false, 1));
            it.Add("LDI",  new Instruction(0xF8, OperType.NONE, true,  1));
            it.Add("STR",  new Instruction(0x50, OperType.REG,  false, 1));
            it.Add("STXD", new Instruction(0x73, OperType.NONE, false, 1));

            // logic instructions
            it.Add("OR",   new Instruction(0xF1, OperType.NONE, false, 1));
            it.Add("ORI",  new Instruction(0xF9, OperType.NONE, true, 1));
            it.Add("XOR",  new Instruction(0xF3, OperType.NONE, false, 1));
            it.Add("XRI",  new Instruction(0xFB, OperType.NONE, true, 1));
            it.Add("AND",  new Instruction(0xF2, OperType.NONE, false, 1));
            it.Add("ANI",  new Instruction(0xFA, OperType.NONE, true, 1));
            it.Add("SHR",  new Instruction(0xF6, OperType.NONE, false, 1));
            it.Add("SHRC", new Instruction(0x76, OperType.NONE, false, 1));
            it.Add("RSHR", new Instruction(0x76, OperType.NONE, false, 1));
            it.Add("SHL",  new Instruction(0xFC, OperType.NONE, false, 1));
            it.Add("SHLC", new Instruction(0x7E, OperType.NONE, false, 1));
            it.Add("RSHL", new Instruction(0x7E, OperType.NONE, false, 1));

            // Arithmetic instructions
            it.Add("ADD",  new Instruction(0xF4, OperType.NONE, false, 1));
            it.Add("ADI",  new Instruction(0xFC, OperType.NONE, true, 1));
            it.Add("ADD",  new Instruction(0x74, OperType.NONE, false, 1));
            it.Add("ADCI", new Instruction(0x7C, OperType.NONE, true, 1));
            it.Add("SD",   new Instruction(0xF5, OperType.NONE, false, 1));
            it.Add("SDI",  new Instruction(0xFD, OperType.NONE, true, 1));
            it.Add("SDB",  new Instruction(0x75, OperType.NONE, false, 1));
            it.Add("SDBI", new Instruction(0x7D, OperType.NONE, true, 1));
            it.Add("SM",   new Instruction(0xF7, OperType.NONE, false, 1));
            it.Add("SMI",  new Instruction(0xFF, OperType.NONE, true, 1));
            it.Add("SMB",  new Instruction(0x77, OperType.NONE, false, 1));
            it.Add("SMBI", new Instruction(0x7F, OperType.NONE, true, 1));

            /*
Short Branch
  BR   sa    30
  NBR  sa    38
  BZ   sa    32
  BNZ  sa    3A
  BDF  sa    33
  BPZ  sa    33
  BGE  sa    33
  BNF  sa    3B
  BM   sa    3B
  BL   sa    3B
  BQ   sa    31
  BNQ  sa    39
  B1   sa    34
  BN1  sa    3C
  B2   sa    35
  BN2  sa    3D
  B3   sa    36
  BN3  sa    3E
  B4   sa    37
  BN4  sa    3F
Long Branch
  LBR  la    C0
  NLBR la    C8
  LBZ  la    C2
  LBNZ la    CA
  LBDF la    C3
  LBNF la    CB
  LBQ  la    C1
  LBNQ la    C9
Skip
  SKP        38
  LSKP       C8
  LSZ        CE
  LSNZ       C6
  LSDF       CF
  LSNF       C7
  LSQ        CD
  LSNQ       C5
  LSIE       CC
Control
  IDLE       00
  NOP        C4
  SEP reg    DN
  SEX reg    EN
  SEQ        7B
  REQ        7A
  SAV        78
  MARK       79
  RET        70
  DIS        71
  OUT io     6I  (I = io) 
  INP io     6I  (I = io+8)
           */
        }
    }
}
