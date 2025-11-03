import { useAuthStore } from '../store/authStore';

export function useRoles() {
  const { user, hasRole, hasAnyRole, hasAllRoles } = useAuthStore();

  return {
    user,
    roles: user?.roles || [],
    hasRole,
    hasAnyRole,
    hasAllRoles,
    isAdmin: hasRole('Administrator'),
    isUser: hasRole('User'),
    isGuest: hasRole('Guest'),
  };
}
