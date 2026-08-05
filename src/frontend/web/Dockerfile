# =========================
# BUILD STAGE
# =========================
FROM node:20-alpine AS build
WORKDIR /app

# Install deps first so this layer is cached across source-only changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite inlines these into the bundle at build time -- they cannot be changed
# at container runtime afterwards, unlike the old dev-server setup.
ARG VITE_API_BASE_URL=http://localhost:5000
ARG VITE_USE_MOCK_API=false
ARG VITE_SEQ_BASE_URL=http://localhost:5341
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_USE_MOCK_API=${VITE_USE_MOCK_API}
ENV VITE_SEQ_BASE_URL=${VITE_SEQ_BASE_URL}

RUN npm run build

# =========================
# RUNTIME STAGE
# =========================
# 1.27-alpine sits on Alpine 3.21 with openssl 3.3.3-r0, which carries a fixable
# CRITICAL (CVE-2026-31789, fixed in 3.3.7-r0) and failed the Trivy gate in CI.
# 1.29-alpine is on Alpine 3.23 with openssl 3.5.6-r0. Pinned to the minor rather
# than plain `alpine` so a base-image bump is a visible commit, not a silent one.
FROM nginx:1.29-alpine AS final

RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
    && rm -rf /usr/share/nginx/html/* \
    && chown -R appuser:appgroup /var/cache/nginx /var/log/nginx

COPY nginx.conf /etc/nginx/conf.d/default.conf
# Kept out of conf.d/ so nginx's `include /etc/nginx/conf.d/*.conf` doesn't load
# it into the http context -- it is included explicitly per location instead.
COPY security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html

# nginx needs to bind :80, listen on an unprivileged port instead so the
# non-root user can start it without extra capabilities. The pid file also
# moves since /run isn't writable by a non-root user.
RUN sed -i 's/listen\s*80;/listen 8080;/' /etc/nginx/conf.d/default.conf \
    && sed -i 's#/run/nginx.pid#/tmp/nginx.pid#' /etc/nginx/nginx.conf \
    && touch /tmp/nginx.pid \
    && chown appuser:appgroup /tmp/nginx.pid

USER appuser
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8080/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
