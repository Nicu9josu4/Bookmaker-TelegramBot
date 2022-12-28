CREATE OR REPLACE PROCEDURE Update_Teams(P_ID   IN NUMBER,
                                          P_NAME IN VARCHAR2) IS



BEGIN
UPDATE Teams t SET t.team_name = P_NAME WHERE t.id = P_ID;
END;
/
