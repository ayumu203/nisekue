import './App.css'
import { Navigate, Route, Routes } from 'react-router-dom'
import Auth from '@/pages/Auth'
import Home from '@/pages/Home'
import Training from '@/pages/Training'
import { useAuth } from '@/contexts/useAuth'

function App() {
  const { isLoading, user } = useAuth()

  if (isLoading) {
    return null
  }

  return (
    <Routes>
      <Route path="/auth" element={user ? <Navigate to="/" replace /> : <Auth />} />
      <Route path="/" element={user ? <Home /> : <Navigate to="/auth" replace />} />
      <Route path="/training" element={user ? <Training /> : <Navigate to="/auth" replace />} />
      <Route path="*" element={<Navigate to={user ? '/' : '/auth'} replace />} />
    </Routes>
  )
}

export default App
