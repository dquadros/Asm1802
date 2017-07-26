# Asm1802
CDP1802 Level I Assembler

Assembles code for the RCA CDP1892 microprocessor writen
in Level I Assemby Language (as defined in the  Operator 
Manual for the RCA COSMAC Development System II).

UT20.asm in Tests directory is the Monitor Software for
the COSMAC Development System II, as listed in RCA's
Operator Manual. To fully test the assembler, I tried to
type it exactly as listed. For now the load routine is
not included.

Known issues:
* Does not truncate symbol in datalist to 8 bit
* Error messages not very helpful

