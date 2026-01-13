import React, { useEffect, useState, useRef } from 'react';
import { Image, Text, TextInput, TouchableOpacity, View, ScrollView, Pressable } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft } from 'lucide-react-native';
import PhoneInputLib from 'react-native-phone-number-input';
import useAuth from '../../../../hooks/useAuth';
import { useI18n } from '../../../../hooks/useI18n';
import useProfile from '../../../../hooks/useProfile';
import { useThemeStore } from '../../../../store/themeStore';
import { useSitesStore } from '../../../../store/sitesStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeProfileInfoStyles } from './ProfileInfoScreen.styles';

// Country name to ISO 3166-1 alpha-2 code mapping
const COUNTRY_NAME_TO_CODE: Record<string, string> = {
  'turkey': 'TR',
  'türkiye': 'TR',
  'turkiye': 'TR',
  'united states': 'US',
  'usa': 'US',
  'united kingdom': 'GB',
  'uk': 'GB',
  'england': 'GB',
  'germany': 'DE',
  'deutschland': 'DE',
  'france': 'FR',
  'italy': 'IT',
  'italia': 'IT',
  'spain': 'ES',
  'españa': 'ES',
  'netherlands': 'NL',
  'holland': 'NL',
  'belgium': 'BE',
  'austria': 'AT',
  'switzerland': 'CH',
  'sweden': 'SE',
  'norway': 'NO',
  'denmark': 'DK',
  'finland': 'FI',
  'poland': 'PL',
  'czech republic': 'CZ',
  'czechia': 'CZ',
  'greece': 'GR',
  'portugal': 'PT',
  'ireland': 'IE',
  'australia': 'AU',
  'canada': 'CA',
  'japan': 'JP',
  'china': 'CN',
  'india': 'IN',
  'brazil': 'BR',
  'mexico': 'MX',
  'russia': 'RU',
  'south korea': 'KR',
  'korea': 'KR',
  'saudi arabia': 'SA',
  'united arab emirates': 'AE',
  'uae': 'AE',
  'qatar': 'QA',
  'kuwait': 'KW',
  'egypt': 'EG',
  'south africa': 'ZA',
  'israel': 'IL',
  'singapore': 'SG',
  'malaysia': 'MY',
  'indonesia': 'ID',
  'thailand': 'TH',
  'vietnam': 'VN',
  'philippines': 'PH',
  'new zealand': 'NZ',
  'argentina': 'AR',
  'chile': 'CL',
  'colombia': 'CO',
  'peru': 'PE',
  'ukraine': 'UA',
  'romania': 'RO',
  'hungary': 'HU',
  'bulgaria': 'BG',
  'croatia': 'HR',
  'slovakia': 'SK',
  'slovenia': 'SI',
  'serbia': 'RS',
  'cyprus': 'CY',
  'malta': 'MT',
  'luxembourg': 'LU',
  'iceland': 'IS',
  'estonia': 'EE',
  'latvia': 'LV',
  'lithuania': 'LT',
  'azerbaijan': 'AZ',
  'georgia': 'GE',
  'armenia': 'AM',
  'kazakhstan': 'KZ',
  'uzbekistan': 'UZ',
  'pakistan': 'PK',
  'bangladesh': 'BD',
  'sri lanka': 'LK',
  'nepal': 'NP',
  'morocco': 'MA',
  'tunisia': 'TN',
  'algeria': 'DZ',
  'nigeria': 'NG',
  'kenya': 'KE',
  'ghana': 'GH',
  'tanzania': 'TZ',
  'ethiopia': 'ET',
  'afghanistan': 'AF',
};

const getCountryCodeFromName = (countryName: string | null | undefined): string => {
  if (!countryName) return 'TR';
  const normalized = countryName.toLowerCase().trim();
  return COUNTRY_NAME_TO_CODE[normalized] || 'TR';
};

// Phone calling code to ISO country code mapping (sorted by length desc for proper matching)
const CALLING_CODE_TO_COUNTRY: Array<{ callingCode: string; isoCode: string }> = [
  // 4-digit codes
  { callingCode: '1684', isoCode: 'AS' }, // American Samoa
  { callingCode: '1670', isoCode: 'MP' }, // Northern Mariana Islands
  { callingCode: '1671', isoCode: 'GU' }, // Guam
  { callingCode: '1787', isoCode: 'PR' }, // Puerto Rico
  { callingCode: '1939', isoCode: 'PR' }, // Puerto Rico
  // 3-digit codes
  { callingCode: '993', isoCode: 'TM' }, // Turkmenistan
  { callingCode: '994', isoCode: 'AZ' }, // Azerbaijan
  { callingCode: '995', isoCode: 'GE' }, // Georgia
  { callingCode: '996', isoCode: 'KG' }, // Kyrgyzstan
  { callingCode: '998', isoCode: 'UZ' }, // Uzbekistan
  { callingCode: '992', isoCode: 'TJ' }, // Tajikistan
  { callingCode: '971', isoCode: 'AE' }, // UAE
  { callingCode: '972', isoCode: 'IL' }, // Israel
  { callingCode: '973', isoCode: 'BH' }, // Bahrain
  { callingCode: '974', isoCode: 'QA' }, // Qatar
  { callingCode: '975', isoCode: 'BT' }, // Bhutan
  { callingCode: '976', isoCode: 'MN' }, // Mongolia
  { callingCode: '977', isoCode: 'NP' }, // Nepal
  { callingCode: '964', isoCode: 'IQ' }, // Iraq
  { callingCode: '965', isoCode: 'KW' }, // Kuwait
  { callingCode: '966', isoCode: 'SA' }, // Saudi Arabia
  { callingCode: '967', isoCode: 'YE' }, // Yemen
  { callingCode: '968', isoCode: 'OM' }, // Oman
  { callingCode: '961', isoCode: 'LB' }, // Lebanon
  { callingCode: '962', isoCode: 'JO' }, // Jordan
  { callingCode: '963', isoCode: 'SY' }, // Syria
  { callingCode: '880', isoCode: 'BD' }, // Bangladesh
  { callingCode: '886', isoCode: 'TW' }, // Taiwan
  { callingCode: '852', isoCode: 'HK' }, // Hong Kong
  { callingCode: '853', isoCode: 'MO' }, // Macau
  { callingCode: '855', isoCode: 'KH' }, // Cambodia
  { callingCode: '856', isoCode: 'LA' }, // Laos
  { callingCode: '374', isoCode: 'AM' }, // Armenia
  { callingCode: '375', isoCode: 'BY' }, // Belarus
  { callingCode: '376', isoCode: 'AD' }, // Andorra
  { callingCode: '377', isoCode: 'MC' }, // Monaco
  { callingCode: '378', isoCode: 'SM' }, // San Marino
  { callingCode: '380', isoCode: 'UA' }, // Ukraine
  { callingCode: '381', isoCode: 'RS' }, // Serbia
  { callingCode: '382', isoCode: 'ME' }, // Montenegro
  { callingCode: '383', isoCode: 'XK' }, // Kosovo
  { callingCode: '385', isoCode: 'HR' }, // Croatia
  { callingCode: '386', isoCode: 'SI' }, // Slovenia
  { callingCode: '387', isoCode: 'BA' }, // Bosnia
  { callingCode: '389', isoCode: 'MK' }, // North Macedonia
  { callingCode: '370', isoCode: 'LT' }, // Lithuania
  { callingCode: '371', isoCode: 'LV' }, // Latvia
  { callingCode: '372', isoCode: 'EE' }, // Estonia
  { callingCode: '373', isoCode: 'MD' }, // Moldova
  { callingCode: '353', isoCode: 'IE' }, // Ireland
  { callingCode: '354', isoCode: 'IS' }, // Iceland
  { callingCode: '355', isoCode: 'AL' }, // Albania
  { callingCode: '356', isoCode: 'MT' }, // Malta
  { callingCode: '357', isoCode: 'CY' }, // Cyprus
  { callingCode: '358', isoCode: 'FI' }, // Finland
  { callingCode: '359', isoCode: 'BG' }, // Bulgaria
  { callingCode: '351', isoCode: 'PT' }, // Portugal
  { callingCode: '352', isoCode: 'LU' }, // Luxembourg
  { callingCode: '420', isoCode: 'CZ' }, // Czech Republic
  { callingCode: '421', isoCode: 'SK' }, // Slovakia
  { callingCode: '423', isoCode: 'LI' }, // Liechtenstein
  { callingCode: '212', isoCode: 'MA' }, // Morocco
  { callingCode: '213', isoCode: 'DZ' }, // Algeria
  { callingCode: '216', isoCode: 'TN' }, // Tunisia
  { callingCode: '218', isoCode: 'LY' }, // Libya
  { callingCode: '220', isoCode: 'GM' }, // Gambia
  { callingCode: '221', isoCode: 'SN' }, // Senegal
  { callingCode: '223', isoCode: 'ML' }, // Mali
  { callingCode: '224', isoCode: 'GN' }, // Guinea
  { callingCode: '225', isoCode: 'CI' }, // Ivory Coast
  { callingCode: '226', isoCode: 'BF' }, // Burkina Faso
  { callingCode: '227', isoCode: 'NE' }, // Niger
  { callingCode: '228', isoCode: 'TG' }, // Togo
  { callingCode: '229', isoCode: 'BJ' }, // Benin
  { callingCode: '230', isoCode: 'MU' }, // Mauritius
  { callingCode: '231', isoCode: 'LR' }, // Liberia
  { callingCode: '232', isoCode: 'SL' }, // Sierra Leone
  { callingCode: '233', isoCode: 'GH' }, // Ghana
  { callingCode: '234', isoCode: 'NG' }, // Nigeria
  { callingCode: '235', isoCode: 'TD' }, // Chad
  { callingCode: '236', isoCode: 'CF' }, // Central African Republic
  { callingCode: '237', isoCode: 'CM' }, // Cameroon
  { callingCode: '238', isoCode: 'CV' }, // Cape Verde
  { callingCode: '239', isoCode: 'ST' }, // Sao Tome
  { callingCode: '240', isoCode: 'GQ' }, // Equatorial Guinea
  { callingCode: '241', isoCode: 'GA' }, // Gabon
  { callingCode: '242', isoCode: 'CG' }, // Congo
  { callingCode: '243', isoCode: 'CD' }, // DR Congo
  { callingCode: '244', isoCode: 'AO' }, // Angola
  { callingCode: '245', isoCode: 'GW' }, // Guinea-Bissau
  { callingCode: '248', isoCode: 'SC' }, // Seychelles
  { callingCode: '249', isoCode: 'SD' }, // Sudan
  { callingCode: '250', isoCode: 'RW' }, // Rwanda
  { callingCode: '251', isoCode: 'ET' }, // Ethiopia
  { callingCode: '252', isoCode: 'SO' }, // Somalia
  { callingCode: '253', isoCode: 'DJ' }, // Djibouti
  { callingCode: '254', isoCode: 'KE' }, // Kenya
  { callingCode: '255', isoCode: 'TZ' }, // Tanzania
  { callingCode: '256', isoCode: 'UG' }, // Uganda
  { callingCode: '257', isoCode: 'BI' }, // Burundi
  { callingCode: '258', isoCode: 'MZ' }, // Mozambique
  { callingCode: '260', isoCode: 'ZM' }, // Zambia
  { callingCode: '261', isoCode: 'MG' }, // Madagascar
  { callingCode: '262', isoCode: 'RE' }, // Reunion
  { callingCode: '263', isoCode: 'ZW' }, // Zimbabwe
  { callingCode: '264', isoCode: 'NA' }, // Namibia
  { callingCode: '265', isoCode: 'MW' }, // Malawi
  { callingCode: '266', isoCode: 'LS' }, // Lesotho
  { callingCode: '267', isoCode: 'BW' }, // Botswana
  { callingCode: '268', isoCode: 'SZ' }, // Eswatini
  { callingCode: '269', isoCode: 'KM' }, // Comoros
  { callingCode: '297', isoCode: 'AW' }, // Aruba
  { callingCode: '298', isoCode: 'FO' }, // Faroe Islands
  { callingCode: '299', isoCode: 'GL' }, // Greenland
  { callingCode: '350', isoCode: 'GI' }, // Gibraltar
  { callingCode: '500', isoCode: 'FK' }, // Falkland Islands
  { callingCode: '501', isoCode: 'BZ' }, // Belize
  { callingCode: '502', isoCode: 'GT' }, // Guatemala
  { callingCode: '503', isoCode: 'SV' }, // El Salvador
  { callingCode: '504', isoCode: 'HN' }, // Honduras
  { callingCode: '505', isoCode: 'NI' }, // Nicaragua
  { callingCode: '506', isoCode: 'CR' }, // Costa Rica
  { callingCode: '507', isoCode: 'PA' }, // Panama
  { callingCode: '508', isoCode: 'PM' }, // Saint Pierre
  { callingCode: '509', isoCode: 'HT' }, // Haiti
  { callingCode: '590', isoCode: 'GP' }, // Guadeloupe
  { callingCode: '591', isoCode: 'BO' }, // Bolivia
  { callingCode: '592', isoCode: 'GY' }, // Guyana
  { callingCode: '593', isoCode: 'EC' }, // Ecuador
  { callingCode: '594', isoCode: 'GF' }, // French Guiana
  { callingCode: '595', isoCode: 'PY' }, // Paraguay
  { callingCode: '596', isoCode: 'MQ' }, // Martinique
  { callingCode: '597', isoCode: 'SR' }, // Suriname
  { callingCode: '598', isoCode: 'UY' }, // Uruguay
  { callingCode: '599', isoCode: 'CW' }, // Curacao
  { callingCode: '670', isoCode: 'TL' }, // Timor-Leste
  { callingCode: '672', isoCode: 'NF' }, // Norfolk Island
  { callingCode: '673', isoCode: 'BN' }, // Brunei
  { callingCode: '674', isoCode: 'NR' }, // Nauru
  { callingCode: '675', isoCode: 'PG' }, // Papua New Guinea
  { callingCode: '676', isoCode: 'TO' }, // Tonga
  { callingCode: '677', isoCode: 'SB' }, // Solomon Islands
  { callingCode: '678', isoCode: 'VU' }, // Vanuatu
  { callingCode: '679', isoCode: 'FJ' }, // Fiji
  { callingCode: '680', isoCode: 'PW' }, // Palau
  { callingCode: '681', isoCode: 'WF' }, // Wallis and Futuna
  { callingCode: '682', isoCode: 'CK' }, // Cook Islands
  { callingCode: '683', isoCode: 'NU' }, // Niue
  { callingCode: '685', isoCode: 'WS' }, // Samoa
  { callingCode: '686', isoCode: 'KI' }, // Kiribati
  { callingCode: '687', isoCode: 'NC' }, // New Caledonia
  { callingCode: '688', isoCode: 'TV' }, // Tuvalu
  { callingCode: '689', isoCode: 'PF' }, // French Polynesia
  { callingCode: '690', isoCode: 'TK' }, // Tokelau
  { callingCode: '691', isoCode: 'FM' }, // Micronesia
  { callingCode: '692', isoCode: 'MH' }, // Marshall Islands
  // 2-digit codes
  { callingCode: '90', isoCode: 'TR' }, // Turkey
  { callingCode: '91', isoCode: 'IN' }, // India
  { callingCode: '92', isoCode: 'PK' }, // Pakistan
  { callingCode: '93', isoCode: 'AF' }, // Afghanistan
  { callingCode: '94', isoCode: 'LK' }, // Sri Lanka
  { callingCode: '95', isoCode: 'MM' }, // Myanmar
  { callingCode: '98', isoCode: 'IR' }, // Iran
  { callingCode: '20', isoCode: 'EG' }, // Egypt
  { callingCode: '27', isoCode: 'ZA' }, // South Africa
  { callingCode: '30', isoCode: 'GR' }, // Greece
  { callingCode: '31', isoCode: 'NL' }, // Netherlands
  { callingCode: '32', isoCode: 'BE' }, // Belgium
  { callingCode: '33', isoCode: 'FR' }, // France
  { callingCode: '34', isoCode: 'ES' }, // Spain
  { callingCode: '36', isoCode: 'HU' }, // Hungary
  { callingCode: '39', isoCode: 'IT' }, // Italy
  { callingCode: '40', isoCode: 'RO' }, // Romania
  { callingCode: '41', isoCode: 'CH' }, // Switzerland
  { callingCode: '43', isoCode: 'AT' }, // Austria
  { callingCode: '44', isoCode: 'GB' }, // United Kingdom
  { callingCode: '45', isoCode: 'DK' }, // Denmark
  { callingCode: '46', isoCode: 'SE' }, // Sweden
  { callingCode: '47', isoCode: 'NO' }, // Norway
  { callingCode: '48', isoCode: 'PL' }, // Poland
  { callingCode: '49', isoCode: 'DE' }, // Germany
  { callingCode: '51', isoCode: 'PE' }, // Peru
  { callingCode: '52', isoCode: 'MX' }, // Mexico
  { callingCode: '53', isoCode: 'CU' }, // Cuba
  { callingCode: '54', isoCode: 'AR' }, // Argentina
  { callingCode: '55', isoCode: 'BR' }, // Brazil
  { callingCode: '56', isoCode: 'CL' }, // Chile
  { callingCode: '57', isoCode: 'CO' }, // Colombia
  { callingCode: '58', isoCode: 'VE' }, // Venezuela
  { callingCode: '60', isoCode: 'MY' }, // Malaysia
  { callingCode: '61', isoCode: 'AU' }, // Australia
  { callingCode: '62', isoCode: 'ID' }, // Indonesia
  { callingCode: '63', isoCode: 'PH' }, // Philippines
  { callingCode: '64', isoCode: 'NZ' }, // New Zealand
  { callingCode: '65', isoCode: 'SG' }, // Singapore
  { callingCode: '66', isoCode: 'TH' }, // Thailand
  { callingCode: '81', isoCode: 'JP' }, // Japan
  { callingCode: '82', isoCode: 'KR' }, // South Korea
  { callingCode: '84', isoCode: 'VN' }, // Vietnam
  { callingCode: '86', isoCode: 'CN' }, // China
  // 1-digit code (must be last for NANP)
  { callingCode: '7', isoCode: 'RU' }, // Russia/Kazakhstan
  { callingCode: '1', isoCode: 'US' }, // USA/Canada (NANP)
];

// Parse phone number and extract country code
const parsePhoneNumber = (phoneNumber: string): { countryCode: string; localNumber: string } => {
  // Remove all non-digit characters
  const digits = phoneNumber.replace(/\D/g, '');

  // Try to match calling codes (longer codes first)
  for (const { callingCode, isoCode } of CALLING_CODE_TO_COUNTRY) {
    if (digits.startsWith(callingCode)) {
      return {
        countryCode: isoCode,
        localNumber: digits.slice(callingCode.length),
      };
    }
  }

  // Default to Turkey if no match
  return {
    countryCode: 'TR',
    localNumber: digits,
  };
};

const AVATAR_PLACEHOLDER =
  'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';

export default function ProfileInfoScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileInfoStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { user } = useAuth();
  const { getUserInfo, updateProfile, profileLoading, clearProfileError, clearProfileSuccess } = useProfile();
  const phoneInputRef = useRef<PhoneInputLib>(null);

  // Get user's first site to determine default country code
  const sites = useSitesStore(s => s.sites);
  const firstSiteCountry = sites.length > 0 ? sites[0].country : null;
  const siteBasedCountryCode = getCountryCodeFromName(firstSiteCountry);

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '', // Raw phone number without country code
    formattedPhone: '', // Full international format (+xx xxxxxxxxx)
  });
  const [defaultCountryCode, setDefaultCountryCode] = useState<string>(siteBasedCountryCode);
  const [phoneInputKey, setPhoneInputKey] = useState<number>(0); // Key for forcing re-render only on initial load
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [avatar, setAvatar] = useState(AVATAR_PLACEHOLDER);

  useEffect(() => {
    const loadUserInfo = async () => {
      if (user) {
        const userAvatar = user.name && user.surname
          ? `https://ui-avatars.com/api/?name=${user.name}+${user.surname}&background=2563eb&color=fff&size=128`
          : user.email
            ? `https://ui-avatars.com/api/?name=${user.email.split('@')[0]}&background=2563eb&color=fff&size=128`
            : AVATAR_PLACEHOLDER;

        try {
          const userInfo = await getUserInfo();
          let phoneNumber = '';
          let formattedPhone = '';

          console.log('Received phone from backend:', userInfo.phoneNumber); // Debug log

          if (userInfo.phoneNumber) {
            // Parse phone number using comprehensive country code detection
            const parsed = parsePhoneNumber(userInfo.phoneNumber);
            console.log('Parsed phone:', parsed); // Debug log

            phoneNumber = parsed.localNumber;
            formattedPhone = userInfo.phoneNumber;
            setDefaultCountryCode(parsed.countryCode);
          } else {
            // No phone number - use site-based country code as default
            setDefaultCountryCode(siteBasedCountryCode);
          }

          setForm({
            firstName: userInfo.name || user.name || '',
            lastName: userInfo.surname || user.surname || '',
            email: userInfo.email || user.email || '',
            phoneNumber,
            formattedPhone,
          });
          // Update key to force PhoneInput re-render with loaded data
          setPhoneInputKey(prev => prev + 1);
        } catch {
          setForm({
            firstName: user.name || '',
            lastName: user.surname || '',
            email: user.email || '',
            phoneNumber: '',
            formattedPhone: '',
          });
          setPhoneInputKey(prev => prev + 1);
        }
        setAvatar(userAvatar);
      }
    };
    loadUserInfo();
  }, [user, getUserInfo, siteBasedCountryCode]);

  const isRequired = (v: string) => v.trim().length >= 2;

  const validatePhone = () => {
    if (!form.formattedPhone) return true; // Optional field
    // Use the library's validation if available
    if (phoneInputRef.current) {
      return phoneInputRef.current.isValidNumber(form.formattedPhone);
    }
    // Basic validation: must start with + and have at least 10 digits
    const digits = form.formattedPhone.replace(/\D/g, '');
    return digits.length >= 10;
  };

  const handleSave = async () => {
    setError('');
    setMessage('');
    clearProfileError();
    clearProfileSuccess();

    if (!isRequired(form.firstName) || !isRequired(form.lastName)) {
      setError(t('profileInfo.fillAllFields'));
      return;
    }

    if (form.formattedPhone && !validatePhone()) {
      setError(t('profileInfo.invalidPhone'));
      return;
    }

    try {
      // Send phone number to backend
      // Backend expects digits only without + sign (e.g., "905551234567")
      let cleanedPhone = '';
      if (form.formattedPhone) {
        // Remove all non-digit characters including +
        cleanedPhone = form.formattedPhone.replace(/\D/g, '');
      }

      console.log('Sending phone to backend:', cleanedPhone); // Debug log

      await updateProfile({
        firstName: form.firstName,
        lastName: form.lastName,
        phone: cleanedPhone,
        email: form.email,
      });
      setMessage(t('profileInfo.profileUpdated'));
    } catch (err: any) {
      setError(err.message || t('profileInfo.profileUpdateError'));
    }
  };

  const handlePhoneChange = (text: string) => {
    setForm(f => ({ ...f, phoneNumber: text }));
  };

  const handleFormattedPhoneChange = (text: string) => {
    setForm(f => ({ ...f, formattedPhone: text }));
  };

  return (
    <View style={[styles.container, { paddingTop: insets.top }]}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{t('profileInfo.title')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 100 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Avatar Section */}
        <View style={styles.avatarSection}>
          <View style={styles.avatarContainer}>
            <Image source={{ uri: avatar }} style={styles.avatar} />
          </View>
          <Text style={styles.avatarName}>
            {form.firstName} {form.lastName}
          </Text>
          <Text style={styles.avatarEmail}>{form.email}</Text>
        </View>

        {/* Personal Information Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('profileInfo.personalDetails')}</Text>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.firstName')}</Text>
            <TextInput
              style={styles.inputField}
              value={form.firstName}
              onChangeText={v => setForm(f => ({ ...f, firstName: v }))}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.lastName')}</Text>
            <TextInput
              style={styles.inputField}
              value={form.lastName}
              onChangeText={v => setForm(f => ({ ...f, lastName: v }))}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>
        </View>

        {/* Contact Information Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('profileInfo.contactDetails')}</Text>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.email')}</Text>
            <TextInput
              style={[styles.inputField, styles.inputFieldDisabled]}
              value={form.email}
              editable={false}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.phone')}</Text>
            <PhoneInputLib
              key={phoneInputKey}
              ref={phoneInputRef}
              defaultValue={form.phoneNumber}
              defaultCode={defaultCountryCode as any}
              layout="first"
              onChangeText={handlePhoneChange}
              onChangeFormattedText={handleFormattedPhoneChange}
              placeholder={t('profileInfo.phonePlaceholder') || '5XX XXX XX XX'}
              withDarkTheme={isDark}
              withShadow={false}
              containerStyle={{
                width: '100%',
                height: 50,
                borderRadius: 10,
                backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.03)',
                borderWidth: 1,
                borderColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
              }}
              textContainerStyle={{
                backgroundColor: 'transparent',
                borderTopRightRadius: 10,
                borderBottomRightRadius: 10,
                paddingVertical: 0,
                paddingLeft: 0,
              }}
              textInputStyle={{
                fontSize: 15,
                color: COLORS.text,
                height: 48,
                paddingVertical: 0,
              }}
              codeTextStyle={{
                fontSize: 15,
                color: COLORS.text,
                marginLeft: -5,
              }}
              flagButtonStyle={{
                width: 60,
                marginLeft: 5,
              }}
              countryPickerButtonStyle={{
                backgroundColor: 'transparent',
                width: 60,
              }}
              textInputProps={{
                placeholderTextColor: COLORS.textSoft,
              }}
            />
          </View>
        </View>

        {/* Messages */}
        {message ? (
          <View style={[styles.messageBox, styles.successBox]}>
            <Text style={[styles.messageText, styles.successText]}>{message}</Text>
          </View>
        ) : null}

        {error ? (
          <View style={[styles.messageBox, styles.errorBox]}>
            <Text style={[styles.messageText, styles.errorText]}>{error}</Text>
          </View>
        ) : null}
      </ScrollView>

      {/* Bottom Buttons */}
      <View style={[styles.buttonContainer, { paddingBottom: insets.bottom + 16 }]}>
        <Pressable
          style={[styles.saveButton, profileLoading && { opacity: 0.6 }]}
          onPress={handleSave}
          disabled={profileLoading}
        >
          <Text style={styles.saveButtonText}>
            {profileLoading ? t('common.loading') : t('profileInfo.save')}
          </Text>
        </Pressable>
        <Pressable style={styles.cancelButton} onPress={() => navigation.goBack()}>
          <Text style={styles.cancelButtonText}>{t('profileInfo.cancel')}</Text>
        </Pressable>
      </View>
    </View>
  );
}
