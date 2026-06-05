# NZO Website

Photography portfolio website for NZO, built with ASP.NET Core MVC.

## Cloudinary Uploads

Admin uploads use Cloudinary when these environment variables are configured:

```bash
CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME
```

Alternatively, configure the values separately:

```bash
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret
```

If Cloudinary is not configured, uploads fall back to local `wwwroot/uploads/admin` storage for development.

## Railway Saved Admin Data

The admin page saves series, carousel choices, and uploaded image records to `App_Data/site-settings.json` by default. That file is inside the deployable app folder, so a Railway deploy can replace live admin changes with the local copy.

For Railway, mount a persistent volume and set these environment variables to paths on that volume:

```bash
SITE_SETTINGS_PATH=/data/site-settings.json
ADMIN_UPLOADS_PATH=/data/uploads/admin
```

If all admin images are uploaded to Cloudinary, `SITE_SETTINGS_PATH` is the important one. `ADMIN_UPLOADS_PATH` only matters for local fallback uploads.
# NZO-Website
