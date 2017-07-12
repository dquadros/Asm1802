/*
 * ASM1802 - CDP1802 Level I Assembler
 * 
 * This module implements the symbol table
 * 
 * (C) 2017, Daniel Quadros
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Asm1802
{
    // This class stores a symbol
    public class Symbol
    {
        // Name Length for printing
        public const int NameLen = 8;

        // Symbol Name
        private string _name;
        public string Name
        {
            get { return _name; }
        }

        // Does symbol have a value?
        private bool _hasValue = false;
        public bool HasValue
        {
            get { return _hasValue; }
        }

        // Symbol Value
        private UInt16 _value = 0;
        public UInt16 Value
        {
            get { return _value; }
            set 
            { 
                _value = value;
                _hasValue = true;
            }
        }

        // Constructors
        public Symbol(string name)
        {
            _name = name;
        }

        public Symbol(string name, UInt16 value)
        {
            _name = name;
            Value = value;
        }

        // Convert to string for printing
        override public string ToString()
        {
            if (HasValue)
            {
                return Name.PadRight(NameLen, ' ').Substring(0, NameLen) + " " +
                    Value.ToString("X4") + ' ' + Value.ToString().PadLeft(5);
            }
            else
            {
                return Name.PadRight(NameLen, ' ').Substring(0, NameLen) + 
                    " ????";
            }
        }

    }

    // Implements the Symbol Table
    class SymbolTable
    {
        // The table is stored in a dictionary
        private Dictionary<string, Symbol> symtable;

        // Constructor
        public SymbolTable()
        {
            symtable = new Dictionary<string, Symbol>();
        }

        // Lookup a symbol
        // return null if not found
        public Symbol Lookup (string name)
        {
            if (symtable.ContainsKey(name))
            {
                return symtable[name];
            }
            else
            {
                return null;
            }
        }

        // Add a Symbol whithout a value
        public void Add (string name)
        {
            symtable.Add(name, new Symbol(name));
        }

        // Add a Symbol whith a value
        public void Add(string name, UInt16 value)
        {
            symtable.Add(name, new Symbol(name, value));
        }

        // Prints the symbol table to the console
        public void Print()
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine("Symbol Table");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Symbol".PadRight(Symbol.NameLen, ' ') + 
                " Hex    Dec");
            foreach (KeyValuePair<string, Symbol> kvp in symtable)
            {
                Console.Out.WriteLine(kvp.Value.ToString());
            }
            Console.Out.WriteLine();
        }

    }
}
