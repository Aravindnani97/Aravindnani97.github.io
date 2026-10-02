# Portfolio CMS Setup

The ASP.NET Core app now includes:

- Public gallery: `/Gallery`
- Private admin login: `/Admin/Login`
- Private media manager: `/Admin`
- Photo/video uploads
- Publish / hide
- Delete
- Server-side Supabase integration
- Secure ASP.NET Core authentication cookie
- Antiforgery protection on admin actions

## 1. Create a Supabase project

Create a free Supabase project.

## 2. Run SQL

Open **SQL Editor** and run the contents of `supabase-setup.sql`.

## 3. Create storage bucket

In **Storage**, create a bucket named:

`portfolio`

Set it to **Public** so published gallery media can be displayed.

## 4. Create your admin user

In **Authentication -> Users**, create the one account you will use for portfolio administration.

Use your own email address. Visitors cannot create accounts through this website.

## 5. Add Render environment variables

In Render -> your service -> Environment, add:

- `SUPABASE_URL` = your Supabase Project URL
- `SUPABASE_PUBLISHABLE_KEY` = your Supabase publishable/anon key
- `SUPABASE_SECRET_KEY` = your Supabase server-side secret/service-role key
- `SUPABASE_BUCKET` = `portfolio`
- `ADMIN_EMAIL` = the exact email of your Supabase admin user

Never commit the secret key to GitHub.

After adding the variables, redeploy the service.

## URLs

- Public site: `/`
- Public work gallery: `/Gallery`
- Owner login: `/Admin/Login`
- Owner dashboard: `/Admin`
