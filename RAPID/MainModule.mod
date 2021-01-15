MODULE MainModule

    PROC main()
		init_comm;
		WHILE TRUE DO
			TestServConn;
			WaitTime 0.1;
		ENDWHILE
    ENDPROC
ENDMODULE