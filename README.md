# AlertSystem - Production-Ready Alert Management System

A comprehensive, enterprise-grade alert management system built with .NET 9, featuring multi-channel notifications (Email, WhatsApp, Desktop), robust API integration, and advanced reminder capabilities.

## 🚀 Features

### Core Functionality
- **Multi-Channel Notifications**: Email (SMTP), WhatsApp (Meta Business API), Desktop Push Notifications
- **Dynamic Alert Management**: Create, send, and track alerts with real-time status updates
- **Confirmation System**: Token-based confirmation flow with secure URL generation
- **Reminder Engine**: Automated hourly reminders for critical alerts until confirmed
- **API Integration**: RESTful API with API key authentication and rate limiting
- **Background Processing**: Worker service for automated alert processing and reminders

### Advanced Features
- **Clean Architecture**: SOLID principles with Domain, Application, Infrastructure, and Presentation layers
- **Entity Framework Core**: Code-first migrations with SQL Server integration
- **Dependency Injection**: Comprehensive DI container configuration
- **Structured Logging**: Serilog integration with console and file logging
- **Configuration Management**: Secure .env file handling with DotNetEnv
- **Comprehensive Testing**: Unit tests, integration tests, and end-to-end testing

## 📋 Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB, Express, or Full)
- Node.js (for frontend development)
- Meta Business Account (for WhatsApp integration)
- SMTP Server (Gmail, Outlook, or custom)

## 🛠️ Installation & Setup

### 1. Clone Repository
   ```bash
   git clone <repository-url>
   cd AlertSystem
```

### 2. Database Setup
```bash
# Update connection string in appsettings.json
# Run migrations
dotnet ef database update --project AlertSystem.DataLayer.DB
```

### 3. Environment Configuration
Create `.env` file in project root:
```env
# Database
CONNECTION_STRING="Server=(localdb)\\MSSQLLocalDB;Database=AlertSystemDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30"

# Email Configuration
SMTP_HOST="smtp.gmail.com"
SMTP_PORT="587"
SMTP_USERNAME="your-email@gmail.com"
SMTP_PASSWORD="your-app-password"
SMTP_USE_TLS="true"

# WhatsApp Configuration
WHATSAPP_ACCESS_TOKEN="your-meta-access-token"
WHATSAPP_PHONE_NUMBER_ID="your-phone-number-id"
WHATSAPP_BUSINESS_ACCOUNT_ID="your-business-account-id"
WHATSAPP_DEFAULT_TEMPLATE_NAME="alert_notification"
WHATSAPP_DEFAULT_TEMPLATE_LANG="en_US"

# Application Configuration
BASE_URL="http://localhost:5185"
TOKEN_SECRET="your-secure-token-secret-key"

# Reminder Configuration
REMINDER_INTERVAL_MINUTES="60"
MAX_REMINDER_ATTEMPTS="24"
```

### 4. Install Dependencies
```bash
   dotnet restore
```

### 5. Run Applications
```bash
# Start Web Application
dotnet run --project AlertSystem.WEB

# Start API (separate terminal)
dotnet run --project AlertSystem.API

# Start Worker Service (separate terminal)
dotnet run --project AlertSystem.Worker
```

## 🔧 Configuration

### Database Configuration
The system uses Entity Framework Core with SQL Server. Key entities:
- `Alerte`: Main alert records
- `HistoriqueAlerte`: Recipient tracking and status
- `RappelSuivant`: Reminder history and attempts
- `ApiClients`: API key management
- `Users`: User management and desktop notifications

### Email Configuration
Supports multiple SMTP providers:
- **Gmail**: Use App Password (2FA required)
- **Outlook**: Use App Password
- **Custom SMTP**: Configure host, port, and credentials

### WhatsApp Configuration
Requires Meta Business Account setup:
1. Create Meta Business Account
2. Add WhatsApp Business API
3. Get Access Token and Phone Number ID
4. Create message templates (approval required)

### API Key Management
The system uses **SHA256 hashing** for API key security. Generate API keys using provided scripts:
```powershell
# Generate new API key
.\scripts\generate-api-key.ps1 -Name "Production Client"

# Rotate existing key
.\scripts\rotate-api-key.ps1 -ApiClientId 1

# Deactivate key
.\scripts\deactivate-api-key.ps1 -ApiClientId 1
```

#### Pre-configured Test API Keys
The system comes with multiple test API clients for realistic testing:

| Client Name | API Key | Environment | Rate Limit |
|-------------|---------|-------------|------------|
| Hotel Riviera - Production | `hotel-riviera-prod-key-2024-abc123def456` | Production | 1000/min |
| Hotel Paradise - Staging | `hotel-paradise-staging-key-2024-xyz789uvw012` | Staging | 500/min |
| Hotel Oasis - Development | `hotel-oasis-dev-key-2024-mno345pqr678` | Development | 200/min |
| Hotel Sunset - Testing | `hotel-sunset-test-key-2024-stu901vwx234` | Testing (Inactive) | 100/min |

## 📡 API Documentation

### Authentication
All API endpoints require API key authentication:
```http
X-API-KEY: your-api-key-here
```

### Endpoints

#### Create Alert
```http
POST /api/v1/alerts
Content-Type: application/json
X-API-KEY: your-api-key

{
  "title": "System Alert",
  "message": "Critical system issue detected",
  "alertType": "acquittementNécessaire",
  "expedType": "Service",
  "appId": 1,
  "recipients": [
    {
      "recipientId": "user@example.com",
      "type": "email"
    },
    {
      "recipientId": "+1234567890",
      "type": "whatsapp"
    }
  ]
}
```

#### Example API Calls with Different Clients

**Hotel Riviera (Production):**
```bash
curl -X POST "http://localhost:5002/api/v1/alerts" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: hotel-riviera-prod-key-2024-abc123def456" \
  -d '{
    "title": "Guest Check-in Alert",
    "message": "VIP guest has arrived and requires special attention",
    "alertType": "acquittementNécessaire",
    "expedType": "Service",
    "appId": 1,
    "recipients": [
      {"recipientId": "manager@hotelriviera.com", "type": "email"},
      {"recipientId": "+1234567890", "type": "whatsapp"}
    ]
  }'
```

**Hotel Paradise (Staging):**
```bash
curl -X POST "http://localhost:5002/api/v1/alerts" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: hotel-paradise-staging-key-2024-xyz789uvw012" \
  -d '{
    "title": "Maintenance Alert",
    "message": "Pool maintenance scheduled for tomorrow",
    "alertType": "acquittementNonNécessaire",
    "expedType": "Service",
    "appId": 1,
    "recipients": [
      {"recipientId": "staff@hotelparadise.com", "type": "email"}
    ]
  }'
```

**Hotel Oasis (Development):**
```bash
curl -X POST "http://localhost:5002/api/v1/alerts" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: hotel-oasis-dev-key-2024-mno345pqr678" \
  -d '{
    "title": "Test Alert",
    "message": "This is a development test alert",
    "alertType": "acquittementNécessaire",
    "expedType": "Service",
    "appId": 1,
    "recipients": [
      {"recipientId": "dev@hoteloasis.com", "type": "email"},
      {"recipientId": "+9876543210", "type": "whatsapp"}
    ]
  }'
```

#### Response Format
```json
{
  "alertId": 123,
  "overallSuccess": true,
  "results": [
    {
      "type": "email",
      "recipient": "user@example.com",
      "success": true,
      "error": null
    },
    {
      "type": "whatsapp",
      "recipient": "+1234567890",
      "success": false,
      "error": "Template not approved"
    }
  ]
}
```

### Status Codes
- `200 OK`: All notifications sent successfully
- `207 Multi-Status`: Partial success (some notifications failed)
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Missing or invalid API key
- `403 Forbidden`: API key inactive or rate limited
- `500 Internal Server Error`: Server error

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test AlertSystem.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
- **Unit Tests**: Individual component testing
- **Integration Tests**: Database and service integration
- **API Tests**: Endpoint testing with authentication
- **Worker Tests**: Background service testing

### Test Data
Tests use in-memory databases and mock services for isolation and speed.

## 🔄 Background Processing

### Worker Service
The `AlertePollingWorker` runs continuously and:
1. Processes alerts with `StatutId` 1 (En Cours) or 4 (Échoué)
2. Ignores alerts with `StatutId` 3 (Annulé)
3. Sends notifications via configured channels
4. Updates alert status to 2 (Envoyé) or 4 (Échoué)
5. Manages reminder scheduling for `acquittementNécessaire` alerts

### Reminder System
- **Trigger**: Alerts with `AlertTypeId` = 2 (acquittementNécessaire)
- **Frequency**: Configurable interval (default: 60 minutes)
- **Max Attempts**: Configurable limit (default: 24 attempts)
- **Stop Conditions**: All recipients confirmed or max attempts reached
- **History**: Complete audit trail in `RappelSuivant` table

## 🔐 Security

### API Key Security
- Keys are hashed using SHA256 before storage
- Rate limiting per client (configurable)
- Inactive key detection and blocking
- Key rotation capabilities

### Token Security
- JWT-based confirmation tokens
- Configurable expiration times
- Secure random generation
- URL-safe encoding

### Data Protection
- Connection strings in environment variables
- Sensitive configuration in `.env` files
- `.env` files excluded from version control
- Secure SMTP and API credentials

## 📊 Monitoring & Logging

### Structured Logging
- **Serilog** integration with multiple sinks
- **Console** output for development
- **File** logging for production
- **Structured** data for analysis

### Log Levels
- **Information**: Normal operations
- **Warning**: Non-critical issues
- **Error**: Failed operations
- **Debug**: Detailed debugging information

### Key Metrics
- Alert processing times
- Notification success rates
- API response times
- Worker service health
- Database connection status

## 🚀 Deployment

### Production Checklist
- [ ] Update connection strings for production database
- [ ] Configure production SMTP settings
- [ ] Set up Meta Business Account for WhatsApp
- [ ] Generate production API keys
- [ ] Configure logging for production
- [ ] Set up monitoring and alerting
- [ ] Configure SSL/TLS certificates
- [ ] Set up backup procedures

### Deployment Options

#### Docker Deployment
The project includes Dockerfiles for all components:

```bash
# Build and run with Docker Compose
docker-compose up -d

# Or build individual components
docker build -f AlertSystem.WEB/Dockerfile -t alertsystem-web .
docker build -f AlertSystem.API/Dockerfile -t alertsystem-api .
docker build -f AlertSystem.Worker/Dockerfile -t alertsystem-worker .
```

#### IIS Deployment
1. Publish the applications:
   ```bash
dotnet publish AlertSystem.WEB -c Release -o ./publish/web
dotnet publish AlertSystem.API -c Release -o ./publish/api
dotnet publish AlertSystem.Worker -c Release -o ./publish/worker
```

2. Configure IIS sites for WEB and API
3. Set up Windows Service for Worker using NSSM or similar

#### Azure App Service
1. Create three App Services (WEB, API, Worker)
2. Configure connection strings in App Settings
3. Set up Application Insights for monitoring
4. Configure custom domains and SSL certificates

#### Kubernetes Deployment
Use the provided Docker images with Kubernetes manifests for:
- Horizontal Pod Autoscaling
- Service mesh integration
- ConfigMaps for configuration
- Secrets for sensitive data

### Production Logging Configuration
The system includes production-ready logging configuration in `appsettings.Production.json`:

- **Console Logging**: For containerized environments
- **File Logging**: Rolling daily logs with 30-day retention
- **Seq Integration**: Structured logging server (optional)
- **Log Enrichment**: Machine name, thread ID, and context

### Environment Variables
All configuration can be overridden via environment variables:
- `CONNECTION_STRING`
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`
- `WHATSAPP_ACCESS_TOKEN`, `WHATSAPP_PHONE_NUMBER_ID`
- `BASE_URL`, `TOKEN_SECRET`
- `REMINDER_INTERVAL_MINUTES`, `MAX_REMINDER_ATTEMPTS`

## 🤝 Contributing

### Development Setup
1. Fork the repository
2. Create feature branch
3. Make changes with tests
4. Run test suite
5. Submit pull request

### Code Standards
- Follow SOLID principles
- Write comprehensive tests
- Use meaningful variable names
- Document public APIs
- Follow C# naming conventions

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

### Common Issues

#### Email Not Sending
- Verify SMTP credentials
   - Check firewall settings
- Ensure 2FA is enabled for Gmail
- Use App Password instead of regular password

#### WhatsApp Integration
- Verify Meta Business Account setup
- Check template approval status
- Ensure phone number is verified
- Verify access token permissions

#### Database Issues
- Check connection string format
- Verify SQL Server is running
- Ensure database exists
- Check user permissions

#### API Authentication
- Verify API key is active
- Check key hashing algorithm
- Ensure proper header format
- Verify rate limiting settings

### Getting Help
- Check logs for detailed error messages
- Review configuration settings
- Test with provided scripts
- Contact support team

## 🔄 Changelog

### Version 1.0.0
- Initial production release
- Multi-channel notification support
- API integration with authentication
- Background processing and reminders
- Comprehensive testing suite
- Production-ready configuration

---

**AlertSystem** - Reliable, scalable, and secure alert management for modern applications.