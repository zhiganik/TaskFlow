# 08 — Docker Infrastructure

## Full Stack

| Container | Image | Port | Purpose |
|-----------|-------|------|---------|
| `taskflow_web` | Built from `docker/Dockerfile.web` | `3000→80` | React UI + Nginx proxy |
| `taskflow_api` | Built from `docker/Dockerfile` | `5000→8080` | ASP.NET Core 9 API + Swagger |
| `taskflow_postgres` | `postgres:16-alpine` | `5432` | PostgreSQL |
| `taskflow_redis` | `redis:7-alpine` | `6379` | Redis cache |

All on isolated `taskflow_net` bridge network. Services talk to each other by container name.

---

## How the Frontend Talks to the API

The React app is served by Nginx. Nginx proxies all `/api/*` requests to the `api` container on the internal Docker network. The browser only ever talks to one origin (`http://localhost:3000`) — **no CORS headers needed for normal usage**.

```
Browser → http://localhost:3000/api/v1/auth/login
  → Nginx (taskflow_web container)
    → http://api:8080/api/v1/auth/login (Docker internal network)
      → ASP.NET Core API
```

CORS is still configured on the API for cases where the browser hits port 5000 directly (Swagger testing, Postman, etc.).

---

## File Locations

```
TaskFlow/                               ← solution root
├── .env                                ← real secrets (never committed)
├── .env.example                        ← committed template
├── .gitignore
├── .dockerignore
├── Directory.Build.props
├── Makefile
│
├── docker/
│   ├── Dockerfile                      ← ASP.NET Core API image
│   ├── Dockerfile.web                  ← React frontend image (Node build → Nginx)
│   ├── docker-compose.yml              ← full stack (4 services)
│   ├── docker-compose.override.yml     ← dev overrides (Development env, debug logging)
│   └── nginx/
│       └── nginx.conf                  ← Nginx: serves React, proxies /api/*
│
├── taskflow-web/                       ← React + Vite + TypeScript project
│   ├── package.json
│   ├── vite.config.ts
│   └── src/
│
├── TaskFlow.Api/
├── TaskFlow.Application/
├── TaskFlow.Infrastructure/
└── TaskFlow.Tests/
```

---

## Makefile Commands

```bash
make up           # build + start all services (web, api, postgres, redis)
make down         # stop containers (volumes preserved)
make reset        # wipe all data and restart fresh
make build        # rebuild all images without cache

make logs         # tail all service logs
make api-logs     # API logs only
make web-logs     # Nginx/web logs only

make migrate      # run EF Core migrations
make shell        # bash inside API container
make db-shell     # psql inside postgres container
make redis-cli    # redis-cli inside redis container

make test         # NUnit tests on host (mocked — no Docker needed)
make web-install  # npm install for taskflow-web (run before first build)
```

---

## Dockerfile.web — Two Stage Build

```
Stage 1 (node:22-alpine)   npm ci → npm run build → /app/dist
Stage 2 (nginx:1.27-alpine) copies /app/dist → /usr/share/nginx/html
                             copies nginx.conf
                             runs: nginx -g "daemon off;"
```

Final image has no Node, no source code, no node_modules — just Nginx + static files.

---

## nginx.conf — Key Behaviours

```nginx
# Proxy all /api/* to the API container — same origin, no CORS
location /api/ {
    proxy_pass http://api:8080;
    client_max_body_size 25M;   # larger than API's 20MB limit — intentional buffer
}

# React Router — unknown paths return index.html
location / {
    try_files $uri $uri/ /index.html;
}

# Vite hashes JS/CSS filenames — safe to cache for 1 year
location ~* \.(js|css|...)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

---

## Env Var Flow

```
.env (host file)
  ↓  env_file in docker-compose
API container env — CORS_ORIGINS, ConnectionStrings__Postgres, REDIS_CONNECTION, JWT_SECRET
Web container env — VITE_API_URL (build arg, baked into JS at build time)
```

`VITE_API_URL` in `docker-compose.yml` is set to `""` (empty string) because the React app uses relative paths (`/api/v1/...`) and Nginx handles the proxy. No hardcoded `localhost:5000` in the built JS.

---

## CORS Setup in the API

```csharp
// In DependencyConfig — needed for direct browser → port 5000 access (Swagger, Postman)
services.AddCors(opts =>
    opts.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                Environment.GetEnvironmentVariable("CORS_ORIGINS")
                    ?.Split(',') ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// In ApplicationConfig — before UseAuthentication
app.UseCors("AllowFrontend");
```

---

## First-Time Setup

```bash
cp .env.example .env
# fill in .env values — JWT: openssl rand -base64 32

make web-install   # install React dependencies on host (for IDE support)
make up            # build all images and start
make migrate       # apply EF Core migrations
```

After `make up`:

| URL | What |
|-----|------|
| `http://localhost:3000` | React app — use this for all manual testing |
| `http://localhost:5000/swagger` | Swagger UI — direct API testing |

---

## Health Checks

`postgres` and `redis` have health checks. `api` uses `depends_on: condition: service_healthy`. `web` uses `depends_on: condition: service_started` on `api` — Nginx will retry proxied requests, so it doesn't need to wait for the API to be fully ready.

---

## Security Notes

- API runs as non-root `appuser`
- JWT stored in memory in React — never in `localStorage`
- `.env` blocked in both `.gitignore` and `.dockerignore`
- Nginx `client_max_body_size 25M` — slightly above the API's 20MB limit to avoid Nginx rejecting uploads before the API can return a proper 400
- No Node or build tools in the final web image
