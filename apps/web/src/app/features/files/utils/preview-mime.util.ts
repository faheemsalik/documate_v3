/** Infer preview MIME when browsers/storage record application/octet-stream. */
export function resolvePreviewMime(
  contentType?: string | null,
  fileName?: string | null,
): string {
  const ct = (contentType ?? '').trim().toLowerCase();
  if (ct && ct !== 'application/octet-stream' && ct !== 'binary/octet-stream') {
    return ct;
  }

  const name = (fileName ?? '').trim().toLowerCase();
  const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
  switch (ext) {
    case '.pdf':
      return 'application/pdf';
    case '.png':
      return 'image/png';
    case '.jpg':
    case '.jpeg':
      return 'image/jpeg';
    case '.gif':
      return 'image/gif';
    case '.webp':
      return 'image/webp';
    case '.tif':
    case '.tiff':
      return 'image/tiff';
    default:
      return ct || 'application/octet-stream';
  }
}

export function isInlinePreviewable(contentType?: string | null, fileName?: string | null): boolean {
  const mime = resolvePreviewMime(contentType, fileName);
  return mime.includes('pdf') || mime.startsWith('image/');
}

export function isPdfMime(contentType?: string | null, fileName?: string | null): boolean {
  return resolvePreviewMime(contentType, fileName).includes('pdf');
}

/** Re-wrap blob with a usable MIME so PDF iframe/img preview works. */
export function blobForPreview(blob: Blob, contentType?: string | null, fileName?: string | null): Blob {
  const mime = resolvePreviewMime(contentType || blob.type, fileName);
  if (!mime || mime === blob.type) {
    return blob;
  }
  return new Blob([blob], { type: mime });
}
