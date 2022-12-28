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
