import SIContentClientOptions from './SIContentClientOptions';
import FileKey from './models/FileKey';
import { encodeBase64, escapeBase64, hashDataAsync } from './helpers';
import SIContentServiceError from './models/SIContentServiceError';
import WellKnownSIContentServiceErrorCode from './models/WellKnownSIContentServiceErrorCode';
import InternalError from './models/InternalError';

export { encodeBase64, hashDataAsync, SIContentServiceError };

/** Defines SIContent service client. */
export default class SIContentClient {
	/**
	 * Initializes a new instance of SIContentClientOptions class.
	 * @param options Client options.
	 */
	constructor(public options: SIContentClientOptions) { }

	/** Gets package public uri.
	 * @param packageKey Unqiue package key.
	 */
	async tryGetPackageUriAsync(packageKey: FileKey) {
		return this.getInternalAsync(`content/packages/${escapeBase64(packageKey.hash)}/${encodeURIComponent(packageKey.name)}`);
	}

	/** Gets avatar public uri.
	 * @param avatarKey Unqiue avatar key.
	 */
	async tryGetAvatarUriAsync(avatarKey: FileKey) {
		return this.getInternalAsync(`content/avatars/${escapeBase64(avatarKey.hash)}/${encodeURIComponent(avatarKey.name)}`);
	}

	/**
	 * Uploads package to service.
	 * @param packageKey Unqiue package key.
	 * @param packageData Package data.
	 * @param onProgress Progress callback.
	 */
	async uploadPackageAsync(
		packageKey: FileKey,
		packageData: Blob,
		onProgress?: (progress: number) => void) {

		const formData = new FormData();
		formData.append('file', packageData, packageKey.name);

		// fetch() does not support reporting progress right now
		// Switch to fetch() when progress support would be implemented

		if (typeof XMLHttpRequest === 'undefined') {
			const response = await fetch(`${this.options.serviceUri}/api/v1/content/packages`, {
				method: 'POST',
				credentials: 'include',
				body: formData,
				headers: {
					'Content-MD5': packageKey.hash
				}
			});

			if (!response.ok) {
				const errorBody = await response.text();
				const errorCode = tryGetErrorCode(errorBody);

				throw new SIContentServiceError(errorBody, response.status, errorCode);
			}

			return await response.text();
		}

		return new Promise<string>((resolve, reject) => {
			const xhr = new XMLHttpRequest();

			xhr.onload = () => {
				if (xhr.status >= 200 && xhr.status < 300) {
					resolve(xhr.responseText);
				} else {
					const errorCode = tryGetErrorCode(xhr.responseText);
					reject(new SIContentServiceError(xhr.statusText || xhr.responseText, xhr.status, errorCode));
				}
			};

			xhr.onerror = () => {
				const errorCode = tryGetErrorCode(xhr.responseText);
				reject(new SIContentServiceError(xhr.statusText || xhr.responseText, xhr.status, errorCode));
			};

			xhr.upload.onprogress = (e) => {
				if (onProgress) {
					onProgress(e.loaded / e.total);
				}
			};

			xhr.open('post', `${this.options.serviceUri}/api/v1/content/packages`, true);
			xhr.setRequestHeader('Content-MD5', packageKey.hash);
			xhr.withCredentials = true;
			xhr.send(formData);
		});
	}

	/**
	 * Uploads avatar to service.
	 * @param avatarKey Unqiue avatar key.
	 * @param avatarData Avatar data.
	 */
	async uploadAvatarAsync(avatarKey: FileKey, avatarData: Blob) {
		const formData = new FormData();
		formData.append('file', avatarData, avatarKey.name);

		const response = await fetch(`${this.options.serviceUri}/api/v1/content/avatars`, {
			method: 'POST',
			credentials: 'include',
			body: formData,
			headers: {
				'Content-MD5': avatarKey.hash
			}
		});

		if (!response.ok) {
			const errorBody = await response.text();
			const errorCode = tryGetErrorCode(errorBody);

			throw new SIContentServiceError(errorBody, response.status, errorCode);
		}

		return await response.text();
	}

	/**
	 * Uploads package to service if it does not exist.
	 * @param packageName Package name.
	 * @param packageData Package data.
	 * @param onStartUpload Start upload callback.
	 * @param onUploadProgress Upload progress callback.
	 * @param onFinishUpload Finish upload callback.
	 */
	async uploadPackageIfNotExistAsync(
		packageName: string,
		packageData: Blob,
		onStartUpload: () => void,
		onUploadProgress: (progress: number) => void,
		onFinishUpload: () => void) {
		const packageKey: FileKey = {
			name: packageName,
			hash: encodeBase64(await hashDataAsync(await packageData.arrayBuffer()))
		};

		const packageUri = await this.tryGetPackageUriAsync(packageKey);

		if (packageUri) {
			return packageUri;
		}

		onStartUpload();

		try {
			return await this.uploadPackageAsync(packageKey, packageData, onUploadProgress);
		} finally {
			onFinishUpload();
		}
	}

	/**
	 * Uploads avatar to service if it does not exist.
	 * @param avatarName Avatar name.
	 * @param avatarData Avatar data.
	 */
	async uploadAvatarIfNotExistAsync(avatarName: string, avatarData: Blob) {
		const avatarKey: FileKey = {
			name: avatarName,
			hash: encodeBase64(await hashDataAsync(await avatarData.arrayBuffer()))
		};

		const avatarUri = await this.tryGetAvatarUriAsync(avatarKey);

		if (avatarUri) {
			return avatarUri;
		}

		return await this.uploadAvatarAsync(avatarKey, avatarData);
	}

	/**
	 * Gets resource by Uri.
	 * @param requestUri Resource Uri.
	 */
	async getAsync(requestUri: string) {
		const response = await fetch(`${this.options.serviceUri}/${requestUri}`);

		if (!response.ok) {
			throw new SIContentServiceError(`Error while retrieving ${requestUri}: ${await response.text()}`, response.status);
		}

		return await response.blob();
	}

	private async getInternalAsync(uri: string) {
		const response = await fetch(`${this.options.serviceUri}/api/v1/${uri}`);

		if (!response.ok) {
			if (response.status === 404) {
				return null;
			}

			throw new SIContentServiceError(`Error while retrieving ${uri}: ${await response.text()}`, response.status);
		}

		return await response.text();
	}
}

function tryGetErrorCode(errorBody: string) {
	let errorCode: WellKnownSIContentServiceErrorCode | undefined;

	try {
		const error = JSON.parse(errorBody) as InternalError;
		errorCode = error?.errorCode;
	} catch { /** Do nothing */ }

	return errorCode;
}
