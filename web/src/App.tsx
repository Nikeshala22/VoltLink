import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import StationsPage from './pages/StationsPage'
import StationDetailPage from './pages/StationDetailPage'
import ReservationsPage from './pages/ReservationsPage'
import ReservationDetailPage from './pages/ReservationDetailPage'
import ProsumersPage from './pages/ProsumersPage'
import PendingActivationsPage from './pages/PendingActivationsPage'
import UsersPage from './pages/UsersPage'

export default function App() {
  return (
    <Routes>
      
      <Route path="/login" element={<LoginPage />} />

      
      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />

        <Route path="/stations" element={<StationsPage />} />
        <Route path="/stations/:id" element={<StationDetailPage />} />

        <Route path="/reservations" element={<ReservationsPage />} />
        <Route path="/reservations/:id" element={<ReservationDetailPage />} />

        <Route path="/prosumers" element={<ProsumersPage />} />

        
        <Route
          path="/activations"
          element={
            <ProtectedRoute roles={['Backoffice']}>
              <PendingActivationsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/users"
          element={
            <ProtectedRoute roles={['Backoffice']}>
              <UsersPage />
            </ProtectedRoute>
          }
        />
      </Route>

     
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
