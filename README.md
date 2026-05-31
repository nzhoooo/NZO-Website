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
# NZO-Website
