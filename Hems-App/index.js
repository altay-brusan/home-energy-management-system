import 'react-native-url-polyfill/auto';
import 'react-native-get-random-values';
import { AppRegistry } from 'react-native';
import App from './src/App';
import { name as appName } from './app.json';
import { setDefaultFontFamily } from './src/utils/setDefaultFontFamily';

// Set Montserrat as default font family for all Text components
setDefaultFontFamily();

AppRegistry.registerComponent(appName, () => App);
