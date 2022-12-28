CREATE OR REPLACE FUNCTION Get_VoterFromTop (P_USERID IN NUMBER) RETURN VARCHAR2 IS
  JSON_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON := UTILS.JSON();
BEGIN



 FOR REC IN (SELECT R.* FROM (
    SELECT ROWNUM AS RNUM, A.* FROM (
      SELECT DISTINCT
            V.Chat_ID,
            v.FIRST_NAME,
            v.LAST_NAME,
						MAX(p.PROGNOSED_DATE) AS LastPrognosedDate,
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
                 ELSE 0 END) Points
      FROM      prognose     P
           JOIN voter        V ON P.voter_id = v.id
      LEFT JOIN match        M ON m.ID = P.match_id
      LEFT JOIN Player_Goals G ON P.Prognosed_Player = G.player_id
      WHERE P.voter_id = v.id OR P.match_id = m.id
      GROUP BY v.FIRST_NAME, v.LAST_NAME, v.chat_id
      ORDER BY Points DESC, LastPrognosedDate  
    )A 
  )R  WHERE Chat_ID = P_USERID
    )
    LOOP
      J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('Place', TRIM(Rec.RNUM));
    J_OBJECT.PUT('FirstName', TRIM(Rec.FIRST_NAME));
    J_OBJECT.PUT('SecondName', TRIM(Rec.Last_Name));
    J_OBJECT.PUT('Score', REC.Points);
    JSON_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  J_RESULT.Put('Result', JSON_LIST);
RETURN J_RESULT.to_char(FALSE);
END;
/
