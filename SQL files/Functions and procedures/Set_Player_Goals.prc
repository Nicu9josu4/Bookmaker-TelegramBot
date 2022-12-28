CREATE OR REPLACE PROCEDURE Set_Player_Goals(P_MatchID   IN NUMBER,
                                                 P_PlayerID IN NUMBER) IS



BEGIN

INSERT INTO player_Goals VALUES (DEFAULT, P_PlayerID, P_MatchID, 1);



END;
/
