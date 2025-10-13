# AlertSystem - Multi-Channel Alert Management System

## Overview

AlertSystem is a comprehensive ASP.NET Core 9.0 application designed for multi-channel alert management with real-time capabilities. It supports Email, WhatsApp, and Desktop notifications through a unified API-first architecture.

## Features

- **Multi-Channel Notifications**: Email (SMTP), WhatsApp (Facebook Graph API), Desktop (WebPush/SignalR)
- **API-First Architecture**: RESTful API with Swagger documentation
- **Real-Time Dashboard**: SignalR-powered live updates
- **API Key Management**: Secure authentication with rate limiting
- **Reminder System**: Automated follow-up notifications
- **Comprehensive Logging**: Debug-friendly with extensive console logging

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- SQL Server LocalDB
- Visual Studio 2022 or VS Code with C# extension

### Installation

1. **Clone and Build**
   ```bash
   git clone <repository-url>
   cd AlertSystem
   dotnet restore
   dotnet build
   ```

2. **Database Setup**
   The application uses SQL Server LocalDB with automatic migrations. The database will be created automatically on first run.

3. **Configuration**
   Update `appsettings.json` with your settings:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AlertSystemDB;Trusted_Connection=True;TrustServerCertificate=True"
     },
     "Smtp": {
       "Host": "smtp.gmail.com",
       "Port": 465,
       "User": "your-email@gmail.com",
       "Pass": "your-app-password",
       "From": "your-email@gmail.com"
     },
     "WhatsApp": {
       "AccessToken": "your-whatsapp-business-token",
       "PhoneNumberId": "your-phone-number-id",
       "ApiVersion": "v22.0"
     }
   }
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```
   
   The application will be available at:
   - Dashboard: `https://localhost:7297`
   - API Documentation: `https://localhost:7297/swagger`

## API Usage

### 1. Create an API Client

First, create an API client to get your API key:

```bash
curl -X POST "https://localhost:7297/api/v1/clients" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Application",
    "rateLimitPerMinute": 100
  }'
```

**Response:**
```json
{
  "clientId": 1,
  "name": "My Application",
  "apiKey": "a1b2c3d4e5f6...", // Save this key!
  "isActive": true,
  "createdAt": "2025-10-13T10:00:00Z",
  "rateLimitPerMinute": 100
}
```

⚠️ **Important**: The API key is only shown once during creation. Save it securely!

### 2. Validate Your API Key

```bash
curl -X GET "https://localhost:7297/api/v1/keys/validate" \
  -H "X-Api-Key: your-api-key-here"
```

### 3. Send an Alert

```bash
curl -X POST "https://localhost:7297/api/v1/alerts" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key-here" \
  -d '{
    "title": "System Maintenance",
    "message": "Scheduled maintenance will begin at 2 AM UTC",
    "alertType": "acquittementNonNécessaire",
    "expedType": "Service",
    "appId": 1,
    "recipients": [
      {"externalRecipientId": "admin@company.com"},
      {"externalRecipientId": "+21612345678"},
      {"externalRecipientId": "device-admin-001"}
    ]
  }'
```

### 4. Query Alerts

```bash
curl -X GET "https://localhost:7297/api/v1/alerts?page=1&size=10&sort=dateCreation&order=desc" \
  -H "X-Api-Key: your-api-key-here"
```

## Dashboard Usage

### 1. Access the Dashboard

Navigate to `https://localhost:7297` to access the web dashboard.

### 2. Configure API Key

1. Click "Nouvelle alerte" to open the alert modal
2. Scroll to the "Configuration API" section
3. Enter your API key and click "Sauvegarder"
4. Click "Tester" to validate the key

### 3. Send Alerts

1. Fill in the alert title and message
2. Select alert type (Information/Obligatoire)
3. Choose delivery platforms (Email/WhatsApp/Desktop)
4. Add recipients using the tag input fields
5. Click "Envoyer l'alerte"

## Recipient Format

The system automatically detects recipient types:

- **Email**: `user@domain.com`
- **WhatsApp**: `+21612345678` (international format)
- **Device ID**: `device-abc123` (8-64 alphanumeric characters)

## API Endpoints

### Clients Management
- `POST /api/v1/clients` - Create API client
- `GET /api/v1/clients` - List clients
- `GET /api/v1/clients/{id}` - Get client details
- `PATCH /api/v1/clients/{id}/activate` - Activate client
- `PATCH /api/v1/clients/{id}/deactivate` - Deactivate client
- `PATCH /api/v1/clients/{id}/rate-limit` - Update rate limit
- `DELETE /api/v1/clients/{id}` - Delete client

### API Key Validation
- `GET /api/v1/keys/validate` - Validate API key (requires X-Api-Key header)
- `POST /api/v1/keys/test` - Test API key (key in request body)

### Alerts Management
- `POST /api/v1/alerts` - Create alert
- `GET /api/v1/alerts` - Query alerts (with filtering and pagination)
- `GET /api/v1/alerts/{id}` - Get alert details
- `POST /api/v1/alerts/{id}/read` - Mark alert as read

## Configuration

### Email (SMTP)
Configure Gmail or other SMTP providers in `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 465,
  "User": "your-email@gmail.com",
  "Pass": "your-app-password",
  "UseStartTls": false
}
```

For Gmail, use an App Password instead of your regular password.

### WhatsApp Business API
1. Set up a WhatsApp Business account
2. Get your access token and phone number ID from Facebook Developer Console
3. Configure in `appsettings.json`:

```json
"WhatsApp": {
  "AccessToken": "your-token",
  "PhoneNumberId": "your-phone-id",
  "ApiVersion": "v22.0"
}
```

### WebPush Notifications
Configure VAPID keys for browser push notifications:

```json
"WebPush": {
  "PublicKey": "your-vapid-public-key",
  "PrivateKey": "your-vapid-private-key",
  "Subject": "mailto:admin@yourcompany.com"
}
```

## Testing

### PowerShell Test Script

Run the included test script to verify API functionality:

```powershell
.\test-api.ps1
```

This script will:
1. Create an API client
2. Validate the API key
3. Send a test alert
4. Query alerts
5. Retrieve alert details

### Manual Testing

1. **Dashboard Test**: Use the web interface to send alerts
2. **API Test**: Use curl or Postman with the provided examples
3. **Notification Test**: Verify emails and WhatsApp messages are delivered

## Deployment

### IIS Deployment

1. **Publish the Application**
   ```bash
   dotnet publish -c Release -o C:\inetpub\wwwAlertSystem
   ```

2. **Configure IIS**
   - Create application pool (.NET CLR version: No Managed Code)
   - Create website pointing to published folder
   - Ensure ASP.NET Core Hosting Bundle is installed

3. **Database Connection**
   Update connection string for production SQL Server in `appsettings.Production.json`

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AlertSystem.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertSystem.dll"]
```

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Ensure SQL Server LocalDB is installed
   - Check connection string format
   - Verify database permissions

2. **API Key Issues**
   - Ensure X-Api-Key header is included
   - Check if client is active
   - Verify rate limits

3. **Email Not Sending**
   - Check SMTP configuration
   - Verify Gmail App Password
   - Check firewall settings

4. **WhatsApp Issues**
   - Verify access token is valid
   - Check phone number format (+country code)
   - Ensure WhatsApp Business API is properly configured

### Debug Logging

The application includes extensive debug logging. Check the browser console for detailed information about:
- API calls and responses
- Form validation
- Notification sending
- Error details

### Performance Considerations

- API key validation is cached for 5 minutes
- Rate limiting uses in-memory cache (sliding window)
- Database queries use proper indexing
- SignalR connections are managed automatically

## Security

- API keys are hashed using BCrypt (work factor 12)
- Rate limiting prevents abuse
- Input validation on all endpoints
- HTTPS enforced in production
- SQL injection protection via Entity Framework

## Support

For issues and questions:
1. Check the browser console for debug information
2. Review application logs
3. Verify configuration settings
4. Test with the provided PowerShell script

## License

[Your License Here]
