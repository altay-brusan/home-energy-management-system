import React from 'react';
import {
  View,
  Text,
  ScrollView,
  Alert,
  StyleSheet,
} from 'react-native';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input, Button } from '../../components';

const loginSchema = z.object({
  email: z.string()
    .min(1, 'Email is required')
    .email('Invalid email format'),
  password: z.string()
    .min(6, 'Password must be at least 6 characters')
    .max(20, 'Password must be less than 20 characters'),
  username: z.string()
    .min(3, 'Username must be at least 3 characters')
    .max(15, 'Username must be less than 15 characters'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function FormExampleScreen() {
  const {
    control,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      username: '',
    },
  });

  const onSubmit = (data: LoginFormData) => {
    Alert.alert('Form Data', JSON.stringify(data, null, 2));
    reset();
  };

  return (
    <ScrollView style={styles.container}>
      <View style={styles.content}>
        <Text style={styles.title}>Form Examples with Zodd</Text>

        <Controller
          control={control}
          name="username"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label="Username"
              onBlur={onBlur}
              onChangeText={onChange}
              value={value}
              placeholder="Enter username"
              error={errors.username?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="email"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label="Email"
              onBlur={onBlur}
              onChangeText={onChange}
              value={value}
              placeholder="Enter email"
              keyboardType="email-address"
              autoCapitalize="none"
              error={errors.email?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="password"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label="Password"
              onBlur={onBlur}
              onChangeText={onChange}
              value={value}
              placeholder="Enter password"
              secureTextEntry
              error={errors.password?.message}
            />
          )}
        />

        <Button
          title="Submit"
          onPress={handleSubmit(onSubmit)}
          style={styles.submitButton}
        />

        <Button
          title="Reset Form"
          onPress={() => reset()}
          variant="secondary"
          style={styles.resetButton}
        />
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  content: {
    padding: 24,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 24,
    color: '#1f2937',
  },
  submitButton: {
    marginTop: 16,
  },
  resetButton: {
    marginTop: 12,
  },
});
