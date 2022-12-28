CREATE OR REPLACE FUNCTION Get_ADDMatchesFunc (P_Row IN NUMBER) RETURN VARCHAR2
IS



 JSON_MATCHES_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;
  v_TotalData NUMBER;
  v_TotalRows NUMBER;



BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (SELECT m.id          AS ID,
                     t.team_name   AS First_Team,
                     m.first_team_id AS FTeamID,
                     tt.team_name  AS Second_Team,
                     m.second_team_id AS STeamID,
                     to_char(m.start_time, 'dd-MON HH24:MI')  AS StartTime
                FROM match m
                LEFT JOIN Teams t ON t.id = m.first_Team_id
                LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id/*
                WHERE m.start_time > SYSDATE AND m.first_team_id IS NOT NULL AND m.second_team_id IS NOT NULL*/
                ORDER BY m.start_time OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('MatchID', REC.ID);
    J_OBJECT.PUT('FirstTeam', REC.First_Team);
    J_OBJECT.PUT('FTeamID', REC.FTeamID);
    J_OBJECT.PUT('SecondTeam', REC.Second_Team);
    J_OBJECT.PUT('STeamID', REC.STeamID);
    J_OBJECT.PUT('StartDate', REC.StartTime);
    JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  SELECT COUNT(*) INTO v_TotalData FROM match;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_MATCHES_LIST);
--JSON_MATCHES_LIST.PRINT;
RETURN J_RESULT.to_char(FALSE);
END;
/
