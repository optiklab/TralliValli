# TralliValli
A self-hosted, invite-only messaging platform for family/friends with end-to-end encryption, built on .NET Core 8/RabbitMQ/MongoDB backend with TypeScript/React web client.

## Documentation
```
docs
├── Architecture at [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
├── REST API at [docs/API.md](docs/API.md).
├── Development at [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
├── Security at [docs/SECURITY.md](docs/SECURITY.md).
├── User guide at [docs/USER-GUIDE.md](docs/USER-GUIDE.md).
├── Web module at [docs/WEB_MODULE.md](docs/WEB_MODULE.md).
│   ├── Services [docs/WEB_MODULE_SERVICES.md](docs/WEB_MODULE_SERVICES.md).
│   ├── Stores [docs/WEB_MODULE_STORES.md](docs/WEB_MODULE_STORES.md).
│   ├── Message Thread component [docs/WEB_MODULE_COMPONENTS_MessageThread.md](docs/WEB_MODULE_COMPONENTS_MessageThread.md).
│   └── E2E [docs/WEB_MODULE_E2E.md](docs/WEB_MODULE_E2E.md).
└── Deployment at [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).
```

## Run

### Copy environment file
Copy-Item .env.example .env

### Start MongoDB, RabbitMQ, and Redis
docker-compose up -d

docker-compose ps

cd src/TraliVali.Api
dotnet run --launch-profile http 

cd src/web
npm install
npm run dev

## Quick run

From project root
> Copy-Item .env.example .env -ErrorAction SilentlyContinue
> docker-compose up -d
> dotnet run --project src/TraliVali.Api