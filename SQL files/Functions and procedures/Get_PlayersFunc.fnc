CREATE OR REPLACE FUNCTION Get_PlayersFunc (TeamName IN VARCHAR2, P_Row IN NUMBER)
RETURN VARCHAR2
IS
  JSON_Players_List UTILS.JSON_LIST := UTILS.JSON_LIST();
  --JSON_TEAMS_CLOB CLOB;
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;
  v_TeamID NUMBER;
  v_TotalData NUMBER;
  v_TotalRows NUMBER;

BEGIN
J_RESULT := UTILS.JSON();
SELECT t.ID INTO v_TeamID FROM teams t WHERE t.Team_Name = TeamName;

  FOR REC IN (SELECT p.id AS ID,
                          p.team_id   AS TeamID,
                     p.player_name   AS PlayerName
                FROM Players p WHERE p.team_id = v_TeamID ORDER BY PlayerName OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('PlayerID', REC.ID);
    J_OBJECT.PUT('TeamID', REC.TeamID);
    J_OBJECT.PUT('PlayerName', REC.PlayerName);
    JSON_Players_List.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  --DBMS_LOB.CREATETEMPORARY(JSON_TEAMS_CLOB, FALSE, DBMS_LOB.CALL);
  --JSON_TEAMS.to_clob(JSON_TEAMS_CLOB);
  SELECT COUNT(*) INTO v_TotalData FROM Players p WHERE p.team_id = v_TeamID;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_Players_List);
--JSON_Teams.PRINT;
RETURN J_RESULT.TO_CHAR(FALSE);
END;
/
