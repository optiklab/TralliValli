# TralliValli
A self-hosted, invite-only messaging platform for family/friends with end-to-end encryption, built on .NET Core 8/RabbitMQ/MongoDB backend with TypeScript/React web client.

## API Documentation

Comprehensive REST API documentation is available in [docs/API.md](docs/API.md).

### Key Features

- **28 REST Endpoints**: Complete API coverage for messaging, file sharing, and administration
- **Authentication**: JWT-based authentication with magic links (passwordless)
- **Interactive Documentation**: Swagger UI available at `/swagger`
- **OpenAPI Specification**: Available at `/swagger/v1/swagger.json`

### Quick Start

1. Start the API:
   ```bash
   dotnet run --project src/TraliVali.Api/TraliVali.Api.csproj
   ```

2. Access Swagger UI:
   ```
   http://localhost:5000/swagger
   ```

3. Export OpenAPI Spec:
   ```bash
   ./scripts/export-openapi-spec.sh
   ```

For detailed endpoint documentation, request/response schemas, and curl examples, see [docs/API.md](docs/API.md).
