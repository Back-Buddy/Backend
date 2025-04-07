# BackBuddy

## About

BackBuddy Backend is the server-side component of the BackBuddy system — a smart posture and sitting-time monitoring platform.
It provides REST and WebSocket APIs to handle:

- Sensor data ingestion and real-time processing
- User authentication and management
- Sensor configuration and assignment
- Push notification triggering
- Session statistics and analysis

Built with ASP.NET Core, the backend is containerized via Docker and supports CI/CD using GitHub Actions.
It serves as the central hub for communication between mobile clients, sensor devices (e.g. ESP32), and the database.

## Services

### Backend

The backend service is executed in a Docker container and can be accessed at [http://localhost:8080](http://localhost:8080).

### MongoDB

The MongoDB service is executed in a Docker container and can be accessed at [http://localhost:27017](http://localhost:27017).

## Requirements

- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [DotNet 9](https://dotnet.microsoft.com/en-us/download/dotnet)

## Installation

1. Clone the Repository:

   ```bash
   git clone https://github.com/Back-Buddy/Backend.git
   cd Backend
   ```

2. Change Directory:

   ```powershell
   cd ./src
   ```

3. Create and start the Docker-Container:

   ```bash
   docker-compose up --build -d
   ```

4. Open any browser and open the Swagger-UI:  
   [http://localhost:8080/swagger](http://localhost:8080/swagger)

## Database

1. **MongoDB**

   - Dev-UI: [http://localhost:8081](http://localhost:8081)

## Tests

### Requirements

- DotNet 9
- Docker

### Unit-Tests

**Command:**

```bash
dotnet test src/Api-Test/Api-Test.csproj -v d
```

### Integration-Tests

**You must execute all command with Powershell.**

**Command:**

1. Change Directory:

   ```powershell
   cd ./src
   ```

1. Start Docker-Container:

   ```powershell
   docker compose up --build -d
   ```

1. Execute Test:

   ```powershell
   dotnet test Integration-Test/Integration-Test.csproj -v d
   ```

## License

This project is released under the MIT License. For more information, see the \`LICENSE` file.
