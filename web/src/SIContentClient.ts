import SIContentClientOptions from './SIContentClientOptions';
import FileKey from './models/FileKey';
import { encodeBase64, hashDataAsync } from './helpers';

export { encodeBase64, hashDataAsync };

/** Defines SIContent service client. */
export default class SIContentClient {
	/**
	 * Initializes a new instance of SIContentClientOptions class.
	 * @param options Client options.
	 */
	constructor(private options: SIContentClientOptions) { }

	/** Gets package public uri.
	 * @param packageKey Unqiue package key.
	 */
	async tryGetPackageUriAsync(packageKey: FileKey) {
		return this.getInternalAsync(`content/packages/${encodeURIComponent(packageKey.hash)}/${encodeURIComponent(packageKey.name)}`);
	}

	/** Gets avatar public uri.
	 * @param avatarKey Unqiue avatar key.
	 */
	async tryGetAvatarUriAsync(avatarKey: FileKey) {
		return this.getInternalAsync(`content/avatars/${encodeURIComponent(avatarKey.hash)}/${encodeURIComponent(avatarKey.name)}`);
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
				throw new Error(`${response.status} ${await response.text()}`);
			}

			return await response.text();
		}
	
		return new Promise<string>((resolve, reject) => {
			const xhr = new XMLHttpRequest();
	
			xhr.onload = () => {
				if (xhr.status >= 200 && xhr.status < 300) {
					resolve(xhr.responseText);
				} else {
					reject(new Error(xhr.response));
				}
			};
			
			xhr.onerror = () => {
				reject(new Error(xhr.statusText || xhr.responseText || xhr.status.toString()));
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
			throw new Error(`${response.status} ${await response.text()}`);
		}

		return await response.text();
	}

	/**
	 * Uploads package to service if it does not exist.
	 * @param packageName Package name.
	 * @param packageData Package data.
	 * @param onProgress Progress callback.
	 */
	async uploadPackageIfNoExistAsync(
		packageName: string,
		packageData: Blob,
		onProgress: (progress: number) => void) {
		const packageKey: FileKey = {
			name: packageName,
			hash: encodeBase64(await hashDataAsync(await packageData.arrayBuffer()))
		};

		const packageUri = await this.tryGetPackageUriAsync(packageKey);

		if (packageUri) {
			return packageUri;
		}

		return await this.uploadPackageAsync(packageKey, packageData, onProgress);
	}

	/**
	 * Uploads avatar to service if it does not exist.
	 * @param avatarName Avatar name.
	 * @param avatarData Avatar data.
	 */
	async uploadAvatarIfNoExistAsync(avatarName: string, avatarData: Blob) {
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
			throw new Error(`Error while retrieving ${requestUri}: ${response.status} ${await response.text()}`);
		}

		return await response.blob();
	}

	private async getInternalAsync(uri: string) {
		const response = await fetch(`${this.options.serviceUri}/api/v1/${uri}`);

		if (!response.ok) {
			if (response.status === 404) {
				return null;
			}

			throw new Error(`Error while retrieving ${uri}: ${response.status} ${await response.text()}`);
		}

		return await response.text();
	}
}