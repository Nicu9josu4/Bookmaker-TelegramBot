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
