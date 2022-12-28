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
