USE Brugtbilshandelsportal;
GO

/*
    Sikrer, at et svar peger på en aktiv hovedkommentar
    under samme annonce.

    MVP tillader kun ét svarniveau.
*/
CREATE OR ALTER TRIGGER dbo.TR_Comments_ValidateParent
    ON dbo.Comments
    AFTER INSERT, UPDATE
    AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
        (
            SELECT 1
            FROM inserted AS insertedComment
                     INNER JOIN dbo.Comments AS parentComment
                                ON parentComment.Id =
                                   insertedComment.ParentCommentId
            WHERE insertedComment.ParentCommentId IS NOT NULL
              AND
                (
                    parentComment.AnnouncementId
                        <> insertedComment.AnnouncementId

                        OR parentComment.ParentCommentId IS NOT NULL

                        OR parentComment.IsDeleted = 1
                    )
        )
        BEGIN
            THROW 50001,
                N'Et svar skal pege på en aktiv hovedkommentar under samme annonce.',
                1;
        END;
END;
GO