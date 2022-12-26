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
CREATE OR REPLACE FUNCTION Get_HistoryFunc (P_Row IN NUMBER, P_UserID IN NUMBER) RETURN VARCHAR2
IS

  JSON_MATCHES_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON;
  v_TotalData NUMBER;
  v_TotalRows NUMBER;

BEGIN
J_RESULT := UTILS.JSON();
  FOR REC IN (SELECT p.id AS ID,
                     p.voter_id AS voterid,
                     p.score_team1 AS sT1,
                     t.team_name AS First_Team,
                     p.score_team2 AS sT2,
                     tt.team_name AS Second_Team,
                     p.prognosed_player AS prognosedP,
                     pp.player_name AS PlayerName,
                     ttt.team_name AS PlayerTeam,
                     to_char(p.prognosed_date, 'dd-MM-yyyy HH24:MI:SS') AS prognosedD,
                     tttt.team_name AS VotedTeam
                FROM prognose_history p
                          LEFT JOIN match m ON m.id = p.match_id
                          LEFT JOIN voter v ON v.id = p.Voter_id
                          LEFT JOIN players pp ON pp.id = p.prognosed_player
                          LEFT JOIN Teams t ON t.id = m.first_Team_id
                          LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id
                          LEFT JOIN Teams ttt ON ttt.id = pp.team_id
                          LEFT JOIN Teams tttt ON tttt.id = p.prognosed_team
                          WHERE v.chat_id = P_UserID
                          ORDER BY p.prognosed_date DESC OFFSET P_Row * 10 ROWS FETCH NEXT 10 ROWS ONLY)
  LOOP
    J_OBJECT := UTILS.JSON();

    J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
    IF REC.sT1 = 99 AND REC.sT2 = 0 THEN
          J_OBJECT.PUT('PrognosedType', '1'); -- first team selected
          J_OBJECT.PUT('PrognosedTeam', REC.First_Team);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales castigatorul echipa ' || REC.First_Team || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.st2 = 99 AND REC.St1 = 0 THEN
          J_OBJECT.PUT('PrognosedType', '2'); -- second team selected
          J_OBJECT.PUT('PrognosedTeam', REC.Second_Team);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales castigatorul echipa ' || REC.Second_Team || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.st1 = 50 AND REC.st2 = 50 THEN
          J_OBJECT.PUT('PrognosedType', '3'); -- equal selected
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales egalitate din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.sT1 <> 99 AND REC.sT2 <> 99 AND REC.sT1 <> 50 AND REC.sT2 <> 50 THEN
          J_OBJECT.PUT('PrognosedType', '4'); -- Total score selected
          J_OBJECT.PUT('FirstTeamScore', REC.ST1);
          J_OBJECT.PUT('SecondTeamScore', REC.st2);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales scorul: ' || REC.ST1 || '-' || REC.st2 || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.Prognosedp IS NOT NULL THEN
          J_OBJECT.PUT('PrognosedType', '5'); -- Player selected
          J_OBJECT.PUT('PlayerName', REC.Playername);
          J_OBJECT.PUT('PlayerTeam', REC.Playerteam);
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales jucatorul ' || REC.Playername || ' al echipei ' || REC.Playerteam || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.VotedTeam IS NOT NULL THEN
          J_OBJECT.PUT('PrognosedType', '6'); -- Team selected
          J_OBJECT.PUT('TeamName', REC.VotedTeam);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales jucatorul ' || REC.Playername || ' al echipei ' || REC.Playerteam || ' Din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);

    END IF;
    JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;

  SELECT COUNT(*) INTO v_TotalData FROM Prognose_History p INNER JOIN voter v ON v.id = p.voter_id WHERE v.chat_id = P_UserID;
  v_TotalRows := v_TotalData / 10;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_MATCHES_LIST);
 --JSON_MATCHES_LIST.PRINT;
RETURN J_RESULT.to_char(FALSE);
END;

/
CREATE OR REPLACE FUNCTION Get_MatchesFunc (P_Row IN NUMBER) RETURN VARCHAR2
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
                     tt.team_name  AS Second_Team,
                     to_char(m.start_time, 'dd-MON HH24:MI')  AS StartTime
                FROM match m
                LEFT JOIN Teams t ON t.id = m.first_Team_id
                LEFT JOIN Teams tt ON tt.id = m.Second_Team_Id
                WHERE m.first_team_id IS NOT NULL AND m.second_team_id IS NOT NULL
                ORDER BY m.start_time OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY)
  LOOP
    J_OBJECT := UTILS.JSON();
    J_OBJECT.PUT('MatchID', REC.ID);
    J_OBJECT.PUT('FirstTeam', REC.First_Team);
    J_OBJECT.PUT('SecondTeam', REC.Second_Team);
    J_OBJECT.PUT('StartDate', REC.StartTime);
    JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
  END LOOP;
  SELECT COUNT(*) INTO v_TotalData FROM match m WHERE m.start_time > SYSDATE AND m.first_team_id IS NOT NULL AND m.second_team_id IS NOT NULL;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_MATCHES_LIST);
 --JSON_MATCHES_LIST.PRINT;
RETURN J_RESULT.to_char(FALSE);
END;

/
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
                FROM Players p WHERE p.team_id = v_TeamID ORDER BY ID OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY)
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
                     to_char(p.prognosed_date, 'dd-MM-yyyy HH24:MI:SS') AS prognosedD

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
      IF REC.Firstteamscore > REC.SecondTeamScore THEN J_OBJECT.PUT('CorrectlyFteamPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyFteamPrognose', '0'); END IF;
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
      IF REC.Firstteamscore < REC.SecondTeamScore THEN J_OBJECT.PUT('CorrectlySteamPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlySteamPrognose', '0'); END IF;
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
      IF REC.Firstteamscore = REC.SecondTeamScore THEN J_OBJECT.PUT('CorrectlyEqualPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyEqualPrognose', '0'); END IF;
          J_OBJECT.PUT('PrognosedDate', REC.prognosedD);
          J_OBJECT.PUT('PrognosedType', '3'); -- equal selected
          J_OBJECT.PUT('MatchFTeam', REC.First_Team);
          J_OBJECT.PUT('MatchSTeam', REC.Second_Team);
          v_FirstTypeVote := 1;
          JSON_MATCHES_LIST.APPEND(J_OBJECT.to_json_value);
          --dbms_OUTPUT.PUT_LINE(REC.PROGNOSEDD || '  ati ales egalitate din meciul dintre: ' || REC.FIRST_TEAM || ' VS ' || REC.Second_Team);
    ELSIF REC.sT1 <> 99 AND REC.sT2 <> 99 AND REC.sT1 <> 50 AND REC.sT2 <> 50 AND v_SecondTypeVote = 0 THEN
      J_OBJECT := UTILS.JSON();
      IF REC.Firstteamscore = REC.ST1 AND REC.SecondTeamScore = REC.St2 THEN J_OBJECT.PUT('CorrectlyTotalPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyTotalPrognose', '0'); END IF;
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
      SELECT COUNT(*) INTO v_CNT FROM Player_Goals pg WHERE pg.player_id = REC.Playerid;
      IF v_CNT <> 0 THEN J_OBJECT.PUT('CorrectlyPlayerPrognose', '1'); ELSE J_OBJECT.PUT('CorrectlyPlayerPrognose', '0'); END IF;
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
CREATE OR REPLACE FUNCTION Get_Team_By_ID(TeamID IN NUMBER) RETURN VARCHAR2 IS
V_TeamName VARCHAR(30);
BEGIN



SELECT t.team_name AS TeamName INTO V_TeamName FROM teams t WHERE t.id = TeamID;
      RETURN V_TeamName;




END;

/
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
            SUM(
            CASE WHEN (P.score_team1 = 99 AND P.Score_Team2 = 0 AND m.first_team_score > m.second_team_score) OR
                      (P.score_team1 = 0 AND P.Score_Team2 = 99 AND m.first_team_score < m.second_team_score) OR
                      (P.score_team1 = 50 AND P.Score_Team2 = 50 AND m.first_team_score = m.second_team_score)
                 THEN 1 ELSE 0 END + --
            CASE WHEN P.score_team1 = m.first_team_score AND P.Score_Team2 = m.second_team_score THEN 3 ELSE 0 END +
            CASE WHEN G.match_id IS NOT NULL AND P.match_id = G.match_id THEN 1 ELSE 0 END +
            CASE WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('20/11/2022', 'dd/MM/yyyy')
                 THEN 50
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('03/12/2022', 'dd/MM/yyyy')
                 THEN 25
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('09/12/2022', 'dd/MM/yyyy')
                 THEN 10
                 ELSE 0 END) Points
      FROM      prognose     P
           JOIN voter        V ON P.voter_id = v.id
      LEFT JOIN match        M ON m.ID = P.match_id
      LEFT JOIN Player_Goals G ON P.Prognosed_Player = G.player_id
      WHERE P.voter_id = v.id OR P.match_id = m.id
      GROUP BY v.FIRST_NAME, v.LAST_NAME
      ORDER BY Points DESC 
      )A OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY
    )
    LOOP
      J_OBJECT := UTILS.JSON();
      v_TotalData := REC.TotalData;
    J_OBJECT.PUT('FirstName', TRIM(Rec.FIRST_NAME));
    J_OBJECT.PUT('SecondName', TRIM(Rec.Last_Name));
    J_OBJECT.PUT('Score', REC.Points);
    JSON_LIST.APPEND(J_OBJECT.to_json_value);
    V_TotalScore := 0;
  END LOOP;
  v_TotalRows := v_TotalData / 5;
  J_RESULT.Put('TotalRows', CEIL(v_TotalRows));
  J_RESULT.Put('Result', JSON_LIST);
RETURN J_RESULT.to_char(FALSE);
END;






/
CREATE OR REPLACE FUNCTION Get_VoterFromTop (P_Row IN NUMBER) RETURN VARCHAR2 IS
  JSON_LIST UTILS.JSON_LIST := UTILS.JSON_LIST();
  J_OBJECT UTILS.JSON;
  J_RESULT UTILS.JSON := UTILS.JSON();
BEGIN



 FOR REC IN (
SELECT ROWNUM AS RNUM, A.* FROM (
      SELECT DISTINCT
            v.FIRST_NAME,
            v.LAST_NAME,
            SUM(
            CASE WHEN (P.score_team1 = 99 AND P.Score_Team2 = 0 AND m.first_team_score > m.second_team_score) OR
                      (P.score_team1 = 0 AND P.Score_Team2 = 99 AND m.first_team_score < m.second_team_score) OR
                      (P.score_team1 = 50 AND P.Score_Team2 = 50 AND m.first_team_score = m.second_team_score)
                 THEN 1 ELSE 0 END + --
            CASE WHEN P.score_team1 = m.first_team_score AND P.Score_Team2 = m.second_team_score THEN 3 ELSE 0 END +
            CASE WHEN G.match_id IS NOT NULL AND P.match_id = G.match_id THEN 1 ELSE 0 END +
            CASE WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('20/11/2022', 'dd/MM/yyyy')
                 THEN 50
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('03/12/2022', 'dd/MM/yyyy')
                 THEN 25
                 WHEN P.PROGNOSED_TYPE = 6 AND P.PROGNOSED_TEAM IS NOT NULL AND M.id = 64 AND
                      (M.FIRST_TEAM_SCORE > M.SECOND_TEAM_SCORE AND M.FIRST_TEAM_ID = P.PROGNOSED_TEAM OR
                       M.FIRST_TEAM_SCORE < M.SECOND_TEAM_SCORE AND M.SECOND_TEAM_ID = P.PROGNOSED_TEAM) AND
                      P.PROGNOSED_DATE < To_Date('09/12/2022', 'dd/MM/yyyy')
                 THEN 10
                 ELSE 0 END) Points
      FROM      prognose     P
           JOIN voter        V ON P.voter_id = v.id
      LEFT JOIN match        M ON m.ID = P.match_id
      LEFT JOIN Player_Goals G ON P.Prognosed_Player = G.player_id
      WHERE P.voter_id = v.id OR P.match_id = m.id
      GROUP BY v.FIRST_NAME, v.LAST_NAME
      ORDER BY Points DESC
      )A OFFSET P_Row * 5 ROWS FETCH NEXT 5 ROWS ONLY
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
CREATE OR REPLACE PROCEDURE PrognoseFinalTeam(VoterChatID IN NUMBER, TeamID IN NUMBER) IS
BEGIN
UPDATE Voter v SET v.voted_team = TeamID, v.date_voted_team = SYSDATE WHERE v.chat_id = VoterChatID;
COMMIT;
END;

/
CREATE OR REPLACE PROCEDURE PrognoseVote(VoterChatID IN NUMBER,
MatchID IN NUMBER, Vote_Type IN NUMBER, Team_Score1 IN NUMBER, Team_Score2 IN NUMBER, Voted_Player IN NUMBER, Voted_Team IN NUMBER) IS
 VoterID NUMBER;
 CntType1 NUMBER;
 CntType2 NUMBER;
 CntType3 NUMBER;
 CntType4 NUMBER;
BEGIN
 SELECT v.ID INTO VoterID FROM Voter v WHERE v.chat_id = VoterChatID;
 SELECT COUNT(*) INTO CntType1 FROM Prognose p  WHERE p.voter_id = VoterID AND p.match_id = MatchID AND (p.prognosed_type = 1 OR p.prognosed_type = 2 OR p.prognosed_type = 3);
 SELECT COUNT(*) INTO CntType2 FROM Prognose p  WHERE p.voter_id = VoterID AND p.match_id = MatchID AND p.prognosed_type = 4;
 SELECT COUNT(*) INTO CntType3 FROM Prognose p  WHERE p.voter_id = VoterID AND p.match_id = MatchID AND p.prognosed_type = 5;
 SELECT COUNT(*) INTO CntType4 FROM Prognose p  WHERE p.voter_id = VoterID AND p.match_id = MatchID AND p.prognosed_type = 6;
IF Vote_Type = 1 THEN BEGIN -- First team win
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, 99, 0, NULL, NULL, SYSDATE, 1);

  IF (CntType1 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, 99, 0, NULL, NULL, SYSDATE, 1);
  ELSE
  UPDATE  Prognose p SET p.score_team1 = 99, p.score_team2 = 0, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.match_id = MatchID AND (p.prognosed_type = 1 OR p.prognosed_type = 2 OR p.prognosed_type = 3);

END IF;
--insert into prognose(id, voter_id, match_id, score_team1, score_team2, prognosed_team, prognosed_player, prognosed_date, prognosed_type)

END;
END IF;
IF Vote_Type = 2 THEN BEGIN -- Second team win
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, 0, 99, NULL, NULL, SYSDATE, 2);
     IF (CntType1 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, 0, 99, NULL, NULL, SYSDATE, 2);
  ELSE
  UPDATE  Prognose p SET p.score_team1 = 0, p.score_team2 = 99, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.match_id = MatchID AND (p.prognosed_type = 1 OR p.prognosed_type = 2 OR p.prognosed_type = 3);
END IF;
END;
END IF;
--insert into prognose(id, voter_id, match_id, score_team1, score_team2, prognosed_team, prognosed_player, prognosed_date, prognosed_type)

IF Vote_Type = 3 THEN BEGIN -- Equal
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, 50, 50, NULL, NULL, SYSDATE, 3);
    IF (CntType1 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, 50, 50, NULL, NULL, SYSDATE, 3);
  ELSE
  UPDATE  Prognose p SET p.score_team1 = 50, p.score_team2 = 50, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.match_id = MatchID AND (p.prognosed_type = 1 OR p.prognosed_type = 2 OR p.prognosed_type = 3);
END IF;
END;
END IF;
--insert into prognose(id, voter_id, match_id, score_team1, score_team2, prognosed_team, prognosed_player, prognosed_date, prognosed_type)

IF Vote_Type = 4 THEN BEGIN -- Total
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, Team_Score1, Team_Score2, NULL, NULL, SYSDATE, 4);

    IF (CntType2 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, Team_Score1, Team_Score2, NULL, NULL, SYSDATE, 4);
  ELSE
  UPDATE  Prognose p SET p.score_team1 = Team_Score1, p.score_team2 = Team_Score2, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.match_id = MatchID AND p.prognosed_type = Vote_Type;

END IF;
END;
END IF;
--insert into prognose(id, voter_id, match_id, score_team1, score_team2, prognosed_team, prognosed_player, prognosed_date, prognosed_type)

IF Vote_Type = 5 THEN BEGIN -- Voted Player
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, NULL, NULL, NULL, Voted_Player, SYSDATE, 5);

    IF (CntType3 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, NULL, NULL, NULL, Voted_Player, SYSDATE, 5);
  ELSE
  UPDATE  Prognose p SET p.prognosed_player = Voted_Player, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.match_id = MatchID AND p.prognosed_type = Vote_Type;

END IF;
END;
END IF;
--insert into prognose(id, voter_id, match_id, score_team1, score_team2, prognosed_team, prognosed_player, prognosed_date, prognosed_type)

IF Vote_Type = 6 THEN BEGIN -- Voted Team
  INSERT INTO prognose_history VALUES( DEFAULT, VoterID, MatchID, NULL, NULL, Voted_Team, NULL, SYSDATE, 6);

  IF (CntType4 = 0) THEN
  INSERT INTO prognose VALUES( DEFAULT, VoterID, MatchID, NULL, NULL, Voted_Team, NULL, SYSDATE, 6);
  ELSE
  UPDATE  Prognose p SET p.prognosed_team = Voted_Team, p.prognosed_date = SYSDATE, p.prognosed_type = Vote_Type WHERE p.voter_id = VoterID AND p.prognosed_type = Vote_Type;
  END IF;

END;
END IF;


COMMIT;
END;






/
CREATE OR REPLACE PROCEDURE SetLoggs(P_UserID IN NUMBER, P_UserMessage IN VARCHAR2, P_UserCallbackData IN VARCHAR2, P_ThrowedExceptions IN VARCHAR2)

IS

BEGIN
  IF P_UserID IS NOT NULL THEN
    BEGIN
      IF P_UserMessage IS NOT NULL THEN INSERT INTO Loggs VALUES (DEFAULT, P_UserID, P_UserMessage, NULL, NULL, 1, DEFAULT);
      ELSE INSERT INTO Loggs VALUES (DEFAULT, P_UserID, NULL, P_UserCallbackData, NULL, 2, DEFAULT);
   END IF;
    END;
    ELSE INSERT INTO Loggs VALUES (DEFAULT, NULL, NULL, NULL, P_ThrowedExceptions, 3, DEFAULT);
END if;
END;

/*

begin
  SetLoggs ( 89965666, null, 'TestCallbackData', null);
  SetLoggs ( null, null, null, 'TestError');
  SetLoggs ( 89965666, 'TestMessage', null, null);
end;

*/

/
CREATE OR REPLACE PROCEDURE Set_New_Voter(Voter_Name     IN VARCHAR2,
                                          Voter_Surname  IN VARCHAR2,
                                          Voter_Phone    IN VARCHAR2,
                                          Voter_Language IN VARCHAR2,
                                          ChatID         IN NUMBER) IS
  ifExist NUMBER;
BEGIN
  SELECT COUNT(*) INTO IfExist FROM Voter v WHERE v.chat_id = ChatID;

  IF IfExist >= 1 THEN
    IF (Voter_Name IS NOT NULL AND IfExist >= 1) THEN
      UPDATE Voter v
         SET v.First_Name = Voter_Name
       WHERE v.Chat_ID = ChatID;
    END IF;
    IF (Voter_Surname IS NOT NULL AND IfExist >= 1) THEN
      UPDATE Voter v
         SET v.Last_Name = Voter_Surname
       WHERE v.Chat_ID = ChatID;
    END IF;
    IF (Voter_Phone IS NOT NULL AND IfExist >= 1) THEN
      UPDATE Voter v SET v.phone = Voter_Phone WHERE v.Chat_ID = ChatID;
    END IF;
    IF (Voter_Language IS NOT NULL AND IfExist >= 1) THEN
      UPDATE Voter v
         SET v.language = Voter_Language
       WHERE v.Chat_ID = ChatID;
    END IF;
    --UPDATE Voter v SET v.First_Name = Voter_Name, v.Last_Name = Voter_Surname, v.phone = Voter_Phone WHERE v.Chat_ID = ChatID;
  ELSIF IfExist <= 1 THEN
    INSERT INTO Voter
    VALUES
      (DEFAULT,
       Voter_Name,
       Voter_Surname,
       Voter_Phone,
       ChatID,
       NULL,
       NULL,
       NULL);
  END IF;
 EXCEPTION 
   WHEN OTHERS THEN
     logs.write.ERROR(P_MESSAGE         => 'Voter_Name='||Voter_Name||',Voter_Name='||Voter_Name||',Voter_Phone='||Voter_Phone||',Voter_Language='||Voter_Language||',ChatID='||ChatID,
                      P_SYSTEM_NAME     => $$PLSQL_UNIT_OWNER,
                      P_PROCEDURE_NAME  => $$PLSQL_UNIT,
                      P_WRITE_EXCEPTION => TRUE);
END;

/
CREATE OR REPLACE PROCEDURE Set_Player_Goals(P_MatchID   IN NUMBER,
                                                 P_PlayerID IN NUMBER) IS



BEGIN

INSERT INTO player_Goals VALUES (DEFAULT, P_PlayerID, P_MatchID, 1);



END;

/
CREATE OR REPLACE PROCEDURE Update_Matches(P_MatchID   IN NUMBER,
                                               P_FirstTeamID IN NUMBER,
                                               P_SecondTeamID IN NUMBER,
                                               P_FirstTeamScore IN NUMBER,
                                               P_SecondTeamScore IN NUMBER) IS

BEGIN

IF P_FirstTeamID IS NOT NULL THEN
  UPDATE Match m SET m.first_team_id = P_FirstTeamID WHERE m.id = P_MatchID; END IF;
IF P_SecondTeamID IS NOT NULL THEN
  UPDATE Match m SET m.second_team_id = P_SecondTeamID WHERE m.id = P_MatchID; END IF;
IF P_FirstTeamScore IS NOT NULL THEN
  UPDATE Match m SET m.first_team_score = P_FirstTeamScore WHERE m.id = P_MatchID; END IF;
IF P_SecondTeamScore IS NOT NULL THEN
  UPDATE Match m SET m.second_team_score = P_SecondTeamScore WHERE m.id = P_MatchID; END IF;

 DELETE FROM Player_Goals pg WHERE pg.match_id = P_MatchID;
END;

/
CREATE OR REPLACE PROCEDURE Update_Teams(P_ID   IN NUMBER,
                                          P_NAME IN VARCHAR2) IS



BEGIN
UPDATE Teams t SET t.team_name = P_NAME WHERE t.id = P_ID;
END;

/
