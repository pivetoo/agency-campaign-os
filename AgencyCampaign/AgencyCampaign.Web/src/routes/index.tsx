import { BrowserRouter, Navigate, Routes, Route } from 'react-router-dom'
import { Callback, ProtectedRoute, useAuth, UsersManagementPage } from 'archon-ui'
import AgencyCampaignLayout from '../layouts/AgencyCampaignLayout'
import Dashboard from '../modules/Main'
import Brands from '../modules/Main/Operations/Brands'
import Creators from '../modules/Main/Operations/Creators'
import CreatorDetail from '../modules/Main/Operations/Creators/Detail'
import Campaigns from '../modules/Main/Operations/Campaigns'
import CampaignDetail from '../modules/Main/Operations/Campaigns/Detail'
import CommercialPipeline from '../modules/Main/Commercial/Pipeline'
import PublicProposal from '../modules/Public/Proposal'
import PublicDeliverable from '../modules/Public/Deliverable'
import PublicCampaignReport from '../modules/Public/CampaignReport'
import CreatorPortalLayout from '../modules/CreatorPortal/Layout'
import CreatorPortalDashboard from '../modules/CreatorPortal/Dashboard'
import CreatorPortalCampaigns from '../modules/CreatorPortal/Campaigns'
import CreatorPortalResults from '../modules/CreatorPortal/Results'
import CreatorPortalDocuments from '../modules/CreatorPortal/Documents'
import CreatorPortalPayments from '../modules/CreatorPortal/Payments'
import CreatorPortalProfile from '../modules/CreatorPortal/Profile'
import CreatorPortalContent from '../modules/CreatorPortal/Content'
import OperationsApprovals from '../modules/Main/Operations/Approvals'
import OperationsCalendar from '../modules/Main/Operations/Calendar'
import CommercialOpportunities from '../modules/Main/Commercial/Opportunities'
import CommercialOpportunityDetail from '../modules/Main/Commercial/OpportunityDetail'
import CommercialProposals from '../modules/Main/Commercial/Proposals'
import CommercialProposalDetail from '../modules/Main/Commercial/ProposalDetail'
import CommercialFollowUps from '../modules/Main/Commercial/FollowUps'
import CommercialApprovals from '../modules/Main/Commercial/Approvals'
import CommercialAnalytics from '../modules/Main/Commercial/Analytics'
import CommercialGoals from '../modules/Main/Commercial/Goals'
import OpportunityOutcomeReasons from '../modules/Configuration/OpportunityOutcomeReasons'
import CommercialPolicyAdmin from '../modules/Configuration/CommercialPolicy'
import FinancialReceivables from '../modules/Main/Financial/Receivables'
import FinancialPayables from '../modules/Main/Financial/Payables'
import CreatorPayments from '../modules/Main/Financial/CreatorPayments'
import FinancialCashFlow from '../modules/Main/Financial/CashFlow'
import FinancialAging from '../modules/Main/Financial/Aging'
import Platforms from '../modules/Configuration/Platforms'
import CommercialPipelineStages from '../modules/Configuration/CommercialPipelineStages'
import CampaignCreatorStatuses from '../modules/Configuration/CampaignCreatorStatuses'
import DeliverableKinds from '../modules/Configuration/DeliverableKinds'
import Integrations from '../modules/Configuration/Integrations'
import ProposalTemplates from '../modules/Configuration/ProposalTemplates'
import OpportunitySources from '../modules/Configuration/OpportunitySources'
import OpportunityTags from '../modules/Configuration/OpportunityTags'
import CampaignDocumentTemplates from '../modules/Configuration/CampaignDocumentTemplates'
import DocumentTemplateEditor from '../modules/Configuration/DocumentTemplate'
import FinancialAccounts from '../modules/Main/Financial/FinancialAccounts'
import Banks from '../modules/Configuration/Banks'
import FinancialSubcategories from '../modules/Configuration/FinancialSubcategories'
import AgencyConfiguration from '../modules/Configuration/Agency'
import ProposalLayouts from '../modules/Configuration/ProposalLayouts'
import ProposalLayoutEditor from '../modules/Configuration/ProposalLayout'

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
        <Route path="/d/:token" element={<PublicDeliverable />} />
        <Route path="/r/:token" element={<PublicCampaignReport />} />

        <Route path="/portal/:token" element={<CreatorPortalLayout />}>
          <Route index element={<CreatorPortalDashboard />} />
          <Route path="campanhas" element={<CreatorPortalCampaigns />} />
          <Route path="resultados" element={<CreatorPortalResults />} />
          <Route path="contratos" element={<CreatorPortalDocuments />} />
          <Route path="pagamentos" element={<CreatorPortalPayments />} />
          <Route path="perfil" element={<CreatorPortalProfile />} />
          <Route path="conteudo" element={<CreatorPortalContent />} />
        </Route>

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
          <Route path="creators/:id" element={<CreatorDetail />} />
          <Route path="operacao/aprovacoes" element={<OperationsApprovals />} />
          <Route path="campanhas" element={<Campaigns />} />
          <Route path="campanhas/:id" element={<CampaignDetail />} />
          <Route path="operacao/calendario" element={<OperationsCalendar />} />
          <Route path="comercial" element={<Navigate to="/comercial/pipeline" replace />} />
          <Route path="comercial/pipeline" element={<CommercialPipeline />} />
          <Route path="comercial/oportunidades" element={<CommercialOpportunities />} />
          <Route path="comercial/oportunidades/:id" element={<CommercialOpportunityDetail />} />
          <Route path="comercial/propostas" element={<CommercialProposals />} />
          <Route path="comercial/propostas/:id" element={<CommercialProposalDetail />} />
          <Route path="comercial/negociacoes" element={<Navigate to="/comercial/oportunidades" replace />} />
          <Route path="comercial/aprovacoes" element={<CommercialApprovals />} />
          <Route path="comercial/analytics" element={<CommercialAnalytics />} />
          <Route path="comercial/metas" element={<CommercialGoals />} />
          <Route path="comercial/followups" element={<CommercialFollowUps />} />
          <Route path="financeiro/receber" element={<FinancialReceivables />} />
          <Route path="financeiro/pagar" element={<FinancialPayables />} />
          <Route path="financeiro/repasses-creators" element={<CreatorPayments />} />
          <Route path="financeiro/fluxo-caixa" element={<FinancialCashFlow />} />
          <Route path="financeiro/aging" element={<FinancialAging />} />
          <Route path="configuracao/plataformas" element={<Platforms />} />
          <Route path="configuracao/pipeline-comercial" element={<CommercialPipelineStages />} />
          <Route path="configuracao/status-creators" element={<CampaignCreatorStatuses />} />
          <Route path="configuracao/tipos-entrega" element={<DeliverableKinds />} />
          <Route path="configuracao/integracoes" element={<Integrations />} />
          <Route path="configuracao/itens-proposta" element={<ProposalTemplates />} />
          <Route path="configuracao/origens-oportunidade" element={<OpportunitySources />} />
          <Route path="configuracao/tags-oportunidade" element={<OpportunityTags />} />
          <Route path="configuracao/motivos-ganho" element={<OpportunityOutcomeReasons kind="win" />} />
          <Route path="configuracao/motivos-perda" element={<OpportunityOutcomeReasons kind="loss" />} />
          <Route path="configuracao/politica-comercial" element={<CommercialPolicyAdmin />} />
          <Route path="configuracao/modelos-contrato" element={<CampaignDocumentTemplates />} />
          <Route path="configuracao/modelos-contrato/:id" element={<DocumentTemplateEditor />} />
          <Route path="financeiro/contas" element={<FinancialAccounts />} />
          <Route path="configuracao/contas-financeiras" element={<Navigate to="/financeiro/contas" replace />} />
          <Route path="configuracao/bancos" element={<Banks />} />
          <Route path="configuracao/subcategorias-financeiras" element={<FinancialSubcategories />} />
          <Route path="configuracao" element={<AgencyConfiguration />} />
          <Route path="configuracao/layouts-proposta" element={<ProposalLayouts />} />
          <Route path="configuracao/layouts-proposta/:id" element={<ProposalLayoutEditor />} />
          <Route path="usuarios" element={<UsersManagementPage initialTab="users" hideTabs title="Usuários" subtitle="Gerencie os usuários do contrato ativo" />} />
          <Route path="usuarios/perfis" element={<UsersManagementPage initialTab="roles" hideTabs title="Perfis" subtitle="Gerencie os perfis de acesso do contrato ativo" />} />
        </Route>

        <Route path="*" element={<div>Página não encontrada</div>} />
      </Routes>
    </BrowserRouter>
  )
}

export default AppRoutes
