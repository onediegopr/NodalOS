FROM node:20-alpine

RUN apk add --no-cache curl

WORKDIR /app

COPY package.json package-lock.json* ./

RUN npm install --production

RUN npx playwright install --with-deps chromium

COPY src/ ./src/
COPY config/ ./config/

ENV BRIDGE_HOST=bridge
ENV BRIDGE_PORT=8787
ENV NODE_ENV=production

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD curl -f http://localhost:8787/health || exit 1

CMD ["node", "src/index.js"]
