import * as Rusha from 'rusha';

/** Converts byte array data into base64 string.
 * @param data Input data.
 */
export function encodeBase64(data: ArrayBuffer) : string {
	const buffer = Buffer.from(data);
	return buffer.toString('base64');
}

/** Hashes data.
 * @param data Data to hash.
 */
export async function hashDataAsync(data: ArrayBuffer): Promise<ArrayBuffer> {
	if (typeof location !== 'undefined' && location.protocol === 'https:') {
		return crypto.subtle.digest('SHA-1', data); // It works only under HTTPS protocol
	}

	return Rusha.createHash().update(data).digest();
}
