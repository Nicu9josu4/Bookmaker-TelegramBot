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
