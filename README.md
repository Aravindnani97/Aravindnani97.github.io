# Aravind Mallaiahgari Portfolio — ASP.NET Core MVC

Production-ready personal portfolio built with ASP.NET Core MVC (.NET 8).

## Stack
- ASP.NET Core MVC
- Razor Views
- .NET 8
- Docker
- Render deployment

## Security
- HTTPS redirection
- HSTS in production
- Content Security Policy
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- restrictive Permissions-Policy
- strict Referrer-Policy
- no database
- no secrets in source
- no third-party JavaScript
- health endpoint at /health

## Deploy free on Render
This repository includes Dockerfile and render.yaml for a free Render Web Service with managed HTTPS.
