import { createClient } from '@supabase/supabase-js'

const storage = typeof window === 'undefined' ? undefined : window.sessionStorage

export const supabase = createClient(import.meta.env.VITE_SUPABASE_URL, import.meta.env.VITE_SUPABASE_ANON_KEY, {
  auth: {
    persistSession: true,
    autoRefreshToken: true,
    detectSessionInUrl: true,
    storage,
  },
})
