
..  UT20 IS A UTILITY PROGRAM USED TO ALTER
..  MEMORY, DUMP MEMORY, AND BEGIN PROGRAM
..  EXECUTION ATA AGIVEN LOCATION. THE COMMANDS
..  ...
   PTER=#00   ..AUXILIARY FOR MAIN ROUTINE
     CL=#01   ..CLOBBERED
     ST=#02   ..STACK POINTER ONLU REFERENCE TO RAM
    SUB=#03   ..SUBROUTINE PROGRAM COUNTER
     PC=#05   ..MAIN PROGRAM COUNTER
 SWITCH=CL    ..DISTINGUISHES BETWEEM ?M AND !M
  DELAY=#0C   ..DELAY ROUTINE PROGRAM COUNTER
    ASL=#0D   ..HEX ASSEMBLY REGISTER ON INPUT;
              ..AUX FOR HEX OUTPUT
  CNTER=ASL   ..USED TO COUNT OUTPUT BYTES
    AUX=#0E   ..AUX.1 HOLDS BIT-TIME CONSTANT
   CHAR=#0F   ..CHAR.1 HOLDS I/O BYTE
   WRAM=#8C1F ..REGISTERS STORED IN TAM
 LOADER=#8400 ..LOCATION LOADER PROGRAM
..
..  ENTER IN R0
..
        ORG#8000                ..UT20 STARTS AT
                                ..M(8000)
        DIS,#00                 ..P=X=0
        LDI A.1(UT20) ;PHI R0   ..HOLDS HIGH BIT
                                ..AFTER FINGER OFF
..  MAY TRY TO GO TO 8000, NOT 0000
..  UNTIL FINGER IS OFF BUTTON
..
..  THE FOLLOWING WRITES REGISTER CONTENTS INTO
..  WRAM-32 THRU WRAM IF IT EXISTS. WRAM-34 ID
..  ASSUMED NOT TO BE RAM (ELSE ROUTINE OVERRUNS).

..
        LDI A.1(WRAM) ;PHI CL   ..CL IS CLOB-
                                ..BERED
        LDI A.0(WRAM-1) ;PLO CL ..SET UP WHERE RF.0
                                ..IS TO GO, MINUS 1
        LDI #A0 ;PHI R4         ..R4.1 STORES A
                                ..MODIFIED INSTRUC.
        SEX CL
LOOP2:  LDI #D0 ;STR CL         ..SET UP SEP INSTR.
                                ..FOR RETURN
        XOR                     ..CHECK IT WROTE
        BNZ UT20
        DEC CL                  ..PREPARE FOR MODI-
                                ..FIED INSTRUCTION
        GHI R4 ;ADI #70         ..IN THE 90'S?
        BDF*+#04
        ADI #21                 ..NO, 8N -> 9N
        ADI #7F                 ..YES, 9N -> 8(N-1)
        PHI R4 ;STR CL          ..SET MODIFIED
                                ..INSTRUC INTO RAM
        SEP CL                  ..EXECUTE INSTRUCS
                                ..(80-9F)
        STXD                    ..STORE RESULT RAM
        DEC CL                  ..& VACK UP FOR
        GHI R4 ;XRI#90        ..CK IF STORAGE DONE
        BNZ LOOP2             ..NEXT BYTE
..
UT20:   GHI R0 ;PHI PC ;PHI SUB ..#80->PC.1 & SUB.1
        LDI A.0(UT20A) ;PLO PC
        SEP PC
UT20A:  SEX PC
        DIS,#55                 ..NOTE PC=5 ASSUMED
        OUT 1,#01               ..SELECT RCA FROUP
        LDI A.1(WRAM)   ;PHI ST ..SET STACK POINTER
        LDI#00   ;PLO ST
                                ..TO M(8C00), ONLY
                                ..RAM USED
        LDI A.0(TIMALC) ;PLO SUB..READ ONE CHAR
                                ..TO SET TIMER
        SEP SUB
..
... INITIATION NOW DONE
..
START:  LDI A.0(TYPE5D) ;PLO SUB
        LDI A.1(TYPE5D)   ;PHI SUB
        SEP SUB; ,#0D           ..CR=CARRIAGE RET
ST2:    SEP SUB; ,#0A           ..LF=LINE FEED
        SEP SUB; ,#2A           ..* PROMPT CHARAC
IGNORE: LDI#00 ;PLO ASL;PHI ASL ..PREPARE TO INPUT
                                ..HEX DIGITS,
                                ..CLEAR ASL
        LDI A.0(READAH) ;PLO SUB
        SEP SUB                 ..INPUT COMMAND
        XRI #24                 ..IS IT "$" ?
        LBZ DOLLAR
        XRI #05                 ..IS IT "!" ?
                                ..TEST $ XRI !
        PLO SWITCH              ..AND SAVE RESULT
        LSZ
        XRI #1E                 ..IS IT "?" ?
                                ..TES $ XRI ! XRI ?
        BNZ IGNORE              ..IGNORE ALL UNTIL
                                ..COMMAND IS READ
..
..  THE FOLLOWING IS COMMON FOR ?M AND !M
..  (SWITCH.0 = 0 FOR LATTER)
..
RDARGS: SEP SUB                 ..NOTE SUB AT
                                ..READAH. READ
                                ..HEX ARGUMENTS
        XRI #4D                 ..SHOULD BE "M"
        BNZ ISITR               ..CK FOR ?R
RD1:    SEP SUB
        BNF *-#01               ..IGNORE NON HEX
                                ..CHARS. AFTER "M"
        SEP SUB
        BDF *-#01               ..READ FIRST ARG
                                ..(LOCA IN MEMORY)
        GHI ASL ;PHI PTER
        GLO ASL ;PLO PTER       ..PTER NOW POINTS
                                ..TO USER MEMORY
        LDI#00 ;PLO ASL ;PHI ASL..CLEAR ASL
        INC ASL                 ..?MXXXXCR PRINTS
                                ..TWO HEX DIGITS
        GHI RF   ;XRI#0D        ..CK FOR CR
        BNZ TEST                ..BR IF NOT A CR
        GLO SWITCH
        BNZ LINE-#03            ..BR IF ?
        BR SYNERR               ..OTHERWISE ERROR
TEST:   XRI#2D                  ..CK FOR SPACE
        BNZ SYNERR
        DEC ASL                 ..ADJUST ASL
        GLO SWITCH              ..LOOK AT SWITCH
        BZ EX1                  ..IF 0 IT IS "!"
                                ..OTHERWISE IT'S ?
..
..  THE FOLLOWING DOES (?M LOC COUNT) AND
..  (?MXXXXCR) COMMANDS
RD2:    SEP SUB
        BDF RD2                 ..READ SECOND ARG
                                ..(NUMBER OF BYTES)
        XRI #0D                 ..NEXT CK FOR CR
        BNZ SYNERR
        LDI A.0(TYPE5D) ;PLO SUB..TYPE
LINE:   SEP SUB; ,#0A           ..LF
        GHI PTER ;PHI CHAR      ..PREPARE LINE
                                ..HEADING
        LDI A.0(TYPE2) ; PLO SUB
        SEP SUB                 ..TYPE 2 HEX DIGITS
        GLO PTER ;PHI CHAR
        LDI A.0(TYPE2) ;PLO SUB
        SEP SUB                 ..TYPE OTHER TWO
TSPACE: SEP SUB; ,#20           ..SPACE
..
TLOOP:  LDA PTER ;PHI CHAR      ..FETCH ONE BYTE
                                ..FOR TYPING
        LDI A.0(TYPE2) ;PLO SUB
        SEP SUB                 ..TYPE 2 HEX
        DEC CNTER
        GLO CNTER
        BNZ TL3                 ..BRANCH NOT DONE
        GHI CNTER
        BZ START                ..BRANCH IF DONE
TL3:    GLO PTER ;ANI #0F       ..PTER DIV BY 16
        BNZ TL2
        SEP SUB; ,#3B           ..YES TYPE ";"
        SEP SUB; ,#0D           ..THEN CR
        BR LINE
TL2:    SHR                     ..DIV BY 2?
        BDF TLOOP               ..NO, LOOP BACK
        BR TSPACE               ..ELSE TYPE SPACE &
                                ..LOOP BACK
..
..  THE FOLLOWING DOES (!M LOC DATA) COMMAND
..  ENTER AT EX1
..
..  EFFECT OF THE FOLLOWING IS TO READ IN HEX
..  TERMINATING WITH A CR, IGNORING NON-HEX CHAR
..  PAIRS; EXCEPTIONS: A COMMA BEFORE A CR ALLOWS
..  THE INPUT TO CONTINUE ON THE NEXT LINE AND A
..  SEMICOLON ALLOWS THE !M COMMAND TO BE ASSUMED.
..
EX3:    SEP SUB                 ..INPUT UNTIL A
                                ..HEX IS READ
        BNF EX3
EX2:    SEP SUB                 ..LOOK FOR SECOND
                                ..HEX DIGIT
        BNF SYNERR              ..BR IF NOT HEC
        GLO ASL ;STR PTER       ..**SET BYTE**
        INC PTER
EX1:    SEP SUB                 ..NOTE SUB @ READAH
        BDF EX2                 ..BRANCH IF HEX
        XRI #0D                 ..CHECK IF CR
        BZ START
EX4:    XRI #21                 ..ELSE CK FOR COMMA
                                ..(TEST CR XRI ",")
        BZ EX3                  ..IF ELSE BRANCH
        XRI #17                 ..ELSE CK FOR ";"
                                ..(TEST CR XRI
                                .."," XRI ";")
        BNZ EX1                 ..IGNORE ALL ELSE
        SEP SUB                 ..ON ";" IGNORE ALL
                                ..UNTIL CR, THEN
                                ..LOOP BACK
        XRI #0D
        BNZ *-03
        BR RD1                  ..THEN BRANCH BACK
                                ..FOR !M COMMAND
ISITR:  XRI#1F                  ..IS IT R?
        LBZ TYPER               ..BR IF R
..
SYNERR: LDI A.0(TYPE5D);PLO SUB ..GENERAL RESULT
                                ..SYNTATIC ERROR
        SEP SUB; ,#0D           ..CR
        LBR FSYNER
..
..
..
..  SUBROUTINES
..
..
        ORG*+#01
..  DELAY ROUTINE
..  DELAY IS (2(1+AUX.1(3+@SUB))
..  USED BY TYPE, READ, AND TIMALC.
..  AUX.1 IS ASSUMED TO HOLD A DELAY CONSTANT
..  =((BIT TIME OF TERMINAL)/
..  (20*INSTR TIME OF COSMAC))-1.
..  THIS CONSTANT CAN BE GENERATED
..  AUTOMATICALLY BY THE TIMALC ROUTINE.
..
DEXIT:  SEP RC;SEP RC;SEP RC;SEP RC     ..4 NOP'S
        SEP SUB         ..RETURN
DELAY1: GHI AUX ;SHR ;PLO AUX   ..SHIFT OUT
                                ..ECHO FLAG
DELAY2: DEC AUX                 ..AUX.0 HOLDS BASIC
                                ..BIT DELAY
        LDA SUB ;SMI #01        ..PICK UP CONSTANT
        BNZ *-#02               ..LOOP AS SPECIFIED
                                ..BY CALL
        GLO AUX                 ..DONE YET?
        BZ DEXIT
        DEC SUB                 ..POINT SUB AT
                                ..DELAY POINTER
        BR DELAY2

DOLLAR:

TIMALC:

TYPE2:

TYPER:

TYPE5D:

READAH:

FSYNER:





















     
END

