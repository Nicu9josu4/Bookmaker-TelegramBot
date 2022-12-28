CREATE OR REPLACE FUNCTION Get_TopVotersFunc (P_Row IN NUMBER) RETURN VARCHAR2 IS
  JSON_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON := UTILS.JSON();
  v_TotalData NUMBER := 1;
  v_TotalRows NUMBER := 0;
  V_TotalScore NUMBER := 0;
BEGIN



 FOR REC IN (
SELECT ROWNUM AS RNUM, A.* FROM (
      SELECT DISTINCT
            COUNT(*) OVER() AS TotalData,
            v.FIRST_NAME,
            v.LAST_NAME,
						MAX(p.PROGNOSED_DATE) AS LastPrognosedDate,
            SUM(
            CASE WHEN (P.score_team1 = 99 AND P.Score_Team2 = 0 AND m.first_team_score > m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME) OR
                      (P.score_team1 = 0 AND P.Score_Team2 = 99 AND m.first_team_score < m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME) OR
                      (P.score_team1 = 50 AND P.Score_Team2 = 50 AND m.first_team_score = m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME)
                 THEN 1 ELSE 0 END + --
            CASE WHEN P.score_team1 = m.first_team_score AND P.Score_Team2 = m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME THEN 3 ELSE 0 END +
            CASE WHEN G.match_id IS NOT NULL AND P.match_id = G.match_id AND p.PROGNOSED_DATE < m.START_TIME THEN 1 ELSE 0 END) Points,
						SUM(
            CASE WHEN (P.score_team1 = 99 AND P.Score_Team2 = 0 AND m.first_team_score > m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME) OR
                      (P.score_team1 = 0 AND P.Score_Team2 = 99 AND m.first_team_score < m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME) OR
                      (P.score_team1 = 50 AND P.Score_Team2 = 50 AND m.first_team_score = m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME)
                 THEN 1 ELSE 0 END + --
            CASE WHEN P.score_team1 = m.first_team_score AND P.Score_Team2 = m.second_team_score AND p.PROGNOSED_DATE < m.START_TIME THEN 3 ELSE 0 END +
            CASE WHEN G.match_id IS NOT NULL AND P.match_id = G.match_id AND p.PROGNOSED_DATE < m.START_TIME THEN 1 ELSE 0 END +
            CASE WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('21/11/2022', 'dd/MM/yyyy')
                 THEN 50
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('04/12/2022', 'dd/MM/yyyy')
                 THEN 20
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('10/12/2022', 'dd/MM/yyyy')
                 THEN 10
                 ELSE 0 END) Points_After,
									 SUM(
            CASE WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('21/11/2022', 'dd/MM/yyyy')
                 THEN 50
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('04/12/2022', 'dd/MM/yyyy')
                 THEN 20
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND p.PROGNOSED_TEAM = 9 AND
                      P.PROGNOSED_DATE < To_Date('10/12/2022', 'dd/MM/yyyy')
                 THEN 10
                 ELSE 0 END) Points_ForFinalTeam
      FROM      prognose     P
           JOIN voter        V ON P.voter_id = v.id
      LEFT JOIN match        M ON m.ID = P.match_id
      LEFT JOIN Player_Goals G ON P.Prognosed_Player = G.player_id
      WHERE P.voter_id = v.id OR P.match_id = m.id
      GROUP BY v.FIRST_NAME, v.LAST_NAME
      ORDER BY Points_After DESC, LastPrognosedDate  
      )A  ORDER BY RNUM OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY
    )
    LOOP
      J_OBJECT := UTILS.JSON();
      v_TotalData := REC.TotalData;
    J_OBJECT.PUT('FirstName', TRIM(Rec.FIRST_NAME));
    J_OBJECT.PUT('SecondName', TRIM(Rec.Last_Name));
    J_OBJECT.PUT('ScoreBefore', REC.Points);
    J_OBJECT.PUT('ScoreAfter', REC.Points_After);
    J_OBJECT.PUT('FinalTeamPoints', REC.Points_ForFinalTeam);
    JSON_LIST.APPEND(J_OBJECT.to_json_value);
    V_TotalScore := 0;
  END LOOP;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_LIST);
RETURN J_RESULT.to_char(FALSE);
END;
/
