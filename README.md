# Aravind Mallaiahgari Portfolio — ASP.NET Core MVC

Production-ready personal portfolio built with **ASP.NET Core MVC (.NET 8)**.

## Stack
- ASP.NET Core MVC
- Razor Views
- .NET 8
- Docker
- Render deployment
- GitHub Actions CI

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
- health endpoint at `/health`

## Deploy free on Render

[![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=https://github.com/Aravindnani97/Aravindnani97.github.io)

The repository includes `render.yaml` and a Dockerfile. Render provides a managed HTTPS `*.onrender.com` address for the deployed web service.
