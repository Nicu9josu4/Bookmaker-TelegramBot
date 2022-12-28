CREATE OR REPLACE FUNCTION Get_VotersFunc
RETURN VARCHAR2
IS
  JSON_Teams UTILS.JSON_LIST := UTILS.JSON_LIST();
  --JSON_TEAMS_CLOB CLOB;
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;

BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (SELECT v.Id   AS ID,
                     v.chat_id   AS Chat_ID,
                     v.first_name AS First_Name,
                     v.last_name AS Last_Name,
                     v.Language AS LANGUAGE,
                     t.Team_Name AS TeamName
                FROM Voter v
                LEFT JOIN Teams t ON t.id = v.voted_team)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('UserID', REC.ID);
    J_OBJECT.PUT('ChatID', REC.Chat_ID);
    J_OBJECT.PUT('FirstName', TRIM(REC.First_Name));
    J_OBJECT.PUT('LastName', TRIM(REC.Last_Name));
    J_OBJECT.PUT('Language', REC.Language);
    J_OBJECT.PUT('TeamName', REC.TeamName);
    JSON_Teams.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  --DBMS_LOB.CREATETEMPORARY(JSON_TEAMS_CLOB, FALSE, DBMS_LOB.CALL);
  --JSON_TEAMS.to_clob(JSON_TEAMS_CLOB);
  J_RESULT.Put('Result', JSON_Teams);
--JSON_Teams.PRINT;
RETURN J_RESULT.TO_CHAR(FALSE);
END;
/
