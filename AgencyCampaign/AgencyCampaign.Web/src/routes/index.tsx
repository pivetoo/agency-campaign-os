import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Callback, ProtectedRoute, useAuth } from 'archon-ui'
import AgencyCampaignLayout from '../layouts/AgencyCampaignLayout'
import Dashboard from '../modules/Dashboard'
import Brands from '../modules/Brands'
import Creators from '../modules/Creators'
import Campaigns from '../modules/Campaigns'
import CampaignDetail from '../modules/Campaigns/Detail'
import FinancialReceivables from '../modules/Financial/Receivables'
import FinancialPayables from '../modules/Financial/Payables'
import Platforms from '../modules/Configuration/Platforms'
import DeliverableKinds from '../modules/Configuration/DeliverableKinds'

const identityManagementUrl = import.meta.env.VITE_IDENTITY_PROVIDER_WEB

function AppRoutes() {
  const { isAuthenticated } = useAuth()

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
              isAuthenticated={isAuthenticated}
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
          <Route path="campanhas/:id" element={<CampaignDetail />} />
          <Route path="financeiro/receber" element={<FinancialReceivables />} />
          <Route path="financeiro/pagar" element={<FinancialPayables />} />
          <Route path="configuracao/plataformas" element={<Platforms />} />
          <Route path="configuracao/tipos-entrega" element={<DeliverableKinds />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  )
}

export default AppRoutes
