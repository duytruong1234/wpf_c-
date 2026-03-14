# Resources Folder

## Images Folder
This folder contains all uploaded images for ingredients (Nguyên li?u).

### Naming Convention
Images are automatically renamed when uploaded with the following format:
```
{originalName}_{timestamp}.{extension}
```

Example: `bot_mi_20260121095530.jpg`

### Supported Formats
- JPG/JPEG
- PNG
- GIF
- BMP

### Storage
- Images are stored in: `Resources/Images/`
- Database stores relative path: `/Resources/Images/{filename}`
- Maximum recommended size: 5MB per image

### Usage
When adding or editing ingredients:
1. Click "Ch?n file" button
2. Select an image file
3. The image will be copied to this folder
4. A unique filename will be generated to avoid conflicts
5. The relative path will be saved to the database
