import * as Rusha from 'rusha';

/** Converts byte array data into base64 string.
 * @param data Input data.
 */
export function encodeBase64(data: ArrayBuffer) : string {
	if (typeof Buffer !== 'undefined') {
		const buffer = Buffer.from(data);
		return buffer.toString('base64');
	}

	const hashArray = new Uint8Array(data);
	return window.btoa(String.fromCharCode.apply(null, hashArray as unknown as number[]));
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

/** Escapes Base64 string.
 * @param base64value Base64 string.
 */
export function escapeBase64(base64value: string): string {
	return base64value.replaceAll('/', '_').replaceAll('+', '-').replaceAll('=', '');
}