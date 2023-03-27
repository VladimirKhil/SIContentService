import FileKey from '../src/models/FileKey';
import SIContentClient, { hashDataAsync } from '../src/SIContentClient';
import SIContentClientOptions from '../src/SIContentClientOptions';
import { randomBytes } from 'crypto';
import { PACKAGE_DATA } from './packageData';

const options: SIContentClientOptions = {
	//serviceUri: 'http://localhost:5165'
	serviceUri: 'http://vladimirkhil.com/sicontent'
};

const siContentClient = new SIContentClient(options);

const TEST_NGINX = false;

test('Upload avatar', async () => {
	const randomValues = randomBytes(20);
	const avatarData = new Blob([randomValues]);
	const avatarHash = await hashDataAsync(await avatarData.arrayBuffer());

	const avatarKey: FileKey = {
		name: `test_${Math.random()}.jpg`,
		hash: '2edejb/' + Buffer.from(avatarHash).toString('base64')
	};

	const noAvatar = await siContentClient.tryGetAvatarUriAsync(avatarKey);
	expect(noAvatar).toBeNull();

	const avatarUri = await siContentClient.uploadAvatarAsync(avatarKey, avatarData);
	expect(avatarUri).not.toBeNull();

	const avatarUri2 = await siContentClient.tryGetAvatarUriAsync(avatarKey);

	expect(avatarUri2).toBe(avatarUri);

	if (TEST_NGINX) {
		const avatarData = await siContentClient.getAsync(avatarUri);
		const avatarBuffer = Buffer.from(await avatarData.arrayBuffer());
		expect(avatarBuffer).toBe(randomValues);
	}
});

test('Upload package', async () => {
	const randomPackage = Buffer.from(PACKAGE_DATA, 'base64');
	const packageData = new Blob([randomPackage]);
	const packageHash = await hashDataAsync(await packageData.arrayBuffer());

	const packageKey: FileKey = {
		name: `test_${Math.random()}`,
		hash: Buffer.from(packageHash).toString('base64')
	};

	const noPackage = await siContentClient.tryGetPackageUriAsync(packageKey);
	expect(noPackage).toBeNull();

	const packageUri = await siContentClient.uploadPackageAsync(packageKey, packageData);
	expect(packageUri).not.toBeNull();

	const packageUri2 = await siContentClient.tryGetPackageUriAsync(packageKey);

	expect(packageUri2).toBe(packageUri);

	if (TEST_NGINX) {
		const imageData = await siContentClient.getAsync(`${packageUri}/Images/294F815D5DB6E7F7.PNG`);
		expect(imageData).not.toBeNull();

		expect(await siContentClient.getAsync(`${packageUri}/content.xml`)).toThrow();
	}
});
