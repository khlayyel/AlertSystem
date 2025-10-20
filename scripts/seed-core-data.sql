-- Seed minimal reference data for AlertSystem
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Statut)
BEGIN
    INSERT INTO Statut (StatutId, Statut) VALUES (1,'En cours'),(2,'Envoyé'),(3,'Annulé'),(4,'Échoué');
END

IF NOT EXISTS (SELECT 1 FROM Etat)
BEGIN
    INSERT INTO Etat (EtatAlerteId, EtatAlerte) VALUES (1,'Non Lu'),(2,'Lu');
END

IF NOT EXISTS (SELECT 1 FROM AlertType)
BEGIN
    INSERT INTO AlertType (AlertTypeId, AlertType) VALUES (1,'acquittementNécessaire'),(2,'information');
END

IF NOT EXISTS (SELECT 1 FROM PlateformeEnvoie)
BEGIN
    INSERT INTO PlateformeEnvoie (PlateformeId, Plateforme) VALUES (1,'Email'),(2,'WhatsApp'),(3,'Desktop');
END


