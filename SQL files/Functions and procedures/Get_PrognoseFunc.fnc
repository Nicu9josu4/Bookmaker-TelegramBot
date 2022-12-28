CREATE OR REPLACE FUNCTION Get_PrognoseFunc (P_UserID IN NUMBER, P_MatchID IN NUMBER) RETURN VARCHAR2
IS

  JSON_MATCHES_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  JSON_Team_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  JSON_Total_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;
  v_FirstTypeVote NUMBER := 0; -- first, second team selected or equal
  v_SecondTypeVote NUMBER := 0; -- total writen
  v_ThirdTypeVote NUMBER := 0; -- selected player
  v_CNT NUMBER := 0;
  v_StartDate VARCHAR2(30) := '';

BEGIN
J_RESULT := UTILS.JSON();
JSON_Total_LIST := UTILS.JSON_LIST();
    FOR REC IN (SELECT          m.id           AS ID,
                m.first_team_score  AS sT1,
                t.team_name    AS First_Team,
                m.second_team_score  AS sT2,
                tt.team_name   AS Second_Team,
                to_char(m.start_time, 'MM/dd/yyyy HH24:MI:SS') AS StartTime
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

    JSON_Total_LIST.APPEND(J_OBJECT.to_json_value);
    v_StartDate := Rec.StartTime;
  END LOOP;
    J_RESULT.Put('TotalScore', JSON_Total_LIST);


  JSON_Team_LIST := UTILS.JSON_LIST();
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

    JSON_Team_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;
    J_RESULT.Put('Players', JSON_Team_LIST);

  FOR REC IN (SELECT p.id AS ID,
                     p.voter_id AS voterid,
                     p.score_team1 AS sT1,
                     t.team_name AS First_Team,
                     p.score_team2 AS sT2,
                     tt.team_name AS Second_Team,
                     p.prognosed_player AS prognosedP,
                     pp.player_name AS PlayerName,
                     ttt.team_name AS PlayerTeam,
                     m.first_team_score AS FirstTeamScore,
                     m.second_team_score AS SecondTeamScore,
                     pp.id               AS PlayerID,
										 m.START_TIME        AS StartMatch,
                     to_char(p.prognosed_date, 'dd-MM-yyyy HH24:MI:SS') AS prognosedD,
										 p.PROGNOSED_DATE AS prognozedDate

                FROM prognose p
                          LEFT JOIN match m ON m.id = p.match_id
                          LEFT JOIN voter v ON v.id = p.Voter_id
                          LEFT JOIN players pp ON pp.id = p.prognosed_player
                          LEFT JOIN Teams t ON t.id = m.first_Team_id
                          LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id
                          LEFT JOIN Teams ttt ON ttt.id = pp.team_id
                          WHERE v.chat_id = P_UserID AND p.match_id = P_MatchID
                          ORDER BY p.prognosed_date DESC)
  LOOP
    --J_OBJECT := UTILS.JSON();
    IF REC.sT1 = 99 AND REC.sT2 = 0 AND v_FirstTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      IF REC.Firstteamscore > REC.SecondTeamScore AND REC.prognozedDate < REC.STARTMATCH THEN J_OBJECT.PUT('CorrectlyFteamPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyFteamPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '1'); -- first team selected
          J_OBJECT.PUT('PrognosedTeam', REC.First_Team);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_FirstTypeVote := 1;
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales castigatorul echipa ' || REC.First_Team || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.st2 = 99 AND REC.St1 = 0 AND v_FirstTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      IF REC.Firstteamscore < REC.SecondTeamScore AND REC.prognozedDate < REC.STARTMATCH THEN J_OBJECT.PUT('CorrectlySteamPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlySteamPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '2'); -- second team selected
          J_OBJECT.PUT('PrognosedTeam', REC.Second_Team);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_FirstTypeVote := 1;
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales castigatorul echipa ' || REC.Second_Team || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.st1 = 50 AND REC.st2 = 50 AND v_FirstTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      IF REC.Firstteamscore = REC.SecondTeamScore AND REC.prognozedDate < REC.STARTMATCH THEN J_OBJECT.PUT('CorrectlyEqualPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyEqualPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '3'); -- equal selected
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_FirstTypeVote := 1;
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales egalitate din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.sT1 <> 99 AND REC.sT2 <> 99 AND REC.sT1 <> 50 AND REC.sT2 <> 50 AND v_SecondTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      IF REC.Firstteamscore = REC.ST1 AND REC.SecondTeamScore = REC.St2 AND REC.prognozedDate < REC.STARTMATCH  THEN J_OBJECT.PUT('CorrectlyTotalPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyTotalPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '4'); -- Total score selected
          J_OBJECT.PUT('FirstTeamScore', REC.ST1);
          J_OBJECT.PUT('SecondTeamScore', REC.st2);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_SecondTypeVote := 1;
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales scorul: ' || REC.ST1 || '-' || REC.st2 || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.Prognosedp IS NOT NULL AND v_ThirdTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      SELECT COUNT(*) INTO v_CNT FROM Player_Goals pg WHERE pg.player_id = REC.Playerid AND pg.MATCH_ID = P_MatchID;
      IF v_CNT <> 0 AND REC.prognozedDate < REC.STARTMATCH THEN J_OBJECT.PUT('CorrectlyPlayerPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyPlayerPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '5'); -- Player selected
          J_OBJECT.PUT('PlayerName', REC.Playername);
          J_OBJECT.PUT('PlayerTeam', REC.Playerteam);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_ThirdTypeVote := 1;
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales jucatorul ' || REC.Playername || ' al echipei ' || REC.Playerteam || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
    END IF;
    --JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);

  END LOOP;
  J_RESULT.PUT('MatchStart', v_StartDate);
  J_RESULT.Put('Result', JSON_MATCHES_LIST);



 --JSON_MATCHES_LIST.PRINT;
RETURN J_RESULT.to_char(FALSE);
END;
/
