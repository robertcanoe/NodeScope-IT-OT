# ============================================================
# Stage 1: Build the frontend
# ============================================================
FROM node:20-bookworm-slim AS build

WORKDIR /app

COPY frontend/nodescope-web/package.json frontend/nodescope-web/pnpm-lock.yaml ./

RUN corepack enable \
    && corepack prepare pnpm@10.14.0 --activate

RUN pnpm install --frozen-lockfile

COPY frontend/nodescope-web/ ./

RUN pnpm build

# ============================================================
# Stage 2: Serve with Nginx
# ============================================================
FROM nginx:1.27-alpine AS final

WORKDIR /usr/share/nginx/html

COPY --from=build /app/dist/nodescope-web/browser ./
COPY frontend/nodescope-web/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
