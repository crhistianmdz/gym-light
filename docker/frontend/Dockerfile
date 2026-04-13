# Build Stage
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies and build
COPY src/frontend/package.json ./
RUN npm install
COPY src/frontend/ .
RUN npm run build

# Production Stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]