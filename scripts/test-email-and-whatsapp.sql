-- Create a manual alert and expected recipients for testing
DECLARE @now DATETIME2 = SYSUTCDATETIME();

INSERT INTO Alerte (AlertTypeId, AppId, ExpedTypeId, ExpediteurId, TitreAlerte, DescriptionAlerte, DateCreationAlerte, StatutId, EtatAlerteId, PlateformeEnvoieId, DestinataireId)
VALUES (1, NULL, 1, NULL, 'Test envoi', 'Message de test', @now, 1, 1, NULL, NULL);

DECLARE @AlerteId INT = SCOPE_IDENTITY();

-- Email recipient
INSERT INTO HistoriqueAlerte (AlerteId, DestinataireEmail, EtatAlerte)
VALUES (@AlerteId, 'khalil.ouerghemmi@gmail.com', 'Non Lu');

-- WhatsApp recipient
INSERT INTO HistoriqueAlerte (AlerteId, DestinatairePhoneNumber, EtatAlerte)
VALUES (@AlerteId, '21699414008', 'Non Lu');

SELECT @AlerteId AS AlerteId;

