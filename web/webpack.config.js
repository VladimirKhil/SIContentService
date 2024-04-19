const path = require('path');

module.exports = () => {
	return {
		mode: 'production',
		entry: './src/SIContentClient.ts',
		module: {
			rules: [
				{ test: /\.tsx?$/, use: 'ts-loader' }
			]
		},
		resolve: {
			extensions: ['.ts', '.js']
		},
		devtool: 'source-map',
		output: {
			path: path.resolve(__dirname, 'dist'),
			filename: '[name].js',
			library: 'SIContentClient',
			libraryTarget: 'umd',
			libraryExport: 'default',
			globalObject: 'this'
		},
		externals: {
			'cross-fetch': 'cross-fetch'
		},
		optimization: {
			splitChunks: {
				chunks: 'all',
				cacheGroups: {
					commons: {
						test: /[\\/]node_modules[\\/]/,
						name: 'vendor',
						chunks: 'all'
					}
				}
			}
		}
	}
};
