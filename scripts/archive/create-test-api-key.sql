-- Créer une clé API de test pour les tests d'envoi automatique

-- 1. Supprimer l'ancienne clé de test si elle existe
DELETE FROM ApiClients WHERE Name = 'Test Auto Send';

-- 2. Insérer une nouvelle clé API de test
-- La clé en clair sera: test-auto-send-key-123
-- Le hash BCrypt de cette clé est calculé avec un salt de 12 rounds
INSERT INTO ApiClients (
    Name,
    ApiKeyHash,
    IsActive,
    CreatedAt,
    RateLimitPerMinute
) VALUES (
    'Test Auto Send',
    '$2a$12$LQv3c1yqBwLVFjjg1P5wcu4E.rp.sRVAipmTvxwoIC2trbQ9gGRgG', -- Hash de 'test-auto-send-key-123'
    1,
    GETDATE(),
    1000
);

-- 3. Vérifier que la clé a été créée
SELECT 
    ApiClientId,
    Name,
    LEFT(ApiKeyHash, 20) + '...' as ApiKeyHash_Preview,
    IsActive,
    CreatedAt,
    RateLimitPerMinute
FROM ApiClients
WHERE Name = 'Test Auto Send';

PRINT 'Clé API de test créée avec succès !';
PRINT 'Nom du client: Test Auto Send';
PRINT 'Clé API à utiliser: test-auto-send-key-123';
PRINT 'Header à ajouter: X-Api-Key: test-auto-send-key-123';
