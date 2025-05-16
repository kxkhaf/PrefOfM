FROM node:18-alpine AS builder

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build 

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html

RUN mkdir -p /etc/nginx/ssl/crt /etc/nginx/ssl/private /etc/ssl/certs/trusted && chown -R nginx:nginx /etc/nginx/ssl

COPY certs/pfx /etc/ssl/certs/trusted
COPY certs/crt /etc/nginx/ssl/crt
COPY certs/private /etc/nginx/ssl/private
COPY nginx/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
