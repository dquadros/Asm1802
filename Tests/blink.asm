.. BLINK -- Blink Q slow
.. from The 1802 Membership Card Manual

START:
        LDI 73
        PHI R2      .. R2 will be our delay counter
LOOP:
        DEC R2
        GHI R2
        BNZ LOOP    .. count from #43xx to #00FF
        LSQ         .. if Q is OFF
        SEQ         ..   turn Q ON
        SKP         .. else
        REQ         ..   turn Q OFF
        BR  START   .. repeat forever
        
        END
