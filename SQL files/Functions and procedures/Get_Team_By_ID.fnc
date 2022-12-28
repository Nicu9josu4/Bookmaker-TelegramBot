CREATE OR REPLACE FUNCTION Get_Team_By_ID(TeamID IN NUMBER) RETURN VARCHAR2 IS
V_TeamName VARCHAR(30);
BEGIN



SELECT t.team_name AS TeamName INTO V_TeamName FROM teams t WHERE t.id = TeamID;
      RETURN V_TeamName;




END;
/
