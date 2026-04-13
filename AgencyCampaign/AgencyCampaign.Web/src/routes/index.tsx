import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Callback, ProtectedRoute, useAuth } from 'archon-ui'
import AgencyCampaignLayout from '../layouts/AgencyCampaignLayout'
import Dashboard from '../modules/Dashboard'
import Brands from '../modules/Brands'
import Creators from '../modules/Creators'
import Campaigns from '../modules/Campaigns'

const identityManagementUrl = import.meta.env.VITE_IDENTITY_PROVIDER_WEB

function AppRoutes() {
  const { user } = useAuth()

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/callback"
          element={
            <Callback
              identityManagementUrl={identityManagementUrl}
              redirectTo="/"
            />
          }
        />

        <Route
          element={
            <ProtectedRoute
              isAuthenticated={!!user}
              redirectTo={identityManagementUrl}
              externalRedirect={true}
              preserveExternalReturn={true}
              callbackPath="/callback"
            >
              <AgencyCampaignLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="marcas" element={<Brands />} />
          <Route path="creators" element={<Creators />} />
          <Route path="campanhas" element={<Campaigns />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  )
}

export default AppRoutes
