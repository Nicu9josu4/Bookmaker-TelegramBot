CREATE OR REPLACE PROCEDURE PrognoseFinalTeam(VoterChatID IN NUMBER, TeamID IN NUMBER) IS
BEGIN
UPDATE Voter v SET v.voted_team = TeamID, v.date_voted_team = SYSDATE WHERE v.chat_id = VoterChatID;
COMMIT;
END;
/
