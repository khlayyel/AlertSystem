-- CrÃ©er une clÃ© API de test pour les tests d'envoi automatique

-- 1. Supprimer l'ancienne clÃ© de test si elle existe
DELETE FROM ApiClients WHERE Name = 'Test Auto Send';

-- 2. InsÃ©rer une nouvelle clÃ© API de test
-- La clÃ© en clair sera: test-auto-send-key-123
-- Le hash BCrypt de cette clÃ© est calculÃ© avec un salt de 12 rounds
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

-- 3. VÃ©rifier que la clÃ© a Ã©tÃ© crÃ©Ã©e
SELECT 
    ApiClientId,
    Name,
    LEFT(ApiKeyHash, 20) + '...' as ApiKeyHash_Preview,
    IsActive,
    CreatedAt,
    RateLimitPerMinute
FROM ApiClients
WHERE Name = 'Test Auto Send';

PRINT 'ClÃ© API de test crÃ©Ã©e avec succÃ¨s !';
PRINT 'Nom du client: Test Auto Send';
PRINT 'ClÃ© API Ã  utiliser: test-auto-send-key-123';
PRINT 'Header Ã  ajouter: X-Api-Key: test-auto-send-key-123';

