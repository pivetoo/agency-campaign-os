import { BrowserRouter, Navigate, Routes, Route } from 'react-router-dom'
import { Callback, ProtectedRoute, useAuth } from 'archon-ui'
import AgencyCampaignLayout from '../layouts/AgencyCampaignLayout'
import Dashboard from '../modules/Dashboard'
import Brands from '../modules/Brands'
import Creators from '../modules/Creators'
import Campaigns from '../modules/Campaigns'
import CampaignDetail from '../modules/Campaigns/Detail'
import CommercialPipeline from '../modules/Commercial/Pipeline'
import PublicProposal from '../modules/PublicProposal'
import CommercialOpportunities from '../modules/Commercial/Opportunities'
import CommercialOpportunityDetail from '../modules/Commercial/OpportunityDetail'
import CommercialProposals from '../modules/Commercial/Proposals'
import CommercialProposalDetail from '../modules/Commercial/ProposalDetail'
import CommercialFollowUps from '../modules/Commercial/FollowUps'
import CommercialApprovals from '../modules/Commercial/Approvals'
import CommercialResponsibles from '../modules/Commercial/CommercialResponsibles'
import FinancialReceivables from '../modules/Financial/Receivables'
import FinancialPayables from '../modules/Financial/Payables'
import Platforms from '../modules/Configuration/Platforms'
import CommercialPipelineStages from '../modules/Configuration/CommercialPipelineStages'
import CampaignCreatorStatuses from '../modules/Configuration/CampaignCreatorStatuses'
import DeliverableKinds from '../modules/Configuration/DeliverableKinds'
import Integrations from '../modules/Configuration/Integrations'
import ProposalTemplates from '../modules/Configuration/ProposalTemplates'
import ProposalBlocks from '../modules/Configuration/ProposalBlocks'
import OpportunitySources from '../modules/Configuration/OpportunitySources'
import OpportunityTags from '../modules/Configuration/OpportunityTags'

const identityManagementUrl = import.meta.env.VITE_IDENTITY_MANAGEMENT_URL;
const oidcClientId = import.meta.env.VITE_OIDC_CLIENT_ID;

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
              oidcClientId={oidcClientId}
              redirectTo="/"
            />
          }
        />

        <Route path="/p/:token" element={<PublicProposal />} />

        <Route
          element={
            <ProtectedRoute
              isAuthenticated={isAuthenticated}
              redirectTo={identityManagementUrl}
              externalRedirect={true}
              callbackPath="/callback"
              oidcClientId={oidcClientId}
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
          <Route path="comercial" element={<Navigate to="/comercial/pipeline" replace />} />
          <Route path="comercial/pipeline" element={<CommercialPipeline />} />
          <Route path="comercial/oportunidades" element={<CommercialOpportunities />} />
          <Route path="comercial/oportunidades/:id" element={<CommercialOpportunityDetail />} />
          <Route path="comercial/propostas" element={<CommercialProposals />} />
          <Route path="comercial/propostas/:id" element={<CommercialProposalDetail />} />
          <Route path="comercial/negociacoes" element={<Navigate to="/comercial/oportunidades" replace />} />
          <Route path="comercial/aprovacoes" element={<CommercialApprovals />} />
          <Route path="comercial/followups" element={<CommercialFollowUps />} />
          <Route path="comercial/responsaveis" element={<CommercialResponsibles />} />
          <Route path="financeiro/receber" element={<FinancialReceivables />} />
          <Route path="financeiro/pagar" element={<FinancialPayables />} />
          <Route path="configuracao/plataformas" element={<Platforms />} />
          <Route path="configuracao/pipeline-comercial" element={<CommercialPipelineStages />} />
          <Route path="configuracao/status-creators" element={<CampaignCreatorStatuses />} />
          <Route path="configuracao/tipos-entrega" element={<DeliverableKinds />} />
          <Route path="configuracao/integracoes" element={<Integrations />} />
          <Route path="configuracao/templates-proposta" element={<ProposalTemplates />} />
          <Route path="configuracao/blocos-proposta" element={<ProposalBlocks />} />
          <Route path="configuracao/origens-oportunidade" element={<OpportunitySources />} />
          <Route path="configuracao/tags-oportunidade" element={<OpportunityTags />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  )
}

export default AppRoutes
