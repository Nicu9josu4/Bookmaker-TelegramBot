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
