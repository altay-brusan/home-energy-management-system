import React from 'react';
import {
  View,
  Text,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  Alert,
  ScrollView,
  Keyboard,
  TouchableWithoutFeedback,
} from 'react-native';
import Animated, {
  useSharedValue,
  useAnimatedStyle,
  withSpring,
  withTiming,
} from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema } from './login/login.schema';
import type { LoginForm } from './login/login.schema';

import MainLayout from '../layouts/MainLayout';
import Input from '../../components/forms/Input';
import Button from '../../components/ui/Button';
import ErrorMessage from '../../components/feedback/ErrorMessage';
import RegionSelector, {
  type Region,
} from '../../components/ui/RegionSelector/RegionSelector';

import useAuth from '../../hooks/useAuth';
import { makeLoginStyles } from './login/LoginScreen.styles';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { usePlatform } from '../../hooks/usePlatform';
import { Card } from '../../components';

type RootStackParamList = { Login: undefined; MainTabs: undefined };

export default function LoginScreen () {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { login, loginLoading, loginError, isAuthenticated } = useAuth();

  const mode = useThemeStore(s => s.mode);
  const toggleTheme = useThemeStore(s => s.toggle);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = React.useMemo(
    () => makeLoginStyles(COLORS, isDark),
    [COLORS, isDark],
  );

  const [region, setRegion] = React.useState<Region>('Europe');
  const [agreed, setAgreed] = React.useState(false);

  const scale = useSharedValue(1);
  const rotation = useSharedValue(0);
  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scale.value }, { rotateZ: `${rotation.value}deg` }],
  }));
  const onPressFancy = () => {
    scale.value = withSpring(1.03, {}, () => (scale.value = withSpring(1)));
    rotation.value = withTiming(
      360,
      { duration: 420 },
      () => (rotation.value = 0),
    );
  };

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  React.useEffect(() => {
    if (isAuthenticated) navigation.replace('MainTabs');
  }, [isAuthenticated, navigation]);

  const onSubmit = (values: LoginForm) => {
    if (!agreed) return;
    onPressFancy();
    void login(values);
  };

  const { isIOS } = usePlatform();

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <KeyboardAvoidingView
          style={styles.root}
          behavior={isIOS ? 'padding' : undefined}
        >
          <View style={styles.topBar}>
            <Button
              title={isDark ? '🌙 Dark' : '☀️ Light'}
              onPress={toggleTheme}
              variant="secondary"
              style={styles.themeToggle}
            />
            <View style={styles.regionWrap}>
              <RegionSelector
                selectedRegion={region}
                onRegionChange={setRegion}
              />
            </View>
          </View>

          <View style={styles.centerWrap}>
            <Card variant="surfaceDark" padding={24} style={styles.card}>
              <ScrollView
                contentContainerStyle={styles.scrollContent}
                keyboardShouldPersistTaps="handled"
                keyboardDismissMode="on-drag"
                showsVerticalScrollIndicator={false}
              >
                <View style={styles.cardInner}>
                  <Text style={styles.brand}>HEMS</Text>
                  <View style={styles.formWrap}>
                    <View style={styles.form}>
                      <Controller
                        control={control}
                        name="email"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            placeholder="Your Email"
                            keyboardType="email-address"
                            autoCapitalize="none"
                            autoCorrect={false}
                            autoComplete="email"
                            value={value}
                            onChangeText={onChange}
                            error={errors.email?.message}
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="password"
                        render={({ field: { onChange, value } }) => (
                          <Input
                            placeholder="Your Password"
                            secureTextEntry
                            autoCorrect={false}
                            autoComplete="password"
                            value={value}
                            onChangeText={onChange}
                            error={errors.password?.message}
                          />
                        )}
                      />
                      <Pressable onPress={() => Alert.alert('Forgot Password')}>
                        <Text style={styles.forgot}>Forgot Password?</Text>
                      </Pressable>
                      {!!loginError && (
                        <ErrorMessage message={String(loginError)} />
                      )}
                      <Pressable
                        onPress={() => setAgreed(v => !v)}
                        accessibilityRole="checkbox"
                        accessibilityState={{ checked: agreed }}
                        style={styles.tosRow}
                      >
                        <View
                          style={[
                            styles.checkbox,
                            agreed && styles.checkboxChecked,
                          ]}
                        >
                          {agreed && <Text style={styles.checkIcon}>✓</Text>}
                        </View>
                        <Text style={styles.tosText}>
                          I agree to the{' '}
                          <Text style={styles.tosLink}>Terms of Service</Text>{' '}
                          and <Text style={styles.tosLink}>Privacy Policy</Text>
                        </Text>
                      </Pressable>
                      <Pressable onPress={onPressFancy} disabled={!agreed}>
                        <Animated.View style={[animatedStyle]}>
                          <Button
                            title={
                              loginLoading || isSubmitting
                                ? 'Signing in…'
                                : 'Log in'
                            }
                            onPress={handleSubmit(onSubmit)}
                            style={[
                              styles.loginBtn,
                              !agreed && styles.loginBtnDisabled,
                            ]}
                            textStyle={styles.loginText}
                            disabled={!agreed || loginLoading || isSubmitting}
                          />
                        </Animated.View>
                      </Pressable>
                    </View>
                  </View>
                </View>
              </ScrollView>
            </Card>
          </View>
          <View style={styles.bgTopShade} pointerEvents="none" />
          <View style={styles.bgBottomShade} pointerEvents="none" />
        </KeyboardAvoidingView>
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
