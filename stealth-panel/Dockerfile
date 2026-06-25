FROM node:20-alpine

WORKDIR /app

COPY package.json ./
RUN npm install

COPY panel.html panel.js panel.css ./

EXPOSE 80

CMD ["npx", "serve", ".", "-p", "80", "--no-clipboard", "--single"]
