# Docker Setup for GymFlow Lite

## Step 1: Configure Environment Variables
Copy the provided `.env.example` file to `.env`:

```
cp docker/.env.example docker/.env
```

Customize the values as needed. Ensure your `POSTGRES_PASSWORD` and `JWT_SECRET` are strong values.

## Step 2: Start the Services
Run the following command to spin up all required services:

```
docker-compose -f docker/docker-compose.yml up -d
```

## Step 3: Access the Application
- Backend: http://localhost:5000
- Frontend: http://localhost:3000

## Step 4: Manage Database Migrations
To run EF Core migrations, follow these steps inside the backend container:

```
docker exec -it <backend-container-id> bash
DOTNET_ENVIRONMENT=Development dotnet ef database update
```

## Notes
- Ensure Docker and Docker Compose are correctly installed on your system.
- Check logs with `docker-compose logs -f`.
- Stop services with `docker-compose down`.