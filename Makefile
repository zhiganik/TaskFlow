-include .env

COMPOSE = docker compose --env-file .env -f docker/docker-compose.yml -f docker/docker-compose.override.yml

.PHONY: up down reset build logs api-logs web-logs file-worker-logs avatar-worker-logs migrate shell db-shell redis-cli rabbitmq-cli test web-install fix-uploads

up:
	$(COMPOSE) up --build -d --remove-orphans

down:
	$(COMPOSE) down

reset:
	$(COMPOSE) down -v
	$(COMPOSE) up --build -d

build:
	$(COMPOSE) build --no-cache

logs:
	$(COMPOSE) logs -f

api-logs:
	$(COMPOSE) logs -f api

web-logs:
	$(COMPOSE) logs -f web

file-worker-logs:
	$(COMPOSE) logs -f file-worker

avatar-worker-logs:
	$(COMPOSE) logs -f avatar-worker

notification-worker-logs:
	$(COMPOSE) logs -f notification-worker

rabbitmq-cli:
	$(COMPOSE) exec rabbitmq rabbitmqctl

migrate:
	dotnet ef database update \
		--project TaskFlow.Infrastructure \
		--startup-project TaskFlow.Api

migrate-railway:
	dotnet ef database update \
		--project TaskFlow.Infrastructure \
		--startup-project TaskFlow.Api \
		--connection "$(POSTGRES_CONNECTION_RAILWAY)"

shell:
	$(COMPOSE) exec api sh

db-shell:
	$(COMPOSE) exec postgres psql -U $${POSTGRES_USER} -d $${POSTGRES_DB}

redis-cli:
	$(COMPOSE) exec redis redis-cli -a $${REDIS_PASSWORD}

test:
	dotnet test

web-install:
	cd taskflow-web && npm install

fix-uploads:
	docker run --rm -v taskflow_uploads_data:/mnt alpine sh -c "mkdir -p /mnt/processed && chmod -R 777 /mnt"
