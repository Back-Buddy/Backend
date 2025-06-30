# BackBuddy Backend

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=black)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)
![Azure Service Bus](https://img.shields.io/badge/Azure%20Service%20Bus-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)
![Azure Container Apps](https://img.shields.io/badge/Azure%20Container%20Apps-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)

</div>

## 📋 About the Project

**BackBuddy Backend** is the core of an intelligent posture and sitting-time monitoring platform. The system was developed to help users develop healthier sitting habits and improve their posture.

### 🎯 Core Features

- **🔍 Real-time Sensor Data Processing**: Continuous monitoring and analysis of ESP32 sensor data
- **👤 User Management**: Complete authentication and authorization via Firebase
- **📱 Push Notifications**: Smart reminders based on sitting behavior
- **📊 Data Analytics**: Detailed statistics and reports on sitting habits
- **🔗 WebSocket Communication**: Bidirectional real-time communication with clients
- **🏗️ Microservice Architecture**: Modular design for maximum scalability

## 🏗️ Architecture & Tech Stack

### Microservices Overview

```mermaid
graph TB
    subgraph "Client Layer"
        APP[Mobile App]
        WEB[Web Dashboard]
        ESP[ESP32 Devices]
    end

    subgraph "API Gateway"
        API[API Service]
        WS[WebSocket Handler]
    end

    subgraph "Core Services"
        DS[Device Service]
        US[User Service]
        NS[Notification Service]
        DO[Device Orchestrator]
    end

    subgraph "Infrastructure"
        MONGO[(MongoDB)]
        REDIS[(Redis Cache)]
        MSG_DEV[RabbitMQ - Dev]
        MSG_PROD[Azure Service Bus - Prod]
        FIREBASE[Firebase]
        KEYVAULT_PROD[Azure KeyVault - Prod]
        ACR[Azure Container Registry]
        ACA[Azure Container Apps]
    end

    APP --> API
    WEB --> API
    ESP --> API
    API --> WS
    API --> DS
    API --> US
    API --> NS
    DO --> DS
    DS --> MONGO
    US --> FIREBASE
    NS --> MSG_DEV
    NS --> MSG_PROD
    DS --> REDIS
    US --> KEYVAULT_PROD

    %% Deployment
    ACR --> ACA
    API -.-> ACA
    DS -.-> ACA
    US -.-> ACA
    NS -.-> ACA
    DO -.-> ACA
```

### 📚 Tech Stack Details

#### Backend Framework

- **.NET 9.0** - Latest LTS version with improved performance
- **ASP.NET Core** - Web API framework with integrated DI
- **C# 13** - Latest language features and performance optimizations

#### Databases & Caching

- **MongoDB 7.x** - NoSQL database for flexible document storage
- **Redis** - In-memory cache for session management and real-time data
- **Azure KeyVault** - Production: Secure management of secrets and certificates

#### Message Broker & Communication

- **RabbitMQ** - Development: Local message broker for async communication
- **Azure Service Bus** - Production: Enterprise messaging service for reliable communication
- **MassTransit 8.x** - .NET Service Bus Framework for Enterprise Messaging
- **WebSockets** - Bidirectional real-time communication

#### Authentication & Authorization

- **Firebase Authentication** - Secure user authentication
- **JWT Bearer Tokens** - Stateless authentication
- **Firebase Admin SDK** - Server-side Firebase integration

#### DevOps & Infrastructure

- **Azure Container Apps** - Serverless container hosting platform
- **Azure Container Registry** - Private Docker registry
- **Docker & Docker Compose** - Containerization of all services
- **GitHub Actions** - CI/CD Pipeline
- **Multi-stage Dockerfile** - Optimized container builds

#### Testing & Quality Assurance

- **MSTest Framework** - Unit Testing
- **NSubstitute** - Mocking Framework
- **Integration Tests** - End-to-End Testing

#### Monitoring & Observability

- **Swagger/OpenAPI** - API Documentation
- **Scalar UI** - Modern API Explorer Alternative (for Emulator)
- **Structured Logging** - Comprehensive Application Logging
- **Health Checks** - Service health monitoring with readiness and liveness probes
- **Azure Application Insights** - Production monitoring, telemetry, and performance analytics
- **OpenTelemetry** - Distributed tracing and metrics collection

### 🏢 Service Details

#### **API Service** (Port: 8080)

- **Purpose**: Main entry point for all client requests
- **Technologies**: ASP.NET Core, JWT Authentication, Swagger
- **Functions**: REST API, WebSocket Gateway, Request Validation
- **Monitoring**: Health check endpoints (`/api/health/isalive`, `/api/health/isready`)

#### **Device Service**

- **Purpose**: Management of all devices and sensor data
- **Technologies**: MongoDB, Redis, MassTransit
- **Functions**: Device Registration, Data Processing, Threshold Management

#### **User Service**

- **Purpose**: User management and profile operations
- **Technologies**:
  - **Development**: Firebase Admin SDK, local configuration
  - **Production**: Firebase Admin SDK, Azure KeyVault
- **Functions**: User Authentication, Profile Management, Relationships

#### **Notification Service**

- **Purpose**: Push notifications and alerting
- **Technologies**:
  - **Development**: Firebase Cloud Messaging, RabbitMQ, Notification Emulator
  - **Production**: Firebase Cloud Messaging, Azure Service Bus
- **Functions**: Smart Notifications, Reminder System

#### **Device Orchestrator**

- **Purpose**: Coordination between devices and services
- **Technologies**: Background Services, MassTransit
- **Functions**: Data Orchestration, Event Processing

## 🚀 Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/get-started) (v4.0+)
- [Docker Compose](https://docs.docker.com/compose/install/) (v2.0+)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for local development)
- [Git](https://git-scm.com/downloads)

### Installation & Setup

1. **Clone repository**:

   ```powershell
   git clone https://github.com/Back-Buddy/Backend.git
   cd Backend/src
   ```

2. **Configure environment variables**:

   ```powershell
   cp Development.env.example Development.env
   # Edit Development.env with your values
   ```

3. **Start services**:

   ```powershell
   docker-compose up --build -d
   ```

4. **Verify services**:

   ```powershell
   # Check service status
   docker-compose ps

   # Verify API health
   curl http://localhost:8080/api/health/isalive

   # Access Swagger UI
   start http://localhost:8080/swagger

   # View logs
   docker-compose logs -f
   ```

### 🌐 Service Endpoints

| Service                   | URL                                                                                  | Description                     |
| ------------------------- | ------------------------------------------------------------------------------------ | ------------------------------- |
| **API Gateway**           | [http://localhost:8080](http://localhost:8080)                                       | Main API Endpoint               |
| **Swagger UI**            | [http://localhost:8080/swagger](http://localhost:8080/swagger)                       | API Documentation               |
| **Scalar API Explorer**   | [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1)                   | Modern API UI                   |
| **Health Checks**         | [http://localhost:8080/api/health/isalive](http://localhost:8080/api/health/isalive) | Service Health Status           |
| **Health Checks**         | [http://localhost:8080/api/health/isready](http://localhost:8080/api/health/isready) | Service Readiness Status        |
| **MongoDB Express**       | [http://localhost:8081](http://localhost:8081)                                       | Database Admin UI               |
| **Redis Insight**         | [http://localhost:5540](http://localhost:5540)                                       | Redis Management                |
| **RabbitMQ Management**   | [http://localhost:15672](http://localhost:15672)                                     | Message Broker UI (Dev)         |
| **Firebase Emulator**     | [http://localhost:4000](http://localhost:4000)                                       | Firebase Local Development      |
| **Notification Emulator** | [http://localhost:8083](http://localhost:8083)                                       | Push Notification Testing (Dev) |

### 🔐 Default Credentials

- **RabbitMQ**: `guest` / `guest` (Development only)
- **Redis**: Password: `my-password`
- **MongoDB**: No authentication (Development)

## 🗄️ Data Model & Relationships

### Entity Relationship Diagram

```mermaid
erDiagram
    USER {
        string userId_Firebase_PK
        string username
        string email
        string avatar_url
        datetime created_at
    }

    DEVICE {
        Guid id_PK
        string name
        string userId_FK
        TimeSpan threshold
        datetime secretGeneratedAt
        boolean active
    }

    DEVICE_LOGS {
        Guid id_PK
        Guid deviceId_FK
        datetime startTime
        datetime endTime
        enum logType "Sit|Error"
    }

    DEVICE_STATUS_REDIS {
        datetime startTime
    }

    REPORT {
        Guid id_PK
        string name
        enum visibilityType "All|Followers|Private"
        string userId_FK
        Guid deviceId_FK
        datetime startTime
        datetime endTime
        report_metadata metadata
        List_Guid usedLogs
        datetime createdAt
    }

    REPORT_METADATA {
        TimeSpan totalTime
        TimeSpan sitTime
        TimeSpan standTime
        double sitPercentage
        double standPercentage
        int postureChanges
        TimeSpan? averageSitPeriod
        TimeSpan? shortestSitPeriod
        TimeSpan? longestSitPeriod
    }

    REPORT_LIKE {
        Guid id_PK
        Guid reportId_FK
        string userId_FK
    }

    USER_FOLLOW {
        Guid id_PK
        string userId_FK
        string targetId_FK
        datetime createdAt
    }

    %% Relationships
    USER ||--o{ DEVICE : "owns"
    DEVICE ||--o{ DEVICE_LOGS : "generates"
    DEVICE ||--o| DEVICE_STATUS_REDIS : "has_current_state"
    DEVICE ||--o{ REPORT : "generates_reports"
    USER ||--o{ REPORT : "creates_reports"
    USER ||--o{ USER_FOLLOW : "follows_users"
    USER ||--o{ REPORT_LIKE : "likes_reports"
    REPORT ||--o{ REPORT_LIKE : "receives_likes"
    REPORT ||--o{ REPORT_METADATA: "owns"
```

### 📊 Database Collections

#### **Users Collection (Firebase)**

- **Primary Key**: Firebase UID (string)
- **Purpose**: User profile and authentication
- **Storage**: Firebase Firestore

#### **Devices Collection (MongoDB)**

- **Primary Key**: GUID
- **Purpose**: Device registration and configuration
- **Indexes**: `userId`

#### **DeviceLogs Collection (MongoDB)**

- **Primary Key**: GUID
- **Purpose**: Historical session data (sitting/standing periods)
- **Indexes**: `deviceId`

#### **Reports Collection (MongoDB)**

- **Primary Key**: GUID
- **Purpose**: User-generated reports with analytics metadata
- **Indexes**: `reportId`

#### **UserFollow Collection (MongoDB)**

- **Primary Key**: GUID
- **Purpose**: User relationship management (following system)
- **Indexes**: `id`, `userId`, `targetId`

#### **ReportLike Collection (MongoDB)**

- **Primary Key**: GUID
- **Purpose**: User likes on reports
- **Indexes**: `reportId`, `userId`

#### **Redis Cache Structure**

```
DeviceStatus:{deviceId} -> Current Device Status JSON
device_status_keys -> List of all DeviceIds where the status exists
DeviceConnected:{deviceId}:presence -> Device is Connected
DeviceConnected:{deviceId}:meta -> Device Connection Meta Data (e.g ConnectedAt)
```

## 📊 Monitoring & Observability

The BackBuddy Backend implements enterprise-grade monitoring and observability features for production-ready operation.

### Health Checks

#### Standard Health Check Endpoints

- **Liveness Probe**: `/api/health/isalive` - Basic service availability
- **Readiness Probe**: `/api/health/isready` - Service readiness for traffic
- **JSON Response Format**: Detailed health status information
- **Kubernetes Compatible**: Ready for container orchestration

#### Health Check Features

```http
GET /api/health/isalive
Content-Type: application/json

{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "self": {
      "status": "Healthy",
      "duration": "00:00:00.0045678"
    }
  }
}
```

### Application Insights & Telemetry

#### Azure Application Insights Integration

- **Custom Metrics**: Business-specific performance indicators
- **Exception Tracking**: Automatic error monitoring and alerting
- **Performance Monitoring**: Response times, throughput, and resource usage
- **Dependency Tracking**: Database, Redis, and external service calls

## 🧪 Testing & Quality Assurance

The BackBuddy Backend features a comprehensive test suite with **212 Integration Tests** and **30 Unit Tests**, ensuring robust quality assurance.

### Test Overview

| Test Type             | Count   | Coverage               |
| --------------------- | ------- | ---------------------- |
| **Integration Tests** | 212     | End-to-End API Testing |
| **Unit Tests**        | 30      | Core Business Logic    |
| **Total**             | **242** | Comprehensive Coverage |

### Integration Test Scaling

The integration tests are executed under realistic conditions where the **API is automatically scaled to 2 containers**. This enables:

- **Realistic Test Conditions**: Simulation of production-like environments
- **Container-to-Container Communication**: Validation of microservice communication
- **Load Balancing Testing**: Verification of load distribution between containers
- **Service Discovery**: Testing of cross-container service discovery

### Running Tests

#### Unit Tests

```powershell
# All Unit Tests (30 Tests)
dotnet test --collect:"XPlat Code Coverage"

# Specific Test Suite
dotnet test src/Core-Test/Core-Test.csproj -v detailed

# Device Service Tests
dotnet test src/DeviceService-Test/DeviceService-Test.csproj -v detailed

# With Coverage Report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

#### Integration Tests

```powershell
# Start infrastructure with API scaling (213 Integration Tests)
docker-compose -f docker-compose.test.yml up -d

# Run integration tests with 2 API containers
dotnet test src/Integration-Test/Integration-Test.csproj -v detailed

# Test cleanup
docker-compose -f docker-compose.test.yml down -v
```

#### All Tests

```powershell
# Run complete test suite (243 Tests total)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run with detailed output and coverage
dotnet test --logger:"console;verbosity=detailed" --collect:"XPlat Code Coverage"
```

## 🚀 Deployment & DevOps

### CI/CD Pipeline (GitHub Actions)

```mermaid
graph LR
    A[Push to main] --> B[Build & Test]
    B --> C[Build Docker Images]
    C --> D[Integration Tests]
    D --> E[Push to ACR]
    E --> F[Deploy to Container Apps]
```

### Deployment Environments

#### Development

```powershell
# Local development setup
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

#### Production (Azure)

- **Container Hosting**: Azure Container Apps
- **Container Registry**: Azure Container Registry
- **Key Management**: Azure KeyVault for secure secret storage
- **Scaling**: Automatic scaling based on CPU/Memory/HTTP requests
- **Monitoring**: Azure Application Insights
- **Logging**: Azure Log Analytics

### 🔧 Configuration

#### Environment Variables

```env
# Database Configuration
MONGODB_CONNECTION_STRING=mongodb://mongodb:27017/backbuddy
REDIS_CONNECTION_STRING=redis:6379

# Message Broker Configuration
# Development (RabbitMQ)
RABBITMQ_CONNECTION_STRING=amqp://guest:guest@rabbitmq:5672
# Production (Azure Service Bus)
AZURE_SERVICEBUS_CONNECTION_STRING=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key

# Firebase Configuration
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_PRIVATE_KEY_ID=your-key-id
FIREBASE_CLIENT_EMAIL=firebase-adminsdk@your-project.iam.gserviceaccount.com

# Azure Services (Production Only)
AZURE_KEYVAULT_URL=https://your-keyvault.vault.azure.net/
AZURE_CLIENT_ID=your-client-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CONTAINER_REGISTRY_URL=your-registry.azurecr.io

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
JWT_ISSUER=BackBuddy.Api
JWT_AUDIENCE=BackBuddy.Client

# Development: Secrets stored directly in environment variables or appsettings
# Production: Secrets retrieved from Azure KeyVault
```

### 📱 API Versioning & Documentation

#### API Versioning Strategy

- **URL Path Versioning**: `/api/v1/`, `/api/v2/`
- **Backward Compatibility**: Minimum 2 versions simultaneously
- **Deprecation Policy**: 6 months notice before breaking changes

#### OpenAPI Specification

- **Swagger UI**: Interactive API documentation
- **Scalar**: Modern alternative to Swagger UI
- **Code Generation**: Client SDKs for various languages
- **API Testing**: Integrated testing tools

## 🔒 Security & Compliance

### Security Measures

#### Authentication & Authorization

- **JWT Token Rotation**: Short lifespan with refresh tokens
- **Roating Secret**: Short lifespan for sensor secret

#### Data Protection

- **Encryption in Transit**: TLS 1.3 for all connections
- **Key Management**:
  - **Development**: Local configuration and environment variables
  - **Production**: Azure KeyVault integration

#### Security Scanning

```powershell
# Dependency Vulnerability Scan
dotnet list package --vulnerable --include-transitive

# Container Security Scan
docker scout cves backbuddy/api:latest

# Static Code Analysis
dotnet format --severity error --verify-no-changes
```

## 🤝 Development & Contribution

### Development Setup

#### Local Development Environment

```powershell
# Clone repository
git clone https://github.com/Back-Buddy/Backend.git
cd Backend/src

# Install dependencies
dotnet restore

# Start development services
docker-compose up -d mongodb redis rabbitmq

# Start API locally
dotnet run --project Api/Api.csproj
```

#### Code Guidelines

- **Code Style**: EditorConfig + .NET Coding Conventions
- **Commit Messages**: Conventional Commits Format
- **Branch Strategy**: GitFlow (main, develop, feature_x)
- **Code Reviews**: Minimum 2 approvals for production

#### Debugging

```powershell
# Start with debugger
dotnet run --project Api/Api.csproj --configuration Debug

# View logs
docker-compose logs -f api
```

### 🔍 Monitoring & Observability

#### Application Metrics

- **Azure Application Insights**: Application performance monitoring
- **Custom Metrics**: Business-specific KPIs
- **Performance Counters**: Response times, throughput
- **Error Tracking**: Exception rates, error types
- **Resource Usage**: CPU, memory, disk I/O

#### Logging Strategy

```csharp
// Structured Logging Example
logger.LogInformation("Device data processed: {DeviceId} - {DataPoints} points in {Duration}ms",
    deviceId, dataPoints, processingTime);
```

## 🐛 Troubleshooting

### Common Issues

#### Services won't start

```powershell
# Check Docker status
docker-compose ps

# Analyze logs
docker-compose logs [service-name]

# Restart containers
docker-compose restart [service-name]

# Complete restart
docker-compose down -v && docker-compose up -d
```

#### Database connection failed

```powershell
# Test MongoDB connection
docker exec -it backbuddy-mongodb-1 mongosh

# Test Redis connection
docker exec -it backbuddy-redis-1 redis-cli ping
```

#### Performance issues

```powershell
# Check resource usage
docker stats

# Database performance
# Enable MongoDB Slow Query Log
# Analyze Redis Memory Usage
```

## 🏆 Acknowledgments & Credits

### Open Source Libraries

- **ASP.NET Core Team** - Microsoft
- **MongoDB .NET Driver** - MongoDB Inc.
- **MassTransit** - Chris Patterson & Contributors
- **FirebaseAdmin** - Google
- **Docker** - Docker Inc.
- **RabbitMQ** - VMware (Development)
- **Azure Service Bus** - Microsoft (Production)
- **Azure KeyVault** - Microsoft (Production)

### Contributors

- **PlaySkyHD (Nic Markfort)** - Backend, Architecture & App Development
- **JPoepsel02 (Jannik Pöpsel)** - Feature Development (Backend & Sensor)
- **Ben-Bongo (Ben Steverding)** - App Development
- **LeonSCG (Leon Schmedding)** - Sensor Development / App Development

---

<div align="center">

**BackBuddy Backend**

[![GitHub Stars](https://img.shields.io/github/stars/Back-Buddy/Backend?style=social)](https://github.com/Back-Buddy/Backend)
[![GitHub Forks](https://img.shields.io/github/forks/Back-Buddy/Backend?style=social)](https://github.com/Back-Buddy/Backend/fork)
[![GitHub Issues](https://img.shields.io/github/issues/Back-Buddy/Backend)](https://github.com/Back-Buddy/Backend/issues)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

</div>
