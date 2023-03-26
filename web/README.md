SIContent service web client.

Example usage:

```typescript
import SpardClient from 'sicontent-client';

const client = new SIContentClient({ serviceUri: '<insert service address here>' });
const result = await client.transformAsync({ input: 'aaa', transform: 'a => b' });

console.log(result); // { result: 'bbb', duration: '...' }
```