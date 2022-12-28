CREATE OR REPLACE FUNCTION Get_PrognoseInfoFunc (P_MatchID IN NUMBER) RETURN VARCHAR2
IS

  JSON_MATCHES_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;

BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (SELECT          m.id           AS ID,
                m.first_team_score  AS sT1,
                t.team_name    AS First_Team,
                m.second_team_score  AS sT2,
                tt.team_name   AS Second_Team
                          FROM match m
                          LEFT JOIN Teams t ON t.id = m.first_Team_id
                          LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id
                          WHERE m.id = P_MatchID)
  LOOP

      J_OBJECT := UTILS.JSON();
          J_OBJECT.PUT('FirstTeamName', REC.First_Team);
          J_OBJECT.PUT('FirstTeamScore', REC.sT1);
          J_OBJECT.PUT('SecondTeamName', REC.Second_Team);
          J_OBJECT.PUT('SecondTeamScore', REC.sT2);

          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales castigatorul echipa ' || REC.First_Team || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);

    JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;
    J_RESULT.Put('Result', JSON_MATCHES_LIST);
  JSON_MATCHES_LIST := UTILS.JSON_LIST();
    FOR REC IN (SELECT
                pg.id           AS ID,
                p.player_name   AS PlayerName,
                t.team_name     AS TeamName,
                t.id            AS TeamID
                          FROM Player_Goals pg
                          LEFT JOIN Players p ON p.id = pg.player_id
                          LEFT JOIN Teams t ON t.id = p.team_id
                          WHERE pg.match_id = P_MatchID
                          ORDER BY TeamID)
  LOOP

      J_OBJECT := UTILS.JSON();
          J_OBJECT.PUT('PlayerName', REC.PlayerName);
          J_OBJECT.PUT('TeamName', REC.TeamName);

    JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;

  J_RESULT.Put('Players', JSON_MATCHES_LIST);
 --JSON_MATCHES_LIST.PRINT;
RETURN J_RESULT.to_char(FALSE);
END;
/
