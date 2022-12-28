CREATE OR REPLACE FUNCTION Get_MenuMatchesFunc (P_Match_ID IN NUMBER)
RETURN VARCHAR2
IS
  JSON_Teams UTILS.JSON_LIST := UTILS.JSON_LIST();
  --JSON_TEAMS_CLOB CLOB;
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;

BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (select m.id, t.team_name AS First_Team, tt.team_name AS Second_Team, to_char(m.start_time, 'MM/dd/yyyy HH24:MI:SS') AS StartTime from match m
LEFT JOIN Teams t ON t.id = m.first_Team_id
LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id
WHERE m.id = P_Match_ID)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('TeamID', REC.ID);
    J_OBJECT.PUT('StartTime', REC.StartTime);
    J_OBJECT.PUT('FirstTeamName', REC.First_Team);
    J_OBJECT.PUT('SecondTeamName', REC.Second_Team);
    JSON_Teams.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  --DBMS_LOB.CREATETEMPORARY(JSON_TEAMS_CLOB, FALSE, DBMS_LOB.CALL);
  --JSON_TEAMS.to_clob(JSON_TEAMS_CLOB);
  J_RESULT.Put('Result', JSON_Teams);
--JSON_Teams.PRINT;
RETURN J_RESULT.TO_CHAR(FALSE);
END;
/
