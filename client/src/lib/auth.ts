import type { User } from '@supabase/supabase-js'

export function getUserProviders(user: User | null): string[] {
  const providers = user?.app_metadata?.providers
  if (!Array.isArray(providers)) {
    return []
  }

  return providers.filter((provider): provider is string => typeof provider === 'string')
}

export function hasAnonymousIdentity(user: User | null): boolean {
  const providers = getUserProviders(user)
  const isAnonymous = (user as (User & { is_anonymous?: boolean }) | null)?.is_anonymous

  return isAnonymous === true || providers.includes('anonymous')
}
