SIContent service web client.

# Install

    npm install sicontent-client

# Example usage

```typescript
import SIContentClient from 'sicontent-client';

const client = new SIContentClient({ serviceUri: '<insert service address here>' });
const packageUri = await client.uploadPackageIfNotExistAsync(
    packageName,
    packageData,
    onStartUploadCallback,
    onUploadProgressCallback,
    onFinishUploadCallback);

console.log(packageUri);
```