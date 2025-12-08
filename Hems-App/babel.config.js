module.exports = api => {
  const isProduction = api.env('production');

  const plugins = [
    ['module:react-native-dotenv', {
      moduleName: '@env',
      path: '.env',
      safe: false,
      allowUndefined: true,
    }],
    'react-native-reanimated/plugin'
  ];

  // Production'da console.log'ları kaldır
  if (isProduction) {
    plugins.unshift(['transform-remove-console', { exclude: ['error', 'warn'] }]);
  }

  return {
    presets: ['module:@react-native/babel-preset'],
    plugins
  };
};
