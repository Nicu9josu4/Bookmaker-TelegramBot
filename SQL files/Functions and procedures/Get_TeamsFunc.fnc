CREATE OR REPLACE FUNCTION Get_TeamsFunc (P_Row IN NUMBER)
RETURN VARCHAR2
IS
  JSON_Teams UTILS.JSON_LIST := UTILS.JSON_LIST();
  --JSON_TEAMS_CLOB CLOB;
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;
  v_TotalData NUMBER;
  v_TotalRows NUMBER;

BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (SELECT t.Id   AS ID,
                     t.team_name   AS Team
                FROM Teams t ORDER BY ID OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('TeamID', REC.ID);
    J_OBJECT.PUT('TeamName', REC.Team);
    JSON_Teams.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  --DBMS_LOB.CREATETEMPORARY(JSON_TEAMS_CLOB, FALSE, DBMS_LOB.CALL);
  --JSON_TEAMS.to_clob(JSON_TEAMS_CLOB);
  SELECT COUNT(*) INTO v_TotalData FROM TEAMS;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_Teams);
--JSON_Teams.PRINT;
RETURN J_RESULT.TO_CHAR(FALSE);
END;
/
