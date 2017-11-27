.. Teste de RAM
.. Daniel Quadros, nov/17
.. Obs.: o programa pode ser destruido se
..       a memoria estiver com problema...


FRAM = #07FF        .. fim da RAM a testar

        ORG     #0
    
INICIO:
        SEX     1           .. R1 sera o índice
        
        .. preenche com 55
        GLO     R3
        LDI     A.0(FRAM)
        PLO     R1
        LDI     A.0(FRAM)
        PHI     R1          .. Preencher a partir do fim
PREENCHE:
        LDI     #55
        STXD
        GLO     R1
        XRI     A.0(BRAM-1)
        BNZ     PREENCHE
        GHI     R1
        XRI     A.1(BRAM-1)
        BNZ     PREENCHE
        
        LDI     #55
        PLO     R2
        LDI     #AA
        PHI     R2

        .. confere com valor anterior e escreve o novo
CONF1:
        GLO     R2
        PLO     R3
        GHI     R2
        PLO     R2
        GLO     R3
        PHI     R2          .. troca os valores     
        LDI     A.0(BRAM)
        PLO     R1
        LDI     A.1(BRAM)
        PHI     R1          .. partir do inicio
CONF2:
        GHI     R2
        XOR                 .. compara
        BNZ     ERRO
        GLO     R2
        STR     R1          .. coloca o outro valor
        OUT     4           .. mostra nos LEDs o valor escrito e avanca R1
        GLO     R1
        XRI     A.0(FRAM+1)
        BNZ     CONF2
        GHI     R1
        XRI     A.1(FRAM+1)
        BNZ     CONF2
        BR      CONF1       .. fim: inverte e repete
ERRO:
        OUT     4           .. mostra nos LEDs o valor errado
        SEQ                 .. acende o LED
        BR      *           .. para por aqui
        
BRAM:                       .. inicio da RAM a testar

        END
