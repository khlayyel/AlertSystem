// Web Push Manager pour AlertSystem
class WebPushManager {
    constructor() {
        this.isSupported = 'serviceWorker' in navigator && 'PushManager' in window;
        this.registration = null;
        this.subscription = null;
        this.vapidPublicKey = null;
        this.currentUserId = null;
        
        console.log('[WebPush] Manager initialized, supported:', this.isSupported);
    }

    // Initialiser le Web Push Manager
    async init(userId) {
        if (!this.isSupported) {
            console.warn('[WebPush] Web Push not supported in this browser');
            return false;
        }

        this.currentUserId = userId;

        try {
            // Enregistrer le Service Worker
            this.registration = await navigator.serviceWorker.register('/sw.js');
            console.log('[WebPush] Service Worker registered:', this.registration);

            // Attendre que le SW soit prêt
            await navigator.serviceWorker.ready;

            // Obtenir la clé publique VAPID
            await this.getVapidPublicKey();

            // Vérifier si déjà abonné
            await this.checkExistingSubscription();

            return true;
        } catch (error) {
            console.error('[WebPush] Initialization failed:', error);
            return false;
        }
    }

    // Obtenir la clé publique VAPID du serveur
    async getVapidPublicKey() {
        try {
            const response = await fetch('/api/v1/webpush/vapid-public-key');
            const data = await response.json();
            this.vapidPublicKey = data.publicKey;
            console.log('[WebPush] VAPID public key obtained');
        } catch (error) {
            console.error('[WebPush] Failed to get VAPID public key:', error);
            throw error;
        }
    }

    // Vérifier s'il y a déjà une subscription
    async checkExistingSubscription() {
        try {
            this.subscription = await this.registration.pushManager.getSubscription();
            if (this.subscription) {
                console.log('[WebPush] Existing subscription found');
                return true;
            }
            return false;
        } catch (error) {
            console.error('[WebPush] Error checking existing subscription:', error);
            return false;
        }
    }

    // Demander permission et s'abonner aux notifications
    async requestPermissionAndSubscribe() {
        if (!this.isSupported) {
            throw new Error('Web Push not supported');
        }

        try {
            // Demander permission
            const permission = await Notification.requestPermission();
            console.log('[WebPush] Permission result:', permission);

            if (permission !== 'granted') {
                throw new Error('Notification permission denied');
            }

            // S'abonner aux notifications push
            await this.subscribe();

            return true;
        } catch (error) {
            console.error('[WebPush] Failed to request permission and subscribe:', error);
            throw error;
        }
    }

    // S'abonner aux notifications push
    async subscribe() {
        if (!this.vapidPublicKey) {
            await this.getVapidPublicKey();
        }

        try {
            // Créer la subscription
            this.subscription = await this.registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: this.urlBase64ToUint8Array(this.vapidPublicKey)
            });

            console.log('[WebPush] Push subscription created:', this.subscription);

            // Envoyer la subscription au serveur
            await this.sendSubscriptionToServer();

            return this.subscription;
        } catch (error) {
            console.error('[WebPush] Failed to subscribe:', error);
            throw error;
        }
    }

    // Envoyer la subscription au serveur
    async sendSubscriptionToServer() {
        if (!this.subscription || !this.currentUserId) {
            throw new Error('No subscription or user ID available');
        }

        try {
            const subscriptionData = {
                userId: this.currentUserId,
                endpoint: this.subscription.endpoint,
                p256dh: this.arrayBufferToBase64(this.subscription.getKey('p256dh')),
                auth: this.arrayBufferToBase64(this.subscription.getKey('auth'))
            };

            const response = await fetch('/api/v1/webpush/subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(subscriptionData)
            });

            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }

            const result = await response.json();
            console.log('[WebPush] Subscription sent to server:', result);

            return result;
        } catch (error) {
            console.error('[WebPush] Failed to send subscription to server:', error);
            throw error;
        }
    }

    // Se désabonner des notifications
    async unsubscribe() {
        if (!this.subscription) {
            console.log('[WebPush] No active subscription to unsubscribe');
            return true;
        }

        try {
            // Désabonner côté client
            const success = await this.subscription.unsubscribe();
            
            if (success) {
                // Informer le serveur
                await fetch('/api/v1/webpush/unsubscribe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        userId: this.currentUserId,
                        endpoint: this.subscription.endpoint
                    })
                });

                this.subscription = null;
                console.log('[WebPush] Successfully unsubscribed');
            }

            return success;
        } catch (error) {
            console.error('[WebPush] Failed to unsubscribe:', error);
            return false;
        }
    }

    // Envoyer une notification de test
    async sendTestNotification(title = 'Test Notification', message = 'This is a test notification') {
        if (!this.currentUserId) {
            throw new Error('No user ID available');
        }

        try {
            const response = await fetch('/api/v1/webpush/test-notification', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    userId: this.currentUserId,
                    title,
                    message
                })
            });

            if (!response.ok) {
                throw new Error(`Server responded with ${response.status}`);
            }

            const result = await response.json();
            console.log('[WebPush] Test notification sent:', result);

            return result;
        } catch (error) {
            console.error('[WebPush] Failed to send test notification:', error);
            throw error;
        }
    }

    // Obtenir le statut de l'abonnement
    getSubscriptionStatus() {
        return {
            isSupported: this.isSupported,
            hasPermission: Notification.permission === 'granted',
            isSubscribed: !!this.subscription,
            subscription: this.subscription
        };
    }

    // Utilitaires
    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }
}

// Instance globale
window.webPushManager = new WebPushManager();

// Interface utilisateur pour les notifications
class NotificationUI {
    constructor(webPushManager) {
        this.webPushManager = webPushManager;
        this.init();
    }

    init() {
        // Créer le bouton de notification dans la navbar
        this.createNotificationButton();
    }

    createNotificationButton() {
        const navbar = document.querySelector('.navbar-nav');
        if (!navbar) return;

        const notificationItem = document.createElement('li');
        notificationItem.className = 'nav-item dropdown';
        notificationItem.innerHTML = `
            <a class="nav-link dropdown-toggle" href="#" id="notificationDropdown" role="button" data-bs-toggle="dropdown">
                <i class="bi bi-bell"></i>
                <span id="notificationBadge" class="badge bg-danger" style="display:none;">0</span>
            </a>
            <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="notificationDropdown">
                <li><h6 class="dropdown-header">Notifications Desktop</h6></li>
                <li><hr class="dropdown-divider"></li>
                <li>
                    <button id="enableNotificationsBtn" class="dropdown-item">
                        <i class="bi bi-bell-fill me-2"></i>Activer les notifications
                    </button>
                </li>
                <li>
                    <button id="testNotificationBtn" class="dropdown-item" style="display:none;">
                        <i class="bi bi-send me-2"></i>Tester une notification
                    </button>
                </li>
                <li>
                    <button id="disableNotificationsBtn" class="dropdown-item" style="display:none;">
                        <i class="bi bi-bell-slash me-2"></i>Désactiver les notifications
                    </button>
                </li>
            </ul>
        `;

        navbar.appendChild(notificationItem);

        // Ajouter les event listeners
        this.setupEventListeners();
        this.updateUI();
    }

    setupEventListeners() {
        const enableBtn = document.getElementById('enableNotificationsBtn');
        const testBtn = document.getElementById('testNotificationBtn');
        const disableBtn = document.getElementById('disableNotificationsBtn');

        if (enableBtn) {
            enableBtn.addEventListener('click', async () => {
                try {
                    await this.webPushManager.requestPermissionAndSubscribe();
                    this.updateUI();
                    this.showToast('Notifications activées avec succès!', 'success');
                } catch (error) {
                    console.error('Failed to enable notifications:', error);
                    this.showToast('Erreur lors de l\'activation des notifications', 'error');
                }
            });
        }

        if (testBtn) {
            testBtn.addEventListener('click', async () => {
                try {
                    await this.webPushManager.sendTestNotification(
                        'Test AlertSystem',
                        'Ceci est une notification de test!'
                    );
                    this.showToast('Notification de test envoyée!', 'success');
                } catch (error) {
                    console.error('Failed to send test notification:', error);
                    this.showToast('Erreur lors de l\'envoi de la notification de test', 'error');
                }
            });
        }

        if (disableBtn) {
            disableBtn.addEventListener('click', async () => {
                try {
                    await this.webPushManager.unsubscribe();
                    this.updateUI();
                    this.showToast('Notifications désactivées', 'info');
                } catch (error) {
                    console.error('Failed to disable notifications:', error);
                    this.showToast('Erreur lors de la désactivation des notifications', 'error');
                }
            });
        }
    }

    updateUI() {
        const status = this.webPushManager.getSubscriptionStatus();
        const enableBtn = document.getElementById('enableNotificationsBtn');
        const testBtn = document.getElementById('testNotificationBtn');
        const disableBtn = document.getElementById('disableNotificationsBtn');

        if (status.isSubscribed) {
            enableBtn?.style.setProperty('display', 'none');
            testBtn?.style.setProperty('display', 'block');
            disableBtn?.style.setProperty('display', 'block');
        } else {
            enableBtn?.style.setProperty('display', 'block');
            testBtn?.style.setProperty('display', 'none');
            disableBtn?.style.setProperty('display', 'none');
        }
    }

    showToast(message, type = 'info') {
        // Créer un toast Bootstrap
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer();
        
        const toastId = 'toast-' + Date.now();
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);

        // Afficher le toast
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();

        // Supprimer après fermeture
        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1055';
        document.body.appendChild(container);
        return container;
    }
}

// Initialiser quand le DOM est prêt
document.addEventListener('DOMContentLoaded', () => {
    // Simuler un userId (à remplacer par la vraie logique d'authentification)
    const userId = 1; // Khalil par défaut
    
    // Initialiser le Web Push Manager
    window.webPushManager.init(userId).then((success) => {
        if (success) {
            console.log('[WebPush] Manager initialized successfully');
            // Créer l'interface utilisateur
            window.notificationUI = new NotificationUI(window.webPushManager);
        } else {
            console.warn('[WebPush] Manager initialization failed');
        }
    });
});
